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

        // -1 / -1 = bu profil için çarpışan ilk vida hedefi kullanılmıyor.
        public int BlockedFirstScrewsMin;
        public int BlockedFirstScrewsMax;

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
                LevelNumber = 1,
                PlankCount = 3,
                FreeHoleCount = 2,

                MinimumMovesMin = 6,
                MinimumMovesMax = 7,

                InitialScrewChoicesMin = 4,
                InitialScrewChoicesMax = 6,

                PhysicsSafeFirstScrewsMin = 2,
                PhysicsSafeFirstScrewsMax = 4,

                BlockedFirstScrewsMin = -1,
                BlockedFirstScrewsMax = -1,

                DecisionDensityMin = 0.15f,
                DecisionDensityMax = 0.30f,

                DeadEndRateMax = 0.05f,

                CandidateCount = 18,
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

                BlockedFirstScrewsMin = -1,
                BlockedFirstScrewsMax = -1,

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

                // Yeni erken-oyun Level 3:
                // 2 bağımsız tahta,
                // shared screw yok,
                // 2 vida / tahta,
                // toplam 4 boş hedef deliği.
                PlankCount = 2,
                FreeHoleCount = 4,

                // Her 4 gerçek vida en az bir kez taşınır.
                MinimumMovesMin = 4,
                MinimumMovesMax = 4,

                InitialScrewChoicesMin = 4,
                InitialScrewChoicesMax = 4,

                PhysicsSafeFirstScrewsMin = 4,
                PhysicsSafeFirstScrewsMax = 4,

                BlockedFirstScrewsMin = 0,
                BlockedFirstScrewsMax = 0,

                // Bu geometri özellikle basit tutuluyor.
                // 4 boş hedef yüzünden karar yoğunluğu
                // doğal olarak yüksek olabilir.
                DecisionDensityMin = 0.50f,
                DecisionDensityMax = 1.00f,

                DeadEndRateMax = 0.05f,

                CandidateCount = 12,
                SolverStateLimit = 8000
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

            BlockedFirstScrewsMin = -1,
            BlockedFirstScrewsMax = -1,

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

        score +=
            FloatRangePenalty(
                GetDecisionDensity(result),
                profile.DecisionDensityMin,
                profile.DecisionDensityMax
            ) *
            1000f;

        float deadEndRate =
            GetDeadEndRate(result);

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
