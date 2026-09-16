using System.Collections.Generic;
using UnityEngine;
using WoodScrew.PhysicsCore;

public static class PuzzlePhysicsModel
{
    private const float ScrewRadius =
        PuzzleRuleContract.ScrewCollisionRadius;

    public struct PlankPose
    {
        public Vector2 Position;
        public float Rotation;
        public float SwingAngle;

        public bool Blocked;
        public int BlockingPlankId;
        public int BlockingScrewId;

        public PlankPose(
            Vector2 position,
            float rotation,
            float swingAngle,
            bool blocked = false,
            int blockingPlankId = -1,
            int blockingScrewId = -1)
        {
            Position = position;
            Rotation = rotation;
            SwingAngle = swingAngle;

            Blocked = blocked;
            BlockingPlankId = blockingPlankId;
            BlockingScrewId = blockingScrewId;
        }
    }

    public static PlankPose GetUnobstructedSwingPose(
        PuzzleDefinition puzzle,
        PlankDefinition plank,
        int pivotScrewId)
    {
        ScrewDefinition pivotScrew =
            FindScrew(puzzle, pivotScrewId);

        if (pivotScrew == null)
        {
            return new PlankPose(
                plank.Position,
                plank.Rotation,
                0f
            );
        }

        HoleDefinition pivotHole =
            FindHole(
                puzzle,
                pivotScrew.CurrentHoleId
            );

        if (pivotHole == null)
        {
            return new PlankPose(
                plank.Position,
                plank.Rotation,
                0f
            );
        }

        float swingAngle =
            GetTargetSwingAngle(
                plank,
                pivotHole.Position
            );

        return GetPoseAtAngle(
            plank,
            pivotHole.Position,
            swingAngle
        );
    }

    public static PlankPose GetCollisionAwareSwingPose(
        PuzzleDefinition puzzle,
        PlankDefinition plank,
        int pivotScrewId,
        float stepDegrees = 2f)
    {
        ScrewDefinition pivotScrew =
            FindScrew(puzzle, pivotScrewId);

        if (pivotScrew == null)
        {
            return new PlankPose(
                plank.Position,
                plank.Rotation,
                0f
            );
        }

        HoleDefinition pivotHole =
            FindHole(
                puzzle,
                pivotScrew.CurrentHoleId
            );

        if (pivotHole == null)
        {
            return new PlankPose(
                plank.Position,
                plank.Rotation,
                0f
            );
        }

        Vector2 pivotPosition =
            pivotHole.Position;

        float targetSwingAngle =
            GetTargetSwingAngle(
                plank,
                pivotPosition
            );

        PlankPose startPose =
            new PlankPose(
                plank.Position,
                plank.Rotation,
                0f
            );

        HashSet<int> initiallyOverlappingPlanks =
            GetInitiallyOverlappingPlanks(
                puzzle,
                plank,
                startPose
            );

        HashSet<int> initiallyOverlappingScrews =
            GetInitiallyOverlappingScrews(
                puzzle,
                plank,
                startPose,
                pivotScrewId
            );

        int stepCount =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    Mathf.Abs(targetSwingAngle) /
                    stepDegrees
                )
            );

        PlankPose previousPose =
            startPose;

        for (int step = 1;
             step <= stepCount;
             step++)
        {
            float progress =
                (float)step /
                stepCount;

            float currentAngle =
                targetSwingAngle *
                progress;

            PlankPose currentPose =
                GetPoseAtAngle(
                    plank,
                    pivotPosition,
                    currentAngle
                );

            int blockingPlankId;
            int blockingScrewId;

            bool blocked =
                IsPoseBlocked(
                    puzzle,
                    plank,
                    previousPose,
                    currentPose,
                    pivotPosition,
                    pivotScrewId,
                    initiallyOverlappingPlanks,
                    initiallyOverlappingScrews,
                    out blockingPlankId,
                    out blockingScrewId
                );

            if (blocked)
            {
                return new PlankPose(
                    previousPose.Position,
                    previousPose.Rotation,
                    previousPose.SwingAngle,
                    true,
                    blockingPlankId,
                    blockingScrewId
                );
            }

            previousPose =
                currentPose;
        }

        return previousPose;
    }

    private static float GetTargetSwingAngle(
        PlankDefinition plank,
        Vector2 pivotPosition)
    {
        Vector2 centerOffset =
            plank.Position -
            pivotPosition;

        if (centerOffset.sqrMagnitude <
            0.0001f)
        {
            return 0f;
        }

        float currentCenterAngle =
            Mathf.Atan2(
                centerOffset.y,
                centerOffset.x
            ) *
            Mathf.Rad2Deg;

        float targetCenterAngle = -90f;

        return Mathf.DeltaAngle(
            currentCenterAngle,
            targetCenterAngle
        );
    }

    private static PlankPose GetPoseAtAngle(
        PlankDefinition plank,
        Vector2 pivotPosition,
        float swingAngle)
    {
        Vector2 originalOffset =
            plank.Position -
            pivotPosition;

        Vector2 rotatedOffset =
            RotateVector(
                originalOffset,
                swingAngle
            );

        Vector2 finalPosition =
            pivotPosition +
            rotatedOffset;

        float finalRotation =
            plank.Rotation +
            swingAngle;

        return new PlankPose(
            finalPosition,
            finalRotation,
            swingAngle
        );
    }

    private static bool IsPoseBlocked(
        PuzzleDefinition puzzle,
        PlankDefinition movingPlank,
        PlankPose previousPose,
        PlankPose movingPose,
        Vector2 pivotPosition,
        int pivotScrewId,
        HashSet<int> initiallyOverlappingPlanks,
        HashSet<int> initiallyOverlappingScrews,
        out int blockingPlankId,
        out int blockingScrewId)
    {
        blockingPlankId = -1;
        blockingScrewId = -1;

        // Tahta-tahta çarpışması mevcut SAT kontrolünde kalıyor.
        foreach (PlankDefinition otherPlank
                 in puzzle.Planks)
        {
            if (otherPlank.Id ==
                movingPlank.Id)
            {
                continue;
            }

            if (initiallyOverlappingPlanks.Contains(
                    otherPlank.Id))
            {
                continue;
            }

            if (PlanksOverlap(
                    movingPose,
                    PuzzleRuleContract
                        .GetEffectivePlankSize(
                            puzzle,
                            movingPlank
                        ),
                    otherPlank,
                    puzzle))
            {
                blockingPlankId =
                    otherPlank.Id;

                return true;
            }
        }

        // Vida çarpışması/tunneling artık runtime ile
        // aynı PhysicsCore sweep kuralını kullanıyor.
        Vector2 effectiveSize =
            PuzzleRuleContract
                .GetEffectivePlankSize(
                    puzzle,
                    movingPlank
                );

        CoreOrientedRect startRect =
            new CoreOrientedRect(
                new CoreVector2(
                    previousPose.Position.x,
                    previousPose.Position.y
                ),
                new CoreVector2(
                    effectiveSize.x,
                    effectiveSize.y
                ),
                previousPose.Rotation
            );

        CoreVector2 corePivot =
            new CoreVector2(
                pivotPosition.x,
                pivotPosition.y
            );

        float segmentAngle =
            Mathf.DeltaAngle(
                previousPose.Rotation,
                movingPose.Rotation
            );

        foreach (ScrewDefinition screw
                 in puzzle.Screws)
        {
            if (screw.Id ==
                pivotScrewId)
            {
                continue;
            }

            HoleDefinition screwHole =
                FindHole(
                    puzzle,
                    screw.CurrentHoleId
                );

            if (screwHole == null)
                continue;

            CoreCircle obstacle =
                new CoreCircle(
                    new CoreVector2(
                        screwHole.Position.x,
                        screwHole.Position.y
                    ),
                    ScrewRadius
                );

            if (WoodScrewPhysicsCore
                .SweepHitsCircle(
                    startRect,
                    corePivot,
                    segmentAngle,
                    obstacle,
                    0f,
                    4))
            {
                blockingScrewId =
                    screw.Id;

                return true;
            }
        }

        return false;
    }


    private static HashSet<int>
        GetInitiallyOverlappingPlanks(
            PuzzleDefinition puzzle,
            PlankDefinition movingPlank,
            PlankPose startPose)
    {
        HashSet<int> result =
            new HashSet<int>();

        foreach (PlankDefinition otherPlank
                 in puzzle.Planks)
        {
            if (otherPlank.Id ==
                movingPlank.Id)
            {
                continue;
            }

            if (PlanksOverlap(
                    startPose,
                    PuzzleRuleContract
                        .GetEffectivePlankSize(
                            puzzle,
                            movingPlank
                        ),
                    otherPlank,
                    puzzle))
            {
                result.Add(
                    otherPlank.Id
                );
            }
        }

        return result;
    }

    private static HashSet<int>
        GetInitiallyOverlappingScrews(
            PuzzleDefinition puzzle,
            PlankDefinition movingPlank,
            PlankPose startPose,
            int pivotScrewId)
    {
        // Runtime swing sırasında yalnız gerçek pivot vidasını
        // collision hesabından çıkarıyor. Başlangıçta planka
        // değen başka bir vida otomatik olarak "ignore" edilmez.
        return
            new HashSet<int>();
    }


    private static bool PlanksOverlap(
        PlankPose movingPose,
        Vector2 movingSize,
        PlankDefinition otherPlank,
        PuzzleDefinition puzzle)
    {
        Vector2 movingRight =
            RotateVector(
                Vector2.right,
                movingPose.Rotation
            );

        Vector2 movingUp =
            RotateVector(
                Vector2.up,
                movingPose.Rotation
            );

        Vector2 otherRight =
            RotateVector(
                Vector2.right,
                otherPlank.Rotation
            );

        Vector2 otherUp =
            RotateVector(
                Vector2.up,
                otherPlank.Rotation
            );

        Vector2[] axes =
        {
            movingRight,
            movingUp,
            otherRight,
            otherUp
        };

        Vector2 centerDifference =
            otherPlank.Position -
            movingPose.Position;

        Vector2 movingHalf =
            movingSize * 0.5f;

        Vector2 otherHalf =
            PuzzleRuleContract
                .GetEffectivePlankSize(
                    puzzle,
                    otherPlank
                ) *
            0.5f;

        foreach (Vector2 axis in axes)
        {
            float distance =
                Mathf.Abs(
                    Vector2.Dot(
                        centerDifference,
                        axis
                    )
                );

            float movingRadius =
                movingHalf.x *
                Mathf.Abs(
                    Vector2.Dot(
                        axis,
                        movingRight
                    )
                ) +
                movingHalf.y *
                Mathf.Abs(
                    Vector2.Dot(
                        axis,
                        movingUp
                    )
                );

            float otherRadius =
                otherHalf.x *
                Mathf.Abs(
                    Vector2.Dot(
                        axis,
                        otherRight
                    )
                ) +
                otherHalf.y *
                Mathf.Abs(
                    Vector2.Dot(
                        axis,
                        otherUp
                    )
                );

            if (distance >
                movingRadius +
                otherRadius)
            {
                return false;
            }
        }

        return true;
    }

    private static bool PlankIntersectsCircle(
        PlankPose plankPose,
        Vector2 plankSize,
        Vector2 circlePosition,
        float circleRadius)
    {
        Vector2 offset =
            circlePosition -
            plankPose.Position;

        Vector2 local =
            RotateVector(
                offset,
                -plankPose.Rotation
            );

        Vector2 halfSize =
            plankSize * 0.5f;

        float closestX =
            Mathf.Clamp(
                local.x,
                -halfSize.x,
                halfSize.x
            );

        float closestY =
            Mathf.Clamp(
                local.y,
                -halfSize.y,
                halfSize.y
            );

        Vector2 closestPoint =
            new Vector2(
                closestX,
                closestY
            );

        float distanceSquared =
            (local - closestPoint)
            .sqrMagnitude;

        return distanceSquared <=
            circleRadius *
            circleRadius;
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

        return new Vector2(
            vector.x * cos -
            vector.y * sin,

            vector.x * sin +
            vector.y * cos
        );
    }

    private static ScrewDefinition FindScrew(
        PuzzleDefinition puzzle,
        int screwId)
    {
        foreach (ScrewDefinition screw
                 in puzzle.Screws)
        {
            if (screw.Id == screwId)
                return screw;
        }

        return null;
    }

    private static HoleDefinition FindHole(
        PuzzleDefinition puzzle,
        int holeId)
    {
        foreach (HoleDefinition hole
                 in puzzle.Holes)
        {
            if (hole.Id == holeId)
                return hole;
        }

        return null;
    }
}
