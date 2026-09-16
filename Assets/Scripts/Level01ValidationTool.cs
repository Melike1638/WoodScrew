using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tutorial_02 -> Level_01 validation scene.
///
/// IMPORTANT:
/// Hole_02 is NOT placed on top of Plank_01 anymore.
///
/// The tool finds a point that:
/// 1) is outside Plank_01 at the initial fixed pose,
/// 2) is inside Plank_01 during the LAST part of its one-screw swing,
/// 3) stays inside the plank near the settled swing pose,
/// 4) is away from screws and other holes.
///
/// This gives us the exact test we want:
/// the hole starts as an independent target, then Plank_01 swings over it
/// and blocks it while the last screw is still attached.
/// </summary>
public static class Level01ValidationTool
{
    private const string TutorialPath =
        "Assets/Scenes/Tutorial_02.unity";

    private const string LevelPath =
        "Assets/Scenes/Level_01.unity";

    private const float MinimumScrewDistance =
        0.34f;

    private const float MinimumOtherHoleDistance =
        0.22f;

    private const float OutsideStartMargin =
        0.07f;

    private const float InsidePoseInset =
        0.08f;

    private const int GridX =
        28;

    private const int GridY =
        16;


    [MenuItem(
        "Tools/Puzzle Validation/Create Level_01 From Tutorial_02")]
    public static void CreateLevel01()
    {
        SceneAsset tutorial =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                TutorialPath
            );

        if (tutorial == null)
        {
            Debug.LogError(
                "Tutorial_02 bulunamadı: " +
                TutorialPath
            );

            return;
        }

        BackupExistingLevel01();

        if (!AssetDatabase.CopyAsset(
                TutorialPath,
                LevelPath))
        {
            Debug.LogError(
                "Tutorial_02 -> Level_01 kopyalanamadı."
            );

            return;
        }

        AssetDatabase.Refresh();

        Scene scene =
            EditorSceneManager.OpenScene(
                LevelPath,
                OpenSceneMode.Single
            );

        GameObject plankObject =
            GameObject.Find(
                "Plank_01"
            );

        GameObject holeObject =
            GameObject.Find(
                "Hole_02"
            );

        if (plankObject == null ||
            holeObject == null)
        {
            Debug.LogError(
                "Level_01 içinde Plank_01 veya Hole_02 bulunamadı."
            );

            return;
        }

        BoxCollider2D plankBox =
            plankObject.GetComponent
                <BoxCollider2D>();

        if (plankBox == null)
        {
            Debug.LogError(
                "Plank_01 üzerinde BoxCollider2D yok."
            );

            return;
        }

        PuzzleDefinition puzzle =
            ScenePuzzleExtractor
                .ExtractActiveScene();

        if (puzzle == null)
        {
            Debug.LogError(
                "Aktif Level_01 sahnesi solver verisine çevrilemedi."
            );

            return;
        }

        PlankDefinition plank =
            FindNearestPlank(
                puzzle,
                GetBoxWorldCenter(
                    plankBox
                )
            );

        if (plank == null)
        {
            Debug.LogError(
                "Plank_01 PuzzleDefinition içinde bulunamadı."
            );

            return;
        }

        Screw[] screws =
            GetActiveSceneComponents<Screw>();

        Hole[] holes =
            GetActiveSceneComponents<Hole>();

        List<int> pivotIds =
            GetPlankScrewIds(
                plank
            );

        bool found =
            false;

        Vector2 bestWorld =
            Vector2.zero;

        float bestScore =
            float.NegativeInfinity;

        int bestPivotId =
            -1;

        float bestSwingDelta =
            0f;

        for (int i = 0;
             i < pivotIds.Count;
             i++)
        {
            int pivotId =
                pivotIds[i];

            ScrewDefinition pivotScrew =
                FindScrew(
                    puzzle,
                    pivotId
                );

            if (pivotScrew == null)
                continue;

            HoleDefinition pivotHole =
                FindHole(
                    puzzle,
                    pivotScrew.CurrentHoleId
                );

            if (pivotHole == null)
                continue;

            PuzzlePhysicsModel.PlankPose finalPose =
                PuzzlePhysicsModel
                    .GetCollisionAwareSwingPose(
                        puzzle,
                        plank,
                        pivotId
                    );

            float delta =
                Mathf.DeltaAngle(
                    plank.Rotation,
                    finalPose.Rotation
                );

            if (Mathf.Abs(delta) <
                8f)
            {
                continue;
            }

            Vector2 candidate;
            float score;

            if (!FindTrueSwingArcPoint(
                    plankBox,
                    plank,
                    pivotHole.Position,
                    delta,
                    screws,
                    holes,
                    holeObject,
                    out candidate,
                    out score))
            {
                continue;
            }

            float totalScore =
                score +
                Mathf.Abs(delta) *
                0.02f;

            if (!found ||
                totalScore >
                bestScore)
            {
                found =
                    true;

                bestWorld =
                    candidate;

                bestScore =
                    totalScore;

                bestPivotId =
                    pivotId;

                bestSwingDelta =
                    Mathf.Abs(delta);
            }
        }

        if (!found)
        {
            Debug.LogError(
                "Hole_02 için gerçek swing-arc noktası bulunamadı. " +
                "Bu Tutorial_02 geometrisinde başlangıçta plank dışında olup, " +
                "tek vida swing'inin son bölümünde plank altında kalan temiz bir alan yok."
            );

            return;
        }

        Undo.RecordObject(
            holeObject.transform,
            "Place Hole_02 in true swing arc"
        );

        holeObject.transform.position =
            new Vector3(
                bestWorld.x,
                bestWorld.y,
                holeObject.transform.position.z
            );

        // Hole kendi başına görünür olsun.
        // Başlangıçta plank üstünde olmadığı için artık
        // renderer order ile hile yapmıyoruz.
        SpriteRenderer holeRenderer =
            holeObject.GetComponent
                <SpriteRenderer>();

        if (holeRenderer != null)
        {
            holeRenderer.enabled =
                true;
        }

        EditorUtility.SetDirty(
            holeObject.transform
        );

        EditorSceneManager.MarkSceneDirty(
            scene
        );

        EditorSceneManager.SaveScene(
            scene
        );

        Selection.activeGameObject =
            holeObject;

        Debug.Log(
            "Level_01 doğru test düzeniyle oluşturuldu. " +
            "Hole_02 başlangıçta Plank_01'in DIŞINDA; " +
            "Plank_01 tek vida üzerinde sallanınca deliğin ÜSTÜNE geliyor ve orada kalıyor. " +
            "Pivot Screw ID: " +
            bestPivotId +
            " | Swing delta: " +
            bestSwingDelta.ToString("0.0") +
            "°. Önce Play ile kontrol et, sonra Puzzle Generator > " +
            "MEVCUT PUZZLE'I ÇÖZ (AKTİF SAHNE)."
        );
    }


    private static bool FindTrueSwingArcPoint(
        BoxCollider2D box,
        PlankDefinition plank,
        Vector2 pivotWorld,
        float totalDelta,
        Screw[] screws,
        Hole[] holes,
        GameObject targetHole,
        out Vector2 bestWorld,
        out float bestScore)
    {
        bestWorld =
            Vector2.zero;

        bestScore =
            float.NegativeInfinity;

        Vector2 worldSize =
            GetBoxWorldSize(
                box
            );

        Vector2 initialCenter =
            GetBoxWorldCenter(
                box
            );

        // Son swing pozunun merkezini pivot etrafında
        // rigid-body dönüşüyle hesapla.
        Vector2 initialRadius =
            initialCenter -
            pivotWorld;

        float[] fractions =
        {
            0.72f,
            0.86f,
            1.00f
        };

        // Candidate'ları son pozdaki plank dikdörtgeni
        // içinde tarıyoruz.
        float finalAngle =
            plank.Rotation +
            totalDelta;

        Vector2 finalCenter =
            pivotWorld +
            Rotate(
                initialRadius,
                totalDelta
            );

        Vector2 half =
            worldSize * 0.5f;

        for (int ix = 0;
             ix <= GridX;
             ix++)
        {
            float tx =
                (float)ix /
                GridX;

            float localX =
                Mathf.Lerp(
                    -half.x +
                    InsidePoseInset,
                    half.x -
                    InsidePoseInset,
                    tx
                );

            for (int iy = 0;
                 iy <= GridY;
                 iy++)
            {
                float ty =
                    (float)iy /
                    GridY;

                float localY =
                    Mathf.Lerp(
                        -half.y +
                        InsidePoseInset,
                        half.y -
                        InsidePoseInset,
                        ty
                    );

                Vector2 world =
                    finalCenter +
                    Rotate(
                        new Vector2(
                            localX,
                            localY
                        ),
                        finalAngle
                    );

                // KRİTİK:
                // Başlangıçta hole plank üstünde olmayacak.
                if (PointInsidePose(
                        world,
                        initialCenter,
                        plank.Rotation,
                        worldSize,
                        -OutsideStartMargin))
                {
                    continue;
                }

                bool coveredLateSwing =
                    true;

                for (int f = 0;
                     f < fractions.Length;
                     f++)
                {
                    float fraction =
                        fractions[f];

                    float angleDelta =
                        totalDelta *
                        fraction;

                    float poseAngle =
                        plank.Rotation +
                        angleDelta;

                    Vector2 poseCenter =
                        pivotWorld +
                        Rotate(
                            initialRadius,
                            angleDelta
                        );

                    if (!PointInsidePose(
                            world,
                            poseCenter,
                            poseAngle,
                            worldSize,
                            InsidePoseInset))
                    {
                        coveredLateSwing =
                            false;

                        break;
                    }
                }

                if (!coveredLateSwing)
                    continue;

                float screwDistance =
                    NearestScrewDistance(
                        world,
                        screws
                    );

                if (screwDistance <
                    MinimumScrewDistance)
                {
                    continue;
                }

                float holeDistance =
                    NearestOtherHoleDistance(
                        world,
                        holes,
                        targetHole
                    );

                if (holeDistance <
                    MinimumOtherHoleDistance)
                {
                    continue;
                }

                float initialPlankDistance =
                    DistanceOutsideRectangle(
                        world,
                        initialCenter,
                        plank.Rotation,
                        worldSize
                    );

                // Tercih:
                // başlangıç plankından net ayrılmış,
                // vidalardan/diğer deliklerden uzak nokta.
                float score =
                    initialPlankDistance *
                    2.4f +
                    screwDistance *
                    1.5f +
                    holeDistance *
                    0.5f;

                if (score >
                    bestScore)
                {
                    bestScore =
                        score;

                    bestWorld =
                        world;
                }
            }
        }

        return
            bestScore >
            float.NegativeInfinity;
    }


    private static float DistanceOutsideRectangle(
        Vector2 world,
        Vector2 center,
        float rotation,
        Vector2 size)
    {
        Vector2 local =
            Rotate(
                world - center,
                -rotation
            );

        Vector2 half =
            size * 0.5f;

        float dx =
            Mathf.Max(
                Mathf.Abs(local.x) -
                half.x,
                0f
            );

        float dy =
            Mathf.Max(
                Mathf.Abs(local.y) -
                half.y,
                0f
            );

        return
            Mathf.Sqrt(
                dx * dx +
                dy * dy
            );
    }


    private static bool PointInsidePose(
        Vector2 world,
        Vector2 center,
        float rotation,
        Vector2 size,
        float inset)
    {
        Vector2 local =
            Rotate(
                world - center,
                -rotation
            );

        Vector2 half =
            size * 0.5f;

        half.x =
            Mathf.Max(
                0f,
                half.x -
                inset
            );

        half.y =
            Mathf.Max(
                0f,
                half.y -
                inset
            );

        return
            Mathf.Abs(local.x) <=
                half.x &&
            Mathf.Abs(local.y) <=
                half.y;
    }


    private static Vector2 GetBoxWorldCenter(
        BoxCollider2D box)
    {
        Vector3 world =
            box.transform.TransformPoint(
                box.offset
            );

        return
            new Vector2(
                world.x,
                world.y
            );
    }


    private static Vector2 GetBoxWorldSize(
        BoxCollider2D box)
    {
        Vector3 scale =
            box.transform.lossyScale;

        return
            Vector2.Scale(
                box.size,
                new Vector2(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y)
                )
            );
    }


    private static float NearestScrewDistance(
        Vector2 point,
        Screw[] screws)
    {
        float best =
            float.MaxValue;

        for (int i = 0;
             i < screws.Length;
             i++)
        {
            Screw screw =
                screws[i];

            if (screw == null)
                continue;

            float d =
                Vector2.Distance(
                    point,
                    screw.transform.position
                );

            if (d < best)
            {
                best =
                    d;
            }
        }

        return best;
    }


    private static float NearestOtherHoleDistance(
        Vector2 point,
        Hole[] holes,
        GameObject targetHole)
    {
        float best =
            float.MaxValue;

        for (int i = 0;
             i < holes.Length;
             i++)
        {
            Hole hole =
                holes[i];

            if (hole == null ||
                hole.gameObject ==
                    targetHole)
            {
                continue;
            }

            float d =
                Vector2.Distance(
                    point,
                    hole.transform.position
                );

            if (d < best)
            {
                best =
                    d;
            }
        }

        return best;
    }


    private static PlankDefinition FindNearestPlank(
        PuzzleDefinition puzzle,
        Vector2 center)
    {
        PlankDefinition best =
            null;

        float bestDistance =
            float.MaxValue;

        for (int i = 0;
             i < puzzle.Planks.Count;
             i++)
        {
            PlankDefinition plank =
                puzzle.Planks[i];

            float d =
                Vector2.Distance(
                    plank.Position,
                    center
                );

            if (d < bestDistance)
            {
                bestDistance =
                    d;

                best =
                    plank;
            }
        }

        return
            bestDistance <= 0.30f
                ? best
                : null;
    }


    private static List<int> GetPlankScrewIds(
        PlankDefinition plank)
    {
        List<int> ids =
            new List<int>();

        int[] source =
        {
            plank.ScrewAId,
            plank.ScrewBId,
            plank.ScrewCId
        };

        for (int i = 0;
             i < source.Length;
             i++)
        {
            if (source[i] >= 0 &&
                !ids.Contains(
                    source[i]))
            {
                ids.Add(
                    source[i]
                );
            }
        }

        return ids;
    }


    private static ScrewDefinition FindScrew(
        PuzzleDefinition puzzle,
        int id)
    {
        for (int i = 0;
             i < puzzle.Screws.Count;
             i++)
        {
            if (puzzle.Screws[i].Id ==
                id)
            {
                return
                    puzzle.Screws[i];
            }
        }

        return null;
    }


    private static HoleDefinition FindHole(
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
                return
                    puzzle.Holes[i];
            }
        }

        return null;
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


    private static void BackupExistingLevel01()
    {
        SceneAsset existing =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                LevelPath
            );

        if (existing == null)
            return;

        string backup =
            "Assets/Scenes/Level_01_Backup_" +
            DateTime.Now.ToString(
                "yyyyMMdd_HHmmss") +
            ".unity";

        AssetDatabase.CopyAsset(
            LevelPath,
            backup
        );

        AssetDatabase.DeleteAsset(
            LevelPath
        );
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
