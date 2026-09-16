using System.Collections.Generic;
using UnityEngine;
using WoodScrew.PhysicsCore;

public static class ScrewPuzzleSolver
{
    public class Result
    {
        public bool Solvable;
        public int MinimumMoves;
        public long ShortestSolutionCount;

        public int InitialMoveCount;
        public int InitialScrewChoices;
        public int InitialPlankChoices;

        public int OptimalFirstMoveCount;
        public int OptimalFirstScrewCount;
        public List<int> OptimalFirstScrewIds = new List<int>();

        public int PhysicsSafeFirstMoveCount;
        public int PhysicsSafeFirstScrewCount;
        public int BlockedFirstMoveCount;
        public int BlockedFirstScrewCount;

        public int DecisionStates;
        public int DeadEndStates;

        public int ExploredStates;
        public bool StateLimitReached;
    }

    private class State
    {
        public int[] ScrewHoleIds;
        public Vector2[] PlankPositions;
        public float[] PlankRotations;
        public int RemovedPlankMask;
    }

    private struct Move
    {
        public int ScrewIndex;
        public int TargetHoleId;

        public Move(int screwIndex, int targetHoleId)
        {
            ScrewIndex = screwIndex;
            TargetHoleId = targetHoleId;
        }
    }

    private class PathMeta
    {
        public long PathCount;
        public ulong FirstMoveMask;
        public ulong FirstScrewMask;
    }

    public static Result Solve(
        PuzzleDefinition puzzle,
        int maxStates = 100000)
    {
        Result result = new Result();

        if (puzzle == null)
            return result;

        State startState =
            CreateStartState(puzzle);

        int fullMask =
            (1 << puzzle.Planks.Count) - 1;

        Queue<State> queue =
            new Queue<State>();

        Dictionary<string, int> distance =
            new Dictionary<string, int>();

        Dictionary<string, PathMeta> meta =
            new Dictionary<string, PathMeta>();

        string startKey =
            CreateStateKey(startState);

        queue.Enqueue(startState);
        distance[startKey] = 0;

        meta[startKey] =
            new PathMeta
            {
                PathCount = 1,
                FirstMoveMask = 0,
                FirstScrewMask = 0
            };

        List<Move> initialMoves =
            GetLegalMoves(
                puzzle,
                startState
            );

        result.InitialMoveCount =
            initialMoves.Count;

        result.InitialScrewChoices =
            CountDistinctScrews(
                initialMoves
            );

        result.InitialPlankChoices =
            CountDistinctPlanks(
                puzzle,
                initialMoves
            );

        EvaluateInitialPhysics(
            puzzle,
            startState,
            initialMoves,
            result
        );

        int minimumGoalDistance = -1;
        long shortestSolutionCount = 0;
        ulong optimalFirstMoveMask = 0;
        ulong optimalFirstScrewMask = 0;

        while (queue.Count > 0)
        {
            if (result.ExploredStates >= maxStates)
            {
                result.StateLimitReached = true;
                break;
            }

            State current =
                queue.Dequeue();

            result.ExploredStates++;

            string currentKey =
                CreateStateKey(current);

            int currentDistance =
                distance[currentKey];

            PathMeta currentMeta =
                meta[currentKey];

            if (minimumGoalDistance >= 0 &&
                currentDistance >=
                minimumGoalDistance)
            {
                continue;
            }

            if (current.RemovedPlankMask ==
                fullMask)
            {
                continue;
            }

            List<Move> legalMoves =
                GetLegalMoves(
                    puzzle,
                    current
                );

            if (CountDistinctScrews(
                    legalMoves) > 1)
            {
                result.DecisionStates++;
            }

            if (legalMoves.Count == 0)
            {
                result.DeadEndStates++;
                continue;
            }

            for (int moveIndex = 0;
                 moveIndex < legalMoves.Count;
                 moveIndex++)
            {
                Move move =
                    legalMoves[moveIndex];

                State next =
                    ApplyMove(
                        puzzle,
                        current,
                        move
                    );

                int nextDistance =
                    currentDistance + 1;

                ulong nextFirstMoveMask;
                ulong nextFirstScrewMask;

                if (currentDistance == 0)
                {
                    nextFirstMoveMask =
                        moveIndex < 64
                            ? 1UL << moveIndex
                            : 0UL;

                    int screwId =
                        puzzle.Screws[
                            move.ScrewIndex]
                            .Id;

                    nextFirstScrewMask =
                        screwId >= 0 &&
                        screwId < 64
                            ? 1UL << screwId
                            : 0UL;
                }
                else
                {
                    nextFirstMoveMask =
                        currentMeta
                            .FirstMoveMask;

                    nextFirstScrewMask =
                        currentMeta
                            .FirstScrewMask;
                }

                bool isGoal =
                    next.RemovedPlankMask ==
                    fullMask;

                if (isGoal)
                {
                    if (minimumGoalDistance < 0)
                    {
                        minimumGoalDistance =
                            nextDistance;

                        shortestSolutionCount =
                            currentMeta.PathCount;

                        optimalFirstMoveMask =
                            nextFirstMoveMask;

                        optimalFirstScrewMask =
                            nextFirstScrewMask;
                    }
                    else if (
                        nextDistance ==
                        minimumGoalDistance)
                    {
                        shortestSolutionCount =
                            SafeAdd(
                                shortestSolutionCount,
                                currentMeta.PathCount
                            );

                        optimalFirstMoveMask |=
                            nextFirstMoveMask;

                        optimalFirstScrewMask |=
                            nextFirstScrewMask;
                    }

                    continue;
                }

                string nextKey =
                    CreateStateKey(next);

                if (!distance.ContainsKey(
                        nextKey))
                {
                    distance[nextKey] =
                        nextDistance;

                    meta[nextKey] =
                        new PathMeta
                        {
                            PathCount =
                                currentMeta.PathCount,

                            FirstMoveMask =
                                nextFirstMoveMask,

                            FirstScrewMask =
                                nextFirstScrewMask
                        };

                    queue.Enqueue(next);
                }
                else if (
                    distance[nextKey] ==
                    nextDistance)
                {
                    PathMeta nextMeta =
                        meta[nextKey];

                    nextMeta.PathCount =
                        SafeAdd(
                            nextMeta.PathCount,
                            currentMeta.PathCount
                        );

                    nextMeta.FirstMoveMask |=
                        nextFirstMoveMask;

                    nextMeta.FirstScrewMask |=
                        nextFirstScrewMask;
                }
            }
        }

        result.Solvable =
            minimumGoalDistance >= 0;

        result.MinimumMoves =
            minimumGoalDistance;

        result.ShortestSolutionCount =
            shortestSolutionCount;

        result.OptimalFirstMoveCount =
            CountBits(
                optimalFirstMoveMask
            );

        result.OptimalFirstScrewCount =
            CountBits(
                optimalFirstScrewMask
            );

        for (int screwId = 0;
             screwId < 64;
             screwId++)
        {
            if ((optimalFirstScrewMask &
                 (1UL << screwId)) != 0)
            {
                result.OptimalFirstScrewIds
                    .Add(screwId);
            }
        }

        return result;
    }

    private static State CreateStartState(
        PuzzleDefinition puzzle)
    {
        State state =
            new State();

        state.ScrewHoleIds =
            new int[
                puzzle.Screws.Count];

        for (int i = 0;
             i < puzzle.Screws.Count;
             i++)
        {
            state.ScrewHoleIds[i] =
                puzzle.Screws[i]
                    .CurrentHoleId;
        }

        state.PlankPositions =
            new Vector2[
                puzzle.Planks.Count];

        state.PlankRotations =
            new float[
                puzzle.Planks.Count];

        for (int i = 0;
             i < puzzle.Planks.Count;
             i++)
        {
            state.PlankPositions[i] =
                puzzle.Planks[i]
                    .Position;

            state.PlankRotations[i] =
                puzzle.Planks[i]
                    .Rotation;
        }

        state.RemovedPlankMask = 0;

        return state;
    }

    private static List<Move> GetLegalMoves(
        PuzzleDefinition puzzle,
        State state)
    {
        List<Move> moves =
            new List<Move>();

        HashSet<int> occupiedHoles =
            new HashSet<int>();

        foreach (int holeId
                 in state.ScrewHoleIds)
        {
            occupiedHoles.Add(holeId);
        }

        for (int screwIndex = 0;
             screwIndex <
             puzzle.Screws.Count;
             screwIndex++)
        {
            int currentHoleId =
                state.ScrewHoleIds[
                    screwIndex];

            foreach (HoleDefinition hole
                     in puzzle.Holes)
            {
                if (hole.Id ==
                    currentHoleId)
                {
                    continue;
                }

                if (occupiedHoles.Contains(
                        hole.Id))
                {
                    continue;
                }

                if (!IsHoleAccessible(
                        puzzle,
                        state,
                        hole))
                {
                    continue;
                }

                moves.Add(
                    new Move(
                        screwIndex,
                        hole.Id
                    )
                );
            }
        }

        return moves;
    }

    private static State ApplyMove(
        PuzzleDefinition puzzle,
        State current,
        Move move)
    {
        State next =
            CloneState(current);

        next.ScrewHoleIds[
            move.ScrewIndex] =
            move.TargetHoleId;

        ResolveAllPlankPhysics(
            puzzle,
            next
        );

        return next;
    }

    private static State CloneState(
        State source)
    {
        return new State
        {
            ScrewHoleIds =
                (int[])source
                    .ScrewHoleIds.Clone(),

            PlankPositions =
                (Vector2[])source
                    .PlankPositions.Clone(),

            PlankRotations =
                (float[])source
                    .PlankRotations.Clone(),

            RemovedPlankMask =
                source.RemovedPlankMask
        };
    }

    private static void ResolveAllPlankPhysics(
        PuzzleDefinition puzzle,
        State state)
    {
        // Shared screw yüzünden bir hamle birden fazla
        // tahtayı etkileyebilir. Bu yüzden sadece
        // Screw.OwnerPlankId çözülmez; tüm planklar
        // kısa bir stabilizasyon döngüsünden geçer.
        int maximumPasses =
            Mathf.Max(
                2,
                puzzle.Planks.Count * 2
            );

        for (int pass = 0;
             pass < maximumPasses;
             pass++)
        {
            bool changed =
                false;

            for (int plankIndex = 0;
                 plankIndex <
                    puzzle.Planks.Count;
                 plankIndex++)
            {
                if (IsPlankRemoved(
                        state.RemovedPlankMask,
                        plankIndex))
                {
                    continue;
                }

                int supportCount =
                    PuzzleRuleContract
                        .GetSupportCount(
                            puzzle,
                            state.ScrewHoleIds,
                            state.PlankPositions,
                            state.PlankRotations,
                            state.RemovedPlankMask,
                            plankIndex
                        );

                PlankMotionState motionState =
                    WoodScrewPhysicsCore
                        .GetMotionState(
                            supportCount
                        );

                if (motionState ==
                    PlankMotionState.Falling)
                {
                    state.RemovedPlankMask |=
                        1 << plankIndex;

                    changed =
                        true;

                    continue;
                }

                if (motionState ==
                    PlankMotionState.Fixed)
                {
                    continue;
                }

                int pivotScrewId =
                    PuzzleRuleContract
                        .GetSingleSupportingScrewId(
                            puzzle,
                            state.ScrewHoleIds,
                            state.PlankPositions,
                            state.PlankRotations,
                            state.RemovedPlankMask,
                            plankIndex
                        );

                if (pivotScrewId < 0)
                    continue;

                PuzzleDefinition statePuzzle =
                    CreatePuzzleForState(
                        puzzle,
                        state
                    );

                PlankDefinition sourcePlank =
                    puzzle.Planks[
                        plankIndex];

                PlankDefinition statePlank =
                    FindPlank(
                        statePuzzle,
                        sourcePlank.Id
                    );

                if (statePlank == null)
                    continue;

                PuzzlePhysicsModel.PlankPose pose =
                    PuzzlePhysicsModel
                        .GetCollisionAwareSwingPose(
                            statePuzzle,
                            statePlank,
                            pivotScrewId
                        );

                float newRotation =
                    NormalizeAngle(
                        pose.Rotation
                    );

                if (Vector2.Distance(
                        state.PlankPositions[
                            plankIndex],
                        pose.Position) >
                    0.0005f ||
                    Mathf.Abs(
                        Mathf.DeltaAngle(
                            state.PlankRotations[
                                plankIndex],
                            newRotation)) >
                    0.05f)
                {
                    state.PlankPositions[
                        plankIndex] =
                        pose.Position;

                    state.PlankRotations[
                        plankIndex] =
                        newRotation;

                    changed =
                        true;
                }
            }

            if (!changed)
                break;
        }
    }


    private static int
        GetRemainingSupportingScrewId(
            PuzzleDefinition puzzle,
            State state,
            PlankDefinition plank)
    {
        int plankIndex =
            GetPlankIndex(
                puzzle,
                plank.Id
            );

        return
            PuzzleRuleContract
                .GetSingleSupportingScrewId(
                    puzzle,
                    state.ScrewHoleIds,
                    state.PlankPositions,
                    state.PlankRotations,
                    state.RemovedPlankMask,
                    plankIndex
                );
    }

    private static int GetSupportCount(
        PuzzleDefinition puzzle,
        State state,
        PlankDefinition plank)
    {
        int plankIndex =
            GetPlankIndex(
                puzzle,
                plank.Id
            );

        return
            PuzzleRuleContract
                .GetSupportCount(
                    puzzle,
                    state.ScrewHoleIds,
                    state.PlankPositions,
                    state.PlankRotations,
                    state.RemovedPlankMask,
                    plankIndex
                );
    }

    private static bool IsHoleAccessible(
        PuzzleDefinition puzzle,
        State state,
        HoleDefinition hole)
    {
        for (int plankIndex = 0;
             plankIndex <
             puzzle.Planks.Count;
             plankIndex++)
        {
            if (IsPlankRemoved(
                    state.RemovedPlankMask,
                    plankIndex))
            {
                continue;
            }

            // Runtime ile aynı istisna:
            // hedef anchor ile gerçek plank socket'i
            // tam hizalıysa vida tahtanın içinden geçebilir.
            if (PlankHasAlignedSocketAtWorldPosition(
                    puzzle,
                    state,
                    plankIndex,
                    hole.Position,
                    PuzzleRuleContract
                        .InsertAlignmentTolerance))
            {
                continue;
            }

            PlankDefinition plank =
                puzzle.Planks[
                    plankIndex];

            Vector2 effectiveSize =
                PuzzleRuleContract
                    .GetEffectivePlankSize(
                        puzzle,
                        plank
                    );

            CoreOrientedRect plankPose =
                new CoreOrientedRect(
                    new CoreVector2(
                        state.PlankPositions[
                            plankIndex].x,
                        state.PlankPositions[
                            plankIndex].y
                    ),
                    new CoreVector2(
                        effectiveSize.x,
                        effectiveSize.y
                    ),
                    state.PlankRotations[
                        plankIndex]
                );

            if (WoodScrewPhysicsCore
                .IsAnchorBlockedAtPose(
                    new CoreVector2(
                        hole.Position.x,
                        hole.Position.y
                    ),
                    PuzzleRuleContract
                        .AnchorClearanceRadius,
                    plankPose))
            {
                return false;
            }
        }

        return true;
    }


    private static bool
        PlankHasAlignedSocketAtWorldPosition(
            PuzzleDefinition puzzle,
            State state,
            int plankIndex,
            Vector2 worldPosition,
            float tolerance)
    {
        PlankDefinition plank =
            puzzle.Planks[
                plankIndex];

        int[] screwIds =
        {
            plank.ScrewAId,
            plank.ScrewBId,
            plank.ScrewCId
        };

        for (int i = 0;
             i < screwIds.Length;
             i++)
        {
            if (screwIds[i] < 0)
                continue;

            int screwIndex =
                FindScrewIndex(
                    puzzle,
                    screwIds[i]
                );

            if (screwIndex < 0)
                continue;

            ScrewDefinition screw =
                puzzle.Screws[
                    screwIndex];

            HoleDefinition originalHole =
                FindHole(
                    puzzle,
                    screw.OriginalHoleId
                );

            if (originalHole == null)
                continue;

            Vector2 initialOffset =
                originalHole.Position -
                plank.Position;

            Vector2 localSocket =
                RotateVector(
                    initialOffset,
                    -plank.Rotation
                );

            Vector2 socketWorld =
                state.PlankPositions[
                    plankIndex] +
                RotateVector(
                    localSocket,
                    state.PlankRotations[
                        plankIndex]
                );

            if (Vector2.Distance(
                    socketWorld,
                    worldPosition) <=
                tolerance)
            {
                return true;
            }
        }

        return false;
    }


