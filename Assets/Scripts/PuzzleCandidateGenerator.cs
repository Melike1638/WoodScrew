using System;
using UnityEngine;

public static class PuzzleCandidateGenerator
{
    public static PuzzleDefinition Generate(
        int levelNumber,
        int seed)
    {
        PuzzleDifficultyProfile.Profile profile =
            PuzzleDifficultyProfile.Get(
                levelNumber
            );

        System.Random random =
            new System.Random(seed);

        // Level 3 artık eski H geometrisini kullanmıyor.
        // Erken oyun için sade ve okunabilir:
        // 2 bağımsız tahta, 4 gerçek vida, 4 boş hedef deliği.
        if (levelNumber == 3)
        {
            return GenerateSimpleLevel3Candidate(
                levelNumber,
                random
            );
        }

        if (profile.PlankCount <= 3)
        {
            return GenerateThreePlankCandidate(
                levelNumber,
                random
            );
        }

        return GenerateStackCandidate(
            levelNumber,
            profile.PlankCount,
            profile.FreeHoleCount,
            random
        );
    }

    private static PuzzleDefinition
        GenerateSimpleLevel3Candidate(
            int levelNumber,
            System.Random random)
    {
        PuzzleDefinition puzzle =
            new PuzzleDefinition();

        puzzle.LevelNumber =
            levelNumber;

        // 4 boş storage / hedef deliği.
        // Tahtalardan tamamen bağımsız ve üst sırada.
        float freeHoleY =
            3.00f;

        float freeHoleSpacing =
            1.05f;

        for (int i = 0;
             i < 4;
             i++)
        {
            float x =
                (i - 1.5f) *
                freeHoleSpacing;

            AddHole(
                puzzle,
                puzzle.Holes.Count,
                new Vector2(
                    x,
                    freeHoleY
                ),
                true
            );
        }

        // İki tahta birbirine değmiyor.
        // Shared screw yok.
        // Her tahtada tam 2 vida var.
        float topWidth =
            Range(
                random,
                2.90f,
                3.20f
            );

        float bottomWidth =
            Range(
                random,
                2.75f,
                3.05f
            );

        float topX =
            Range(
                random,
                -0.12f,
                0.12f
            );

        float bottomX =
            Range(
                random,
                -0.12f,
                0.12f
            );

        AddPlankWithSymmetricScrews(
            puzzle,
            0,
            new Vector2(
                topX,
                0.85f
            ),
            new Vector2(
                topWidth,
                0.52f
            ),
            0f,
            0.68f
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            1,
            new Vector2(
                bottomX,
                -0.85f
            ),
            new Vector2(
                bottomWidth,
                0.52f
            ),
            0f,
            0.68f
        );

        return puzzle;
    }


    private static PuzzleDefinition
        GenerateThreePlankCandidate(
            int levelNumber,
            System.Random random)
    {
        PuzzleDefinition puzzle =
            new PuzzleDefinition();

        puzzle.LevelNumber =
            levelNumber;

        float freeHoleSpacing =
            Range(random, 0.55f, 0.95f);

        float freeHoleY =
            Range(random, 2.65f, 3.20f);

        AddHole(
            puzzle,
            0,
            new Vector2(
                -freeHoleSpacing,
                freeHoleY
            ),
            true
        );

        AddHole(
            puzzle,
            1,
            new Vector2(
                freeHoleSpacing,
                freeHoleY
            ),
            true
        );

        int template =
            random.Next(0, 5);

        if (template == 0)
        {
            BuildHorizontalGate(
                puzzle,
                random
            );
        }
        else if (template == 1)
        {
            BuildCenterLock(
                puzzle,
                random
            );
        }
        else if (template == 2)
        {
            BuildMirroredDiagonals(
                puzzle,
                random
            );
        }
        else if (template == 3)
        {
            BuildSidePosts(
                puzzle,
                random
            );
        }
        else
        {
            BuildLayeredBars(
                puzzle,
                random
            );
        }

        return puzzle;
    }

    private static void BuildHorizontalGate(
        PuzzleDefinition puzzle,
        System.Random random)
    {
        float topY =
            Range(random, 0.95f, 1.35f);

        float middleY =
            Range(random, -0.15f, 0.25f);

        float bottomY =
            Range(random, -1.35f, -0.85f);

        AddPlankWithSymmetricScrews(
            puzzle,
            0,
            new Vector2(0f, topY),
            new Vector2(
                Range(random, 4.1f, 4.7f),
                0.52f
            ),
            0f,
            Range(random, 0.62f, 0.78f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            1,
            new Vector2(
                Range(random, -0.20f, 0.20f),
                middleY
            ),
            new Vector2(
                Range(random, 3.2f, 4.0f),
                0.52f
            ),
            0f,
            Range(random, 0.62f, 0.78f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            2,
            new Vector2(0f, bottomY),
            new Vector2(
                Range(random, 2.6f, 3.4f),
                0.52f
            ),
            0f,
            Range(random, 0.60f, 0.76f)
        );
    }

    private static void BuildCenterLock(
        PuzzleDefinition puzzle,
        System.Random random)
    {
        float topY =
            Range(random, 0.85f, 1.20f);

        float lowerY =
            Range(random, -1.15f, -0.75f);

        AddPlankWithSymmetricScrews(
            puzzle,
            0,
            new Vector2(0f, topY),
            new Vector2(
                Range(random, 4.1f, 4.7f),
                0.52f
            ),
            0f,
            Range(random, 0.62f, 0.78f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            1,
            new Vector2(
                0f,
                Range(random, -0.10f, 0.20f)
            ),
            new Vector2(
                0.52f,
                Range(random, 2.7f, 3.5f)
            ),
            0f,
            Range(random, 0.60f, 0.76f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            2,
            new Vector2(0f, lowerY),
            new Vector2(
                Range(random, 2.8f, 3.6f),
                0.52f
            ),
            0f,
            Range(random, 0.60f, 0.76f)
        );
    }

    private static void BuildMirroredDiagonals(
        PuzzleDefinition puzzle,
        System.Random random)
    {
        float topY =
            Range(random, 1.00f, 1.35f);

        float lowerY =
            Range(random, -0.80f, -0.35f);

        float sideX =
            Range(random, 0.55f, 0.90f);

        float angle =
            Range(random, 18f, 32f);

        AddPlankWithSymmetricScrews(
            puzzle,
            0,
            new Vector2(0f, topY),
            new Vector2(
                Range(random, 4.0f, 4.6f),
                0.52f
            ),
            0f,
            Range(random, 0.62f, 0.78f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            1,
            new Vector2(-sideX, lowerY),
            new Vector2(
                Range(random, 2.8f, 3.4f),
                0.52f
            ),
            -angle,
            Range(random, 0.58f, 0.72f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            2,
            new Vector2(sideX, lowerY),
            new Vector2(
                puzzle.Planks[1].Size.x,
                0.52f
            ),
            angle,
            Range(random, 0.58f, 0.72f)
        );
    }

    private static void BuildSidePosts(
        PuzzleDefinition puzzle,
        System.Random random)
    {
        float topY =
            Range(random, 0.95f, 1.30f);

        float postX =
            Range(random, 0.75f, 1.05f);

        float postY =
            Range(random, -0.55f, -0.15f);

        float postHeight =
            Range(random, 2.5f, 3.2f);

        AddPlankWithSymmetricScrews(
            puzzle,
            0,
            new Vector2(0f, topY),
            new Vector2(
                Range(random, 3.8f, 4.5f),
                0.52f
            ),
            0f,
            Range(random, 0.62f, 0.78f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            1,
            new Vector2(-postX, postY),
            new Vector2(
                0.52f,
                postHeight
            ),
            0f,
            Range(random, 0.60f, 0.74f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            2,
            new Vector2(postX, postY),
            new Vector2(
                0.52f,
                postHeight
            ),
            0f,
            Range(random, 0.60f, 0.74f)
        );
    }

    private static void BuildLayeredBars(
        PuzzleDefinition puzzle,
        System.Random random)
    {
        float topY =
            Range(random, 0.85f, 1.15f);

        float secondY =
            topY -
            Range(random, 0.65f, 0.95f);

        float thirdY =
            secondY -
            Range(random, 0.65f, 0.95f);

        float middleAngle =
            Range(random, 7f, 16f);

        AddPlankWithSymmetricScrews(
            puzzle,
            0,
            new Vector2(0f, topY),
            new Vector2(
                Range(random, 4.0f, 4.6f),
                0.52f
            ),
            0f,
            Range(random, 0.62f, 0.78f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            1,
            new Vector2(0f, secondY),
            new Vector2(
                Range(random, 3.4f, 4.0f),
                0.52f
            ),
            middleAngle,
            Range(random, 0.60f, 0.74f)
        );

        AddPlankWithSymmetricScrews(
            puzzle,
            2,
            new Vector2(0f, thirdY),
            new Vector2(
                Range(random, 2.8f, 3.5f),
                0.52f
            ),
            -middleAngle,
            Range(random, 0.58f, 0.72f)
        );
    }

    private static PuzzleDefinition GenerateStackCandidate(
        int levelNumber,
        int plankCount,
        int freeHoleCount,
        System.Random random)
    {
        PuzzleDefinition puzzle =
            new PuzzleDefinition();

        puzzle.LevelNumber =
            levelNumber;

        float topRowY = 3.15f;

        float spacing =
            1.4f /
            Mathf.Max(1, freeHoleCount - 1);

        float startX =
            -spacing *
            (freeHoleCount - 1) *
            0.5f;

        for (int i = 0;
             i < freeHoleCount;
             i++)
        {
            AddHole(
                puzzle,
                i,
                new Vector2(
                    startX + spacing * i,
                    topRowY
                ),
                true
            );
        }

        for (int plankIndex = 0;
             plankIndex < plankCount;
             plankIndex++)
        {
            float y =
                1.3f -
                plankIndex *
                Range(random, 0.72f, 0.92f);

            float width =
                Mathf.Max(
                    2.4f,
                    4.7f -
                    plankIndex * 0.45f
                );

            float rotation =
                plankIndex == 0
                    ? 0f
                    : Range(random, -14f, 14f);

            AddPlankWithSymmetricScrews(
                puzzle,
                plankIndex,
                new Vector2(0f, y),
                new Vector2(width, 0.52f),
                rotation,
                Range(random, 0.60f, 0.76f)
            );
        }

        return puzzle;
    }

    private static void AddPlankWithSymmetricScrews(
        PuzzleDefinition puzzle,
        int plankId,
        Vector2 position,
        Vector2 size,
        float rotation,
        float screwPositionRatio)
    {
        int screwAId =
            puzzle.Screws.Count;

        int screwBId =
            screwAId + 1;

        int holeAId =
            puzzle.Holes.Count;

        int holeBId =
            holeAId + 1;

        Vector2 axis =
            size.x >= size.y
                ? Vector2.right
                : Vector2.up;

        float longSize =
            Mathf.Max(
                size.x,
                size.y
            );

        float halfDistance =
            longSize *
            0.5f *
            screwPositionRatio;

        Vector2 rotatedAxis =
            Rotate(
                axis,
                rotation
            );

        Vector2 holeAPosition =
            position -
            rotatedAxis *
            halfDistance;

        Vector2 holeBPosition =
            position +
            rotatedAxis *
            halfDistance;

        AddHole(
            puzzle,
            holeAId,
            holeAPosition,
            false
        );

        AddHole(
            puzzle,
            holeBId,
            holeBPosition,
            false
        );

        AddPlank(
            puzzle,
            plankId,
            position,
            size,
            rotation,
            screwAId,
            screwBId
        );

        AddScrew(
            puzzle,
            screwAId,
            plankId,
            holeAId
        );

        AddScrew(
            puzzle,
            screwBId,
            plankId,
            holeBId
        );
    }

    private static void AddHole(
        PuzzleDefinition puzzle,
        int id,
        Vector2 position,
        bool isFreeStartHole)
    {
        HoleDefinition hole =
            new HoleDefinition();

        hole.Id = id;
        hole.Position = position;
        hole.IsFreeStartHole =
            isFreeStartHole;

        puzzle.Holes.Add(hole);
    }

    private static void AddPlank(
        PuzzleDefinition puzzle,
        int id,
        Vector2 position,
        Vector2 size,
        float rotation,
        int screwAId,
        int screwBId)
    {
        PlankDefinition plank =
            new PlankDefinition();

        plank.Id = id;
        plank.Position = position;
        plank.Size = size;
        plank.Rotation = rotation;
        plank.ScrewAId = screwAId;
        plank.ScrewBId = screwBId;

        puzzle.Planks.Add(plank);
    }

    private static void AddScrew(
        PuzzleDefinition puzzle,
        int id,
        int ownerPlankId,
        int holeId)
    {
        ScrewDefinition screw =
            new ScrewDefinition();

        screw.Id = id;
        screw.OwnerPlankId =
            ownerPlankId;
        screw.OriginalHoleId =
            holeId;
        screw.CurrentHoleId =
            holeId;

        puzzle.Screws.Add(screw);
    }

    private static Vector2 Rotate(
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

    private static float Range(
        System.Random random,
        float minimum,
        float maximum)
    {
        return minimum +
            (float)random.NextDouble() *
            (maximum - minimum);
    }
}
