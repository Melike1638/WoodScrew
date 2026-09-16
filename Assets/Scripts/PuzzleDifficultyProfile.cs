using UnityEngine;

public static class PuzzleDifficultyProfile
{
    public class Profile
    {
        public int LevelNumber;

        public int PlankCount;
        public int FreeHoleCount;

        public int MinimumMovesMin;
        public int MinimumMovesMax;

        public int InitialScrewChoicesMin;
        public int InitialScrewChoicesMax;

        public int PhysicsSafeFirstScrewsMin;
        public int PhysicsSafeFirstScrewsMax;

        // -1 = bu profil için çarpışan ilk vida hedefi kullanılmıyor.
        public int BlockedFirstScrewsMin = -1;
        public int BlockedFirstScrewsMax = -1;

        public float DecisionDensityMin;
        public float DecisionDensityMax;

        public float DeadEndRateMax;

        public int CandidateCount;
        public int SolverStateLimit;
    }

    public static Profile Get(
        int levelNumber)
    {
        if (levelNumber <= 1)
        {
            return new Profile
            {
                // Generator'daki Bölüm 1 artık oyundaki Level 3'tür.
                LevelNumber = 3,
                PlankCount = 3,
                FreeHoleCount = 3,

                MinimumMovesMin = 6,
                MinimumMovesMax = 7,

                InitialScrewChoicesMin = 4,
                InitialScrewChoicesMax = 6,

                PhysicsSafeFirstScrewsMin = 4,
                PhysicsSafeFirstScrewsMax = 6,

                BlockedFirstScrewsMin = 0,
                BlockedFirstScrewsMax = 2,

                DecisionDensityMin = 0.25f,
                DecisionDensityMax = 0.40f,

                DeadEndRateMax = 0.05f,

                CandidateCount = 500,
                SolverStateLimit = 8000
            };
        }

        if (levelNumber == 2)
        {
            return new Profile
            {
                LevelNumber = 2,
                PlankCount = 3,
                FreeHoleCount = 2,

                MinimumMovesMin = 6,
                MinimumMovesMax = 8,

                InitialScrewChoicesMin = 4,
                InitialScrewChoicesMax = 6,

                PhysicsSafeFirstScrewsMin = 2,
                PhysicsSafeFirstScrewsMax = 4,

                DecisionDensityMin = 0.18f,
                DecisionDensityMax = 0.33f,

                DeadEndRateMax = 0.06f,

                CandidateCount = 20,
                SolverStateLimit = 9000
            };
        }

        if (levelNumber == 3)
        {
            return new Profile
            {
                LevelNumber = 3,
                PlankCount = 3,
                FreeHoleCount = 2,

                MinimumMovesMin = 7,
                MinimumMovesMax = 9,

                InitialScrewChoicesMin = 3,
                InitialScrewChoicesMax = 6,

                PhysicsSafeFirstScrewsMin = 1,
                PhysicsSafeFirstScrewsMax = 3,

                DecisionDensityMin = 0.22f,
                DecisionDensityMax = 0.38f,

                DeadEndRateMax = 0.08f,

                CandidateCount = 22,
                SolverStateLimit = 10000
            };
        }

        int stage =
            Mathf.Clamp(
                (levelNumber - 1) / 3,
                1,
                4
            );

        return new Profile
        {
            LevelNumber = levelNumber,

            PlankCount =
                Mathf.Min(
                    3 + stage,
                    5
                ),

            FreeHoleCount =
                2 +
                (stage >= 3 ? 1 : 0),

            MinimumMovesMin =
                7 +
                stage * 2,

            MinimumMovesMax =
                9 +
                stage * 3,

            InitialScrewChoicesMin = 3,
            InitialScrewChoicesMax =
                Mathf.Min(
                    10,
                    5 + stage
                ),

            PhysicsSafeFirstScrewsMin = 1,
            PhysicsSafeFirstScrewsMax =
                3 + stage,

            DecisionDensityMin =
                Mathf.Min(
                    0.42f,
                    0.24f +
                    stage * 0.035f
                ),

            DecisionDensityMax =
                Mathf.Min(
                    0.55f,
                    0.36f +
                    stage * 0.04f
                ),

            DeadEndRateMax =
                Mathf.Min(
                    0.14f,
                    0.06f +
                    stage * 0.02f
                ),

            CandidateCount = 16,
            SolverStateLimit = 9000
        };
    }

    public static bool Matches(
        Profile profile,
        ScrewPuzzleSolver.Result result)
    {
        if (profile == null ||
            result == null)
        {
            return false;
        }

        if (!result.Solvable)
            return false;

        if (result.StateLimitReached)
            return false;

        if (result.MinimumMoves <
                profile.MinimumMovesMin ||
            result.MinimumMoves >
                profile.MinimumMovesMax)
        {
            return false;
        }

        if (result.InitialScrewChoices <
                profile.InitialScrewChoicesMin ||
            result.InitialScrewChoices >
                profile.InitialScrewChoicesMax)
        {
            return false;
        }

        if (result.PhysicsSafeFirstScrewCount <
                profile.PhysicsSafeFirstScrewsMin ||
            result.PhysicsSafeFirstScrewCount >
                profile.PhysicsSafeFirstScrewsMax)
        {
            return false;
        }

        if (profile.BlockedFirstScrewsMin >= 0 &&
            profile.BlockedFirstScrewsMax >= 0 &&
            (result.BlockedFirstScrewCount <
                 profile.BlockedFirstScrewsMin ||
             result.BlockedFirstScrewCount >
                 profile.BlockedFirstScrewsMax))
        {
            return false;
        }

        float decisionDensity =
            GetDecisionDensity(result);

        if (decisionDensity <
                profile.DecisionDensityMin ||
            decisionDensity >
                profile.DecisionDensityMax)
        {
            return false;
        }

        float deadEndRate =
            GetDeadEndRate(result);

        if (deadEndRate >
            profile.DeadEndRateMax)
        {
            return false;
        }

        return true;
    }

    public static float Score(
        Profile profile,
        ScrewPuzzleSolver.Result result)
    {
        if (profile == null ||
            result == null)
        {
            return 1000000f;
        }

        if (!result.Solvable)
            return 900000f;

        if (result.StateLimitReached)
            return 700000f;

        float score = 0f;

        score +=
            RangePenalty(
                result.MinimumMoves,
                profile.MinimumMovesMin,
                profile.MinimumMovesMax
            ) *
            120f;

        score +=
            RangePenalty(
                result.InitialScrewChoices,
                profile.InitialScrewChoicesMin,
                profile.InitialScrewChoicesMax
            ) *
            80f;

        score +=
            RangePenalty(
                result.PhysicsSafeFirstScrewCount,
                profile.PhysicsSafeFirstScrewsMin,
                profile.PhysicsSafeFirstScrewsMax
            ) *
            100f;

        if (profile.BlockedFirstScrewsMin >= 0 &&
            profile.BlockedFirstScrewsMax >= 0)
        {
            score +=
                RangePenalty(
                    result.BlockedFirstScrewCount,
                    profile.BlockedFirstScrewsMin,
                    profile.BlockedFirstScrewsMax
                ) *
                150f;
        }

        float decisionDensity =
            GetDecisionDensity(result);

        score +=
            FloatRangePenalty(
                decisionDensity,
                profile.DecisionDensityMin,
                profile.DecisionDensityMax
            ) *
            1000f;

        // Hedef aralığındaki birden fazla adayın hepsi
        // eskiden 0 puan alabiliyordu. "En iyi aday" için
        // aralığın merkezine yakın olanı çok küçük bir
        // tie-break ile tercih ediyoruz.
        float decisionMidpoint =
            (profile.DecisionDensityMin +
             profile.DecisionDensityMax) *
            0.5f;

        score +=
            Mathf.Abs(
                decisionDensity -
                decisionMidpoint
            ) *
            10f;

        float deadEndRate =
            GetDeadEndRate(result);

        // Maksimum sınırın altında da daha az çıkmaz
        // küçük bir kalite avantajıdır.
        score +=
            deadEndRate *
            10f;

        if (deadEndRate >
            profile.DeadEndRateMax)
        {
            score +=
                (deadEndRate -
                 profile.DeadEndRateMax) *
                2500f;
        }

        return score;
    }

    public static float GetDecisionDensity(
        ScrewPuzzleSolver.Result result)
    {
        if (result == null ||
            result.ExploredStates <= 0)
        {
            return 0f;
        }

        return
            (float)result.DecisionStates /
            result.ExploredStates;
    }

    public static float GetDeadEndRate(
        ScrewPuzzleSolver.Result result)
    {
        if (result == null ||
            result.ExploredStates <= 0)
        {
            return 0f;
        }

        return
            (float)result.DeadEndStates /
            result.ExploredStates;
    }

    private static int RangePenalty(
        int value,
        int minimum,
        int maximum)
    {
        if (value < minimum)
            return minimum - value;

        if (value > maximum)
            return value - maximum;

        return 0;
    }

    private static float FloatRangePenalty(
        float value,
        float minimum,
        float maximum)
    {
        if (value < minimum)
            return minimum - value;

        if (value > maximum)
            return value - maximum;

        return 0f;
    }
}