    private static PuzzleDefinition
        CreatePuzzleForState(
            PuzzleDefinition source,
            State state)
    {
        PuzzleDefinition copy =
            new PuzzleDefinition();

        copy.LevelNumber =
            source.LevelNumber;

        for (int i = 0;
             i < source.Planks.Count;
             i++)
        {
            if (IsPlankRemoved(
                    state.RemovedPlankMask,
                    i))
            {
                continue;
            }

            PlankDefinition sourcePlank =
                source.Planks[i];

            PlankDefinition plankCopy =
                new PlankDefinition();

            plankCopy.Id =
                sourcePlank.Id;

            plankCopy.Position =
                state.PlankPositions[i];

            plankCopy.Size =
                sourcePlank.Size;

            plankCopy.Rotation =
                state.PlankRotations[i];

            plankCopy.ScrewAId =
                sourcePlank.ScrewAId;

            plankCopy.ScrewBId =
                sourcePlank.ScrewBId;

            plankCopy.ScrewCId =
                sourcePlank.ScrewCId;

            copy.Planks.Add(
                plankCopy
            );
        }

        foreach (HoleDefinition sourceHole
                 in source.Holes)
        {
            HoleDefinition holeCopy =
                new HoleDefinition();

            holeCopy.Id =
                sourceHole.Id;

            holeCopy.Position =
                sourceHole.Position;

            holeCopy.IsFreeStartHole =
                sourceHole
                    .IsFreeStartHole;

            copy.Holes.Add(
                holeCopy
            );
        }

        for (int i = 0;
             i < source.Screws.Count;
             i++)
        {
            ScrewDefinition sourceScrew =
                source.Screws[i];

            ScrewDefinition screwCopy =
                new ScrewDefinition();

            screwCopy.Id =
                sourceScrew.Id;

            screwCopy.OwnerPlankId =
                sourceScrew
                    .OwnerPlankId;

            screwCopy.OriginalHoleId =
                sourceScrew
                    .OriginalHoleId;

            screwCopy.CurrentHoleId =
                state.ScrewHoleIds[i];

            copy.Screws.Add(
                screwCopy
            );
        }

        return copy;
    }

    private static void EvaluateInitialPhysics(
        PuzzleDefinition puzzle,
        State startState,
        List<Move> initialMoves,
        Result result)
    {
        HashSet<int> safeScrewIds =
            new HashSet<int>();

        HashSet<int> blockedScrewIds =
            new HashSet<int>();

        foreach (Move move
                 in initialMoves)
        {
            ScrewDefinition movedScrew =
                puzzle.Screws[
                    move.ScrewIndex];

            State nextState =
                CloneState(
                    startState
                );

            nextState.ScrewHoleIds[
                move.ScrewIndex] =
                move.TargetHoleId;

            bool blocked =
                false;

            // Bir vida shared olabilir. İlk hamlenin
            // fizik sonucunu yalnız OwnerPlank üzerinden
            // değil, etkilenen bütün planklar üzerinden
            // değerlendiriyoruz.
            for (int plankIndex = 0;
                 plankIndex <
                    puzzle.Planks.Count;
                 plankIndex++)
            {
                int supportCount =
                    PuzzleRuleContract
                        .GetSupportCount(
                            puzzle,
                            nextState.ScrewHoleIds,
                            nextState.PlankPositions,
                            nextState.PlankRotations,
                            nextState.RemovedPlankMask,
                            plankIndex
                        );

                if (WoodScrewPhysicsCore
                    .GetMotionState(
                        supportCount) !=
                    PlankMotionState.Pivoting)
                {
                    continue;
                }

                int pivotScrewId =
                    PuzzleRuleContract
                        .GetSingleSupportingScrewId(
                            puzzle,
                            nextState.ScrewHoleIds,
                            nextState.PlankPositions,
                            nextState.PlankRotations,
                            nextState.RemovedPlankMask,
                            plankIndex
                        );

                if (pivotScrewId < 0)
                    continue;

                PuzzleDefinition statePuzzle =
                    CreatePuzzleForState(
                        puzzle,
                        nextState
                    );

                PlankDefinition sourcePlank =
                    puzzle.Planks[
                        plankIndex];

                PlankDefinition statePlank =
                    FindPlank(
                        statePuzzle,
                        sourcePlank.Id
                    );

                if (statePlank == null)
                    continue;

                PuzzlePhysicsModel.PlankPose pose =
                    PuzzlePhysicsModel
                        .GetCollisionAwareSwingPose(
                            statePuzzle,
                            statePlank,
                            pivotScrewId
                        );

                if (pose.Blocked)
                {
                    blocked =
                        true;

                    break;
                }
            }

            if (blocked)
            {
                result
                    .BlockedFirstMoveCount++;

                blockedScrewIds.Add(
                    movedScrew.Id
                );
            }
            else
            {
                result
                    .PhysicsSafeFirstMoveCount++;

                safeScrewIds.Add(
                    movedScrew.Id
                );
            }
        }

        result.PhysicsSafeFirstScrewCount =
            safeScrewIds.Count;

        result.BlockedFirstScrewCount =
            blockedScrewIds.Count;
    }


