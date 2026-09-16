using System.Collections.Generic;

public static class PuzzleSolver
{
    public class Result
    {
        public bool Solvable;
        public int MinimumMoves;
        public long SolutionCount;
        public int InitialChoices;
        public int DecisionStates;
        public int DeadEndStates;
    }

    public static Result Solve(
        int plankCount,
        List<int>[] dependencies)
    {
        Result result = new Result();

        int fullState = (1 << plankCount) - 1;

        Queue<int> queue = new Queue<int>();
        Dictionary<int, int> distance =
            new Dictionary<int, int>();

        queue.Enqueue(0);
        distance[0] = 0;

        result.InitialChoices =
            CountAvailablePlanks(
                0,
                plankCount,
                dependencies
            );

        while (queue.Count > 0)
        {
            int state = queue.Dequeue();

            if (state == fullState)
                continue;

            int availableChoices =
                CountAvailablePlanks(
                    state,
                    plankCount,
                    dependencies
                );

            if (availableChoices > 1)
                result.DecisionStates++;

            if (availableChoices == 0)
                result.DeadEndStates++;

            for (int plank = 0;
                 plank < plankCount;
                 plank++)
            {
                if (IsRemoved(state, plank))
                    continue;

                if (!DependenciesSatisfied(
                    state,
                    dependencies[plank]))
                {
                    continue;
                }

                int nextState =
                    state | (1 << plank);

                if (!distance.ContainsKey(nextState))
                {
                    distance[nextState] =
                        distance[state] + 2;

                    queue.Enqueue(nextState);
                }
            }
        }

        result.Solvable =
            distance.ContainsKey(fullState);

        result.MinimumMoves =
            result.Solvable
                ? distance[fullState]
                : -1;

        Dictionary<int, long> memo =
            new Dictionary<int, long>();

        result.SolutionCount =
            CountSolutions(
                0,
                fullState,
                plankCount,
                dependencies,
                memo
            );

        return result;
    }

    private static int CountAvailablePlanks(
        int state,
        int plankCount,
        List<int>[] dependencies)
    {
        int count = 0;

        for (int plank = 0;
             plank < plankCount;
             plank++)
        {
            if (IsRemoved(state, plank))
                continue;

            if (DependenciesSatisfied(
                state,
                dependencies[plank]))
            {
                count++;
            }
        }

        return count;
    }

    private static long CountSolutions(
        int state,
        int fullState,
        int plankCount,
        List<int>[] dependencies,
        Dictionary<int, long> memo)
    {
        if (state == fullState)
            return 1;

        if (memo.ContainsKey(state))
            return memo[state];

        long totalSolutions = 0;

        for (int plank = 0;
             plank < plankCount;
             plank++)
        {
            if (IsRemoved(state, plank))
                continue;

            if (!DependenciesSatisfied(
                state,
                dependencies[plank]))
            {
                continue;
            }

            int nextState =
                state | (1 << plank);

            totalSolutions +=
                CountSolutions(
                    nextState,
                    fullState,
                    plankCount,
                    dependencies,
                    memo
                );
        }

        memo[state] = totalSolutions;

        return totalSolutions;
    }

    private static bool IsRemoved(
        int state,
        int plank)
    {
        return (state & (1 << plank)) != 0;
    }

    private static bool DependenciesSatisfied(
        int state,
        List<int> dependencies)
    {
        foreach (int requiredPlank in dependencies)
        {
            if (!IsRemoved(
                state,
                requiredPlank))
            {
                return false;
            }
        }

        return true;
    }
}