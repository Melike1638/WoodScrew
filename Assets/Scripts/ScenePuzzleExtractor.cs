using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aktif Unity sahnesini solver'ın PuzzleDefinition verisine çevirir.
/// Böylece "Mevcut Puzzle'ı Çöz" artık son üretilen RAM verisini değil,
/// gerçekten açık olan sahneyi test edebilir.
/// </summary>
public static class ScenePuzzleExtractor
{
    public static PuzzleDefinition ExtractActiveScene()
    {
        Scene scene =
            SceneManager.GetActiveScene();

        Hole[] holes =
            GetActiveSceneComponents<Hole>();

        Screw[] screws =
            GetActiveSceneComponents<Screw>();

        BoxCollider2D[] allBoxes =
            GetActiveSceneComponents<BoxCollider2D>();

        List<BoxCollider2D> plankBoxes =
            new List<BoxCollider2D>();

        for (int i = 0;
             i < allBoxes.Length;
             i++)
        {
            BoxCollider2D box =
                allBoxes[i];

            if (box == null)
                continue;

            if (!box.gameObject.name
                .StartsWith("Plank_"))
            {
                continue;
            }

            plankBoxes.Add(
                box
            );
        }

        if (holes.Length == 0 ||
            screws.Length == 0 ||
            plankBoxes.Count == 0)
        {
            Debug.LogError(
                "ScenePuzzleExtractor: aktif sahnede Hole, Screw veya Plank_XX bulunamadı."
            );

            return null;
        }

        PuzzleDefinition puzzle =
            new PuzzleDefinition();

        puzzle.LevelNumber =
            ParseSceneLevel(
                scene.name
            );

        Dictionary<Hole, int>
            holeIds =
                new Dictionary<Hole, int>();

        for (int i = 0;
             i < holes.Length;
             i++)
        {
            Hole hole =
                holes[i];

            int id =
                hole.DefinitionId >= 0
                    ? hole.DefinitionId
                    : i;

            while (ContainsHoleId(
                puzzle,
                id))
            {
                id++;
            }

            holeIds[hole] =
                id;

            HoleDefinition definition =
                new HoleDefinition();

            definition.Id =
                id;

            definition.Position =
                hole.transform.position;

            definition.IsFreeStartHole =
                hole.IsStorageHole ||
                hole.OccupyingScrew == null;

            puzzle.Holes.Add(
                definition
            );
        }

        Dictionary<Screw, int>
            screwIds =
                new Dictionary<Screw, int>();

        for (int i = 0;
             i < screws.Length;
             i++)
        {
            Screw screw =
                screws[i];

            screwIds[screw] =
                i;

            Hole currentHole =
                screw.CurrentHole;

            if (currentHole == null)
            {
                currentHole =
                    FindNearestHole(
                        holes,
                        screw.transform.position
                    );
            }

            if (currentHole == null ||
                !holeIds.ContainsKey(
                    currentHole))
            {
                continue;
            }

            ScrewDefinition definition =
                new ScrewDefinition();

            definition.Id =
                i;

            definition.OwnerPlankId =
                -1;

            definition.OriginalHoleId =
                holeIds[
                    currentHole];

            definition.CurrentHoleId =
                holeIds[
                    currentHole];

            puzzle.Screws.Add(
                definition
            );
        }

        for (int plankIndex = 0;
             plankIndex <
                plankBoxes.Count;
             plankIndex++)
        {
            BoxCollider2D box =
                plankBoxes[
                    plankIndex];

            PlankDefinition plank =
                new PlankDefinition();

            plank.Id =
                plankIndex;

            Vector3 center =
                box.transform.TransformPoint(
                    box.offset
                );

            plank.Position =
                new Vector2(
                    center.x,
                    center.y
                );

            plank.Size =
                Vector2.Scale(
                    box.size,
                    new Vector2(
                        Mathf.Abs(
                            box.transform
                                .lossyScale.x),
                        Mathf.Abs(
                            box.transform
                                .lossyScale.y)
                    )
                );

            plank.Rotation =
                box.transform.eulerAngles.z;

            List<int> supportingScrews =
                new List<int>();

            for (int s = 0;
                 s < screws.Length;
                 s++)
            {
                Screw screw =
                    screws[s];

                if (screw == null ||
                    !screwIds.ContainsKey(
                        screw))
                {
                    continue;
                }

                if (PointInsideBox(
                        box,
                        screw.transform.position,
                        0.08f))
                {
                    supportingScrews.Add(
                        screwIds[screw]
                    );
                }
            }

            plank.ScrewAId =
                supportingScrews.Count > 0
                    ? supportingScrews[0]
                    : -1;

            plank.ScrewBId =
                supportingScrews.Count > 1
                    ? supportingScrews[1]
                    : -1;

            plank.ScrewCId =
                supportingScrews.Count > 2
                    ? supportingScrews[2]
                    : -1;

            puzzle.Planks.Add(
                plank
            );

            for (int s = 0;
                 s < supportingScrews.Count;
                 s++)
            {
                int screwId =
                    supportingScrews[s];

                for (int d = 0;
                     d < puzzle.Screws.Count;
                     d++)
                {
                    if (puzzle.Screws[d].Id !=
                        screwId)
                    {
                        continue;
                    }

                    if (puzzle.Screws[d]
                            .OwnerPlankId < 0)
                    {
                        puzzle.Screws[d]
                            .OwnerPlankId =
                            plank.Id;
                    }

                    break;
                }
            }
        }

        return puzzle;
    }

    private static bool PointInsideBox(
        BoxCollider2D box,
        Vector2 worldPoint,
        float margin)
    {
        Vector3 local3 =
            box.transform
                .InverseTransformPoint(
                    worldPoint
                );

        Vector2 local =
            new Vector2(
                local3.x,
                local3.y
            ) -
            box.offset;

        Vector2 half =
            box.size * 0.5f;

        return
            Mathf.Abs(local.x) <=
                half.x + margin &&
            Mathf.Abs(local.y) <=
                half.y + margin;
    }

    private static Hole FindNearestHole(
        Hole[] holes,
        Vector2 worldPosition)
    {
        Hole best =
            null;

        float bestDistance =
            float.MaxValue;

        for (int i = 0;
             i < holes.Length;
             i++)
        {
            Hole hole =
                holes[i];

            if (hole == null)
                continue;

            float distance =
                Vector2.Distance(
                    hole.transform.position,
                    worldPosition
                );

            if (distance <
                bestDistance)
            {
                bestDistance =
                    distance;

                best =
                    hole;
            }
        }

        return
            bestDistance <= 0.35f
                ? best
                : null;
    }

    private static bool ContainsHoleId(
        PuzzleDefinition puzzle,
        int id)
    {
        for (int i = 0;
             i < puzzle.Holes.Count;
             i++)
        {
            if (puzzle.Holes[i].Id ==
                id)
            {
                return true;
            }
        }

        return false;
    }

    private static int ParseSceneLevel(
        string sceneName)
    {
        if (string.IsNullOrEmpty(
                sceneName))
        {
            return 1;
        }

        string digits =
            "";

        for (int i = 0;
             i < sceneName.Length;
             i++)
        {
            char c =
                sceneName[i];

            if (char.IsDigit(c))
            {
                digits +=
                    c;
            }
        }

        int parsed;

        if (int.TryParse(
                digits,
                out parsed))
        {
            return
                Mathf.Max(
                    1,
                    parsed
                );
        }

        return 1;
    }

    private static T[] GetActiveSceneComponents<T>()
        where T : Component
    {
        Scene activeScene =
            SceneManager.GetActiveScene();

        T[] all =
            Resources.FindObjectsOfTypeAll<T>();

        List<T> result =
            new List<T>();

        for (int i = 0;
             i < all.Length;
             i++)
        {
            T component =
                all[i];

            if (component == null)
                continue;

            GameObject go =
                component.gameObject;

            if (go == null ||
                !go.scene.IsValid() ||
                !go.scene.isLoaded ||
                go.scene != activeScene)
            {
                continue;
            }

            result.Add(
                component
            );
        }

        return result.ToArray();
    }

}