    private static int FindScrewIndex(
        PuzzleDefinition puzzle,
        int screwId)
    {
        for (int i = 0;
             i < puzzle.Screws.Count;
             i++)
        {
            if (puzzle.Screws[i].Id ==
                screwId)
            {
                return i;
            }
        }

        return -1;
    }

    private static HoleDefinition FindHole(
        PuzzleDefinition puzzle,
        int holeId)
    {
        foreach (HoleDefinition hole
                 in puzzle.Holes)
        {
            if (hole.Id ==
                holeId)
            {
                return hole;
            }
        }

        return null;
    }


    private static Vector2 RotateVector(
        Vector2 vector,
        float degrees)
    {
        float radians =
            degrees *
            Mathf.Deg2Rad;

        float cos =
            Mathf.Cos(radians);

        float sin =
            Mathf.Sin(radians);

        return
            new Vector2(
                vector.x * cos -
                vector.y * sin,
                vector.x * sin +
                vector.y * cos
            );
    }


    private static PlankDefinition FindPlank(
        PuzzleDefinition puzzle,
        int plankId)
    {
        foreach (PlankDefinition plank
                 in puzzle.Planks)
        {
            if (plank.Id ==
                plankId)
            {
                return plank;
            }
        }

        return null;
    }

    private static int GetPlankIndex(
        PuzzleDefinition puzzle,
        int plankId)
    {
        for (int i = 0;
             i < puzzle.Planks.Count;
             i++)
        {
            if (puzzle.Planks[i].Id ==
                plankId)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsPlankRemoved(
        int mask,
        int plankIndex)
    {
        if (plankIndex < 0)
            return false;

        return
            (mask &
             (1 << plankIndex)) != 0;
    }

    private static int CountDistinctScrews(
        List<Move> moves)
    {
        HashSet<int> screws =
            new HashSet<int>();

        foreach (Move move in moves)
        {
            screws.Add(
                move.ScrewIndex
            );
        }

        return screws.Count;
    }

    private static int CountDistinctPlanks(
        PuzzleDefinition puzzle,
        List<Move> moves)
    {
        HashSet<int> planks =
            new HashSet<int>();

        foreach (Move move in moves)
        {
            int screwId =
                puzzle.Screws[
                    move.ScrewIndex].Id;

            for (int plankIndex = 0;
                 plankIndex <
                    puzzle.Planks.Count;
                 plankIndex++)
            {
                PlankDefinition plank =
                    puzzle.Planks[
                        plankIndex];

                if (PuzzleRuleContract
                    .PlankUsesScrew(
                        plank,
                        screwId))
                {
                    planks.Add(
                        plank.Id
                    );
                }
            }
        }

        return planks.Count;
    }


    private static int CountBits(
        ulong value)
    {
        int count = 0;

        while (value != 0)
        {
            value &=
                value - 1;

            count++;
        }

        return count;
    }

    private static long SafeAdd(
        long a,
        long b)
    {
        if (long.MaxValue - a < b)
            return long.MaxValue;

        return a + b;
    }

    private static float NormalizeAngle(
        float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle < -180f)
            angle += 360f;

        return angle;
    }

    private static string CreateStateKey(
        State state)
    {
        string key =
            state.RemovedPlankMask
                .ToString();

        for (int i = 0;
             i <
             state.ScrewHoleIds.Length;
             i++)
        {
            key +=
                "|S" +
                state.ScrewHoleIds[i];
        }

        for (int i = 0;
             i <
             state.PlankPositions.Length;
             i++)
        {
            if (IsPlankRemoved(
                    state.RemovedPlankMask,
                    i))
            {
                key +=
                    "|P_REMOVED";

                continue;
            }

            int x =
                Mathf.RoundToInt(
                    state.PlankPositions[i].x *
                    100f
                );

            int y =
                Mathf.RoundToInt(
                    state.PlankPositions[i].y *
                    100f
                );

            int rotation =
                Mathf.RoundToInt(
                    NormalizeAngle(
                        state.PlankRotations[i]
                    ) *
                    10f
                );

            key +=
                "|P" +
                x +
                "," +
                y +
                "," +
                rotation;
        }

        return key;
    }
}
