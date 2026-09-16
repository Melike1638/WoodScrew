using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wood Screw oyununun kilitlenmiş GAMEPLAY fizik sözleşmesi.
///
/// Bu sınıf "hissettirme" değerlerini yönetmez:
/// gravity, damping, animation speed, görsel boyut vb. runtime tarafında kalır.
///
/// Generator / solver için değişmemesi gereken kurallar burada temsil edilir:
/// - 2+ support = fixed
/// - 1 support = pivot/swing
/// - 0 support = removed/fall
/// - vida ancak gerçek plank socket'i ile anchor hizalıysa support olur
/// - solid plank bir anchor'ı kapatıyorsa, o noktada gerçek socket yoksa vida geçemez
/// - shared screw aynı anchor'da birden fazla plank socket'ini destekleyebilir
/// </summary>
public static class PuzzleRuleContract
{
    // Runtime'daki mevcut değerlerle eşleştirildi.
    public const float SupportAlignmentTolerance = 0.025f;
    public const float InsertAlignmentTolerance = 0.025f;
    public const float AnchorClearanceRadius = 0.18f;
    public const float RuntimePlankHoleMargin = 0.22f;
    public const float ScrewCollisionRadius = 0.25f;

    public static int[] GetPlankScrewIds(
        PlankDefinition plank)
    {
        return new int[]
        {
            plank.ScrewAId,
            plank.ScrewBId,
            plank.ScrewCId
        };
    }

    public static bool PlankUsesScrew(
        PlankDefinition plank,
        int screwId)
    {
        return
            plank.ScrewAId == screwId ||
            plank.ScrewBId == screwId ||
            plank.ScrewCId == screwId;
    }

    public static Vector2 GetEffectivePlankSize(
        PuzzleDefinition puzzle,
        PlankDefinition plank)
    {
        Vector2 size =
            plank.Size;

        float requiredHalfX =
            size.x * 0.5f;

        float requiredHalfY =
            size.y * 0.5f;

        int[] screwIds =
            GetPlankScrewIds(plank);

        for (int i = 0;
             i < screwIds.Length;
             i++)
        {
            int screwId =
                screwIds[i];

            if (screwId < 0)
                continue;

            ScrewDefinition screw =
                FindScrew(
                    puzzle,
                    screwId
                );

            if (screw == null)
                continue;

            HoleDefinition hole =
                FindHole(
                    puzzle,
                    screw.OriginalHoleId
                );

            if (hole == null)
                continue;

            Vector2 local =
                WorldToLocal(
                    hole.Position,
                    plank.Position,
                    plank.Rotation
                );

            requiredHalfX =
                Mathf.Max(
                    requiredHalfX,
                    Mathf.Abs(local.x) +
                    RuntimePlankHoleMargin
                );

            requiredHalfY =
                Mathf.Max(
                    requiredHalfY,
                    Mathf.Abs(local.y) +
                    RuntimePlankHoleMargin
                );
        }

        return new Vector2(
            requiredHalfX * 2f,
            requiredHalfY * 2f
        );
    }

    public static int GetSupportCount(
        PuzzleDefinition puzzle,
        int[] screwHoleIds,
        Vector2[] plankPositions,
        float[] plankRotations,
        int removedPlankMask,
        int plankIndex)
    {
        return
            GetSupportingScrewIds(
                puzzle,
                screwHoleIds,
                plankPositions,
                plankRotations,
                removedPlankMask,
                plankIndex
            ).Count;
    }

    public static int GetSingleSupportingScrewId(
        PuzzleDefinition puzzle,
        int[] screwHoleIds,
        Vector2[] plankPositions,
        float[] plankRotations,
        int removedPlankMask,
        int plankIndex)
    {
        List<int> supports =
            GetSupportingScrewIds(
                puzzle,
                screwHoleIds,
                plankPositions,
                plankRotations,
                removedPlankMask,
                plankIndex
            );

        if (supports.Count != 1)
            return -1;

        return supports[0];
    }

    public static List<int> GetSupportingScrewIds(
        PuzzleDefinition puzzle,
        int[] screwHoleIds,
        Vector2[] plankPositions,
        float[] plankRotations,
        int removedPlankMask,
        int plankIndex)
    {
        List<int> result =
            new List<int>();

        if (plankIndex < 0 ||
            plankIndex >=
                puzzle.Planks.Count)
        {
            return result;
        }

        if (IsPlankRemoved(
                removedPlankMask,
                plankIndex))
        {
            return result;
        }

        PlankDefinition plank =
            puzzle.Planks[
                plankIndex];

        for (int screwIndex = 0;
             screwIndex <
                puzzle.Screws.Count;
             screwIndex++)
        {
            int currentHoleId =
                screwHoleIds[
                    screwIndex];

            HoleDefinition anchor =
                FindHole(
                    puzzle,
                    currentHoleId
                );

            if (anchor == null)
                continue;

            if (!PlankHasSocketAtWorldPosition(
                    puzzle,
                    plank,
                    plankPositions[plankIndex],
                    plankRotations[plankIndex],
                    anchor.Position,
                    SupportAlignmentTolerance))
            {
                continue;
            }

            int screwId =
                puzzle.Screws[
                    screwIndex].Id;

            if (!result.Contains(
                    screwId))
            {
                result.Add(
                    screwId
                );
            }
        }

        return result;
    }

    public static bool CanInsertScrewAtHole(
        PuzzleDefinition puzzle,
        int[] screwHoleIds,
        Vector2[] plankPositions,
        float[] plankRotations,
        int removedPlankMask,
        int targetHoleId)
    {
        HoleDefinition targetHole =
            FindHole(
                puzzle,
                targetHoleId
            );

        if (targetHole == null)
            return false;

        Vector2 worldPosition =
            targetHole.Position;

        for (int plankIndex = 0;
             plankIndex <
                puzzle.Planks.Count;
             plankIndex++)
        {
            if (IsPlankRemoved(
                    removedPlankMask,
                    plankIndex))
            {
                continue;
            }

            PlankDefinition plank =
                puzzle.Planks[
                    plankIndex];

            bool socketAligned =
                PlankHasSocketAtWorldPosition(
                    puzzle,
                    plank,
                    plankPositions[plankIndex],
                    plankRotations[plankIndex],
                    worldPosition,
                    InsertAlignmentTolerance
                );

            if (socketAligned)
            {
                continue;
            }

            Vector2 effectiveSize =
                GetEffectivePlankSize(
                    puzzle,
                    plank
                );

            if (RectangleBlocksInsertionCircle(
                    worldPosition,
                    plankPositions[plankIndex],
                    plankRotations[plankIndex],
                    effectiveSize,
                    AnchorClearanceRadius))
            {
                return false;
            }
        }

        return true;
    }

    public static bool PlankHasSocketAtWorldPosition(
        PuzzleDefinition puzzle,
        PlankDefinition plank,
        Vector2 currentPlankPosition,
        float currentPlankRotation,
        Vector2 worldPosition,
        float tolerance)
    {
        int[] screwIds =
            GetPlankScrewIds(plank);

        for (int i = 0;
             i < screwIds.Length;
             i++)
        {
            int screwId =
                screwIds[i];

            if (screwId < 0)
                continue;

            ScrewDefinition screw =
                FindScrew(
                    puzzle,
                    screwId
                );

            if (screw == null)
                continue;

            HoleDefinition originalHole =
                FindHole(
                    puzzle,
                    screw.OriginalHoleId
                );

            if (originalHole == null)
                continue;

            Vector2 socketLocal =
                WorldToLocal(
                    originalHole.Position,
                    plank.Position,
                    plank.Rotation
                );

            Vector2 socketWorld =
                currentPlankPosition +
                Rotate(
                    socketLocal,
                    currentPlankRotation
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

    public static bool IsPlankRemoved(
        int mask,
        int plankIndex)
    {
        if (plankIndex < 0)
            return false;

        return
            (mask &
             (1 << plankIndex)) != 0;
    }

    private static bool RectangleBlocksInsertionCircle(
        Vector2 circleCenter,
        Vector2 plankPosition,
        float plankRotation,
        Vector2 plankSize,
        float circleRadius)
    {
        Vector2 local =
            WorldToLocal(
                circleCenter,
                plankPosition,
                plankRotation
            );

        Vector2 half =
            plankSize * 0.5f;

        float closestX =
            Mathf.Clamp(
                local.x,
                -half.x,
                half.x
            );

        float closestY =
            Mathf.Clamp(
                local.y,
                -half.y,
                half.y
            );

        Vector2 closest =
            new Vector2(
                closestX,
                closestY
            );

        float distanceSquared =
            (local - closest)
                .sqrMagnitude;

        return
            distanceSquared <
            circleRadius *
            circleRadius;
    }

    private static Vector2 WorldToLocal(
        Vector2 world,
        Vector2 origin,
        float rotation)
    {
        return Rotate(
            world - origin,
            -rotation
        );
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

    private static ScrewDefinition FindScrew(
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
                return
                    puzzle.Screws[i];
            }
        }

        return null;
    }

    private static HoleDefinition FindHole(
        PuzzleDefinition puzzle,
        int holeId)
    {
        for (int i = 0;
             i < puzzle.Holes.Count;
             i++)
        {
            if (puzzle.Holes[i].Id ==
                holeId)
            {
                return
                    puzzle.Holes[i];
            }
        }

        return null;
    }
}
