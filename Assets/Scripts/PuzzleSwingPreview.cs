using UnityEngine;
using UnityEditor;

public static class PuzzleSwingPreview
{
    public static void Draw(
        Rect board,
        PuzzleDefinition puzzle)
    {
        if (puzzle == null)
            return;

        foreach (PlankDefinition plank
                 in puzzle.Planks)
        {
            DrawPivotPreview(
                board,
                puzzle,
                plank,
                plank.ScrewAId
            );

            DrawPivotPreview(
                board,
                puzzle,
                plank,
                plank.ScrewBId
            );
        }
    }

    private static void DrawPivotPreview(
        Rect board,
        PuzzleDefinition puzzle,
        PlankDefinition plank,
        int pivotScrewId)
    {
        PuzzlePhysicsModel.PlankPose pose =
            PuzzlePhysicsModel
                .GetCollisionAwareSwingPose(
                    puzzle,
                    plank,
                    pivotScrewId
                );

        Vector2 center =
            WorldToPreview(
                board,
                pose.Position
            );

        float width =
            plank.Size.x * 55f;

        float height =
            plank.Size.y * 55f;

        Vector2 right =
            Quaternion.Euler(
                0f,
                0f,
                -pose.Rotation
            ) *
            Vector2.right;

        Vector2 up =
            Quaternion.Euler(
                0f,
                0f,
                -pose.Rotation
            ) *
            Vector2.up;

        Vector2 halfRight =
            right *
            width *
            0.5f;

        Vector2 halfUp =
            up *
            height *
            0.5f;

        Vector3[] points =
        {
            center - halfRight - halfUp,
            center + halfRight - halfUp,
            center + halfRight + halfUp,
            center - halfRight + halfUp
        };

        Handles.BeginGUI();

        Color previousColor =
            Handles.color;

        if (pose.Blocked)
        {
            Handles.color =
                new Color(
                    0.95f,
                    0.25f,
                    0.15f,
                    0.42f
                );
        }
        else
        {
            Handles.color =
                new Color(
                    0.15f,
                    0.55f,
                    0.95f,
                    0.35f
                );
        }

        Handles.DrawAAConvexPolygon(
            points
        );

        Handles.color =
            previousColor;

        Handles.EndGUI();
    }

    private static Vector2 WorldToPreview(
        Rect board,
        Vector2 worldPosition)
    {
        float x =
            Mathf.InverseLerp(
                -3.5f,
                3.5f,
                worldPosition.x
            );

        float y =
            Mathf.InverseLerp(
                4.0f,
                -4.0f,
                worldPosition.y
            );

        return new Vector2(
            Mathf.Lerp(
                board.x + 20,
                board.xMax - 20,
                x
            ),
            Mathf.Lerp(
                board.y + 20,
                board.yMax - 20,
                y
            )
        );
    }
}