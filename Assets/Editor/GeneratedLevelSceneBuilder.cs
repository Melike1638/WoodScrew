using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GeneratedLevelSceneBuilder
{
    [MenuItem(
        "Tools/Generated Levels/Test Level 1")]
    public static void TestLevel1()
    {
        PuzzleDefinition puzzle =
            LoadLevel1();

        if (puzzle == null)
            return;

        Debug.Log(
            "LEVEL 1 OKUNDU | " +
            "Tahta: " +
            puzzle.Planks.Count +
            " | Vida: " +
            puzzle.Screws.Count +
            " | Delik: " +
            puzzle.Holes.Count
        );
    }


    [MenuItem(
        "Tools/Generated Levels/Build Level 1 Preview")]
    public static void BuildLevel1Preview()
    {
        PuzzleDefinition puzzle =
            LoadLevel1();

        if (puzzle == null)
            return;

        if (!EditorSceneManager
                .SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string scenePath =
            "Assets/Scenes/" +
            "Generated_Level_01_Preview.unity";

        Scene scene =
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );


        GameObject cameraObject =
            new GameObject(
                "Main Camera"
            );

        Camera camera =
            cameraObject
                .AddComponent<Camera>();

        camera.orthographic =
            true;

        camera.orthographicSize =
            4.5f;

        camera.clearFlags =
            CameraClearFlags.SolidColor;

        camera.backgroundColor =
            new Color(
                0.25f,
                0.32f,
                0.42f
            );

        cameraObject.tag =
            "MainCamera";

        cameraObject.transform.position =
            new Vector3(
                0f,
                0f,
                -10f
            );


        GameObject root =
            new GameObject(
                "Generated_Level_01"
            );


        Dictionary<int, GameObject>
            plankObjects =
                new Dictionary
                    <int, GameObject>();

        Dictionary<int, GeneratedPlankPhysics>
            plankPhysics =
                new Dictionary
                    <int, GeneratedPlankPhysics>();

        Dictionary<int, Hole>
            holesByDefinitionId =
                new Dictionary<int, Hole>();

        List<Hole> allRuntimeHoles =
            new List<Hole>();


        // -------------------------
        // PLANKS
        // -------------------------

        for (int i = 0;
             i < puzzle.Planks.Count;
             i++)
        {
            PlankDefinition plank =
                puzzle.Planks[i];

            Vector2 runtimeSize =
                GetRuntimePlankSize(
                    puzzle,
                    plank
                );

            GameObject plankRoot =
                new GameObject(
                    "Plank_" +
                    (i + 1)
                        .ToString("00")
                );

            plankRoot.transform.SetParent(
                root.transform
            );

            // Tahtanın BoxCollider2D'si fizik için aktif kalır
            // ama mouse tıklamasını yutmasın.
            // Böylece tahta üstündeki gerçek boş deliğe
            // tıklama doğrudan Hole collider'ına ulaşır.
            plankRoot.layer =
                2; // Ignore Raycast

            plankRoot.transform.position =
                new Vector3(
                    plank.Position.x,
                    plank.Position.y,
                    0f
                );

            plankRoot.transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    plank.Rotation
                );


            GameObject visual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Quad
                );

            visual.name =
                "Visual";

            visual.transform.SetParent(
                plankRoot.transform,
                false
            );

            visual.transform.localPosition =
                Vector3.zero;

            visual.transform.localRotation =
                Quaternion.identity;

            visual.transform.localScale =
                new Vector3(
                    runtimeSize.x,
                    runtimeSize.y,
                    1f
                );

            MeshCollider oldCollider =
                visual.GetComponent
                    <MeshCollider>();

            if (oldCollider != null)
            {
                Object.DestroyImmediate(
                    oldCollider
                );
            }

            MeshRenderer renderer =
                visual.GetComponent
                    <MeshRenderer>();

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            Material material =
                new Material(
                    shader
                );

            material.SetColor(
                "_BaseColor",
                new Color(
                    0.34f,
                    0.62f,
                    0.28f
                )
            );

            renderer.sharedMaterial =
                material;


            BoxCollider2D boxCollider =
                plankRoot.AddComponent
                    <BoxCollider2D>();

            boxCollider.isTrigger =
                false;

            boxCollider.size =
                runtimeSize;


            Rigidbody2D body =
                plankRoot.AddComponent
                    <Rigidbody2D>();

            body.bodyType =
                RigidbodyType2D.Kinematic;

            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            body.interpolation =
                RigidbodyInterpolation2D.Interpolate;


            GeneratedPlankPhysics physics =
                plankRoot.AddComponent
                    <GeneratedPlankPhysics>();

            physics.PlankId =
                plank.Id;

            physics.HoleAlignmentTolerance =
                0.10f;

            physics.ScrewInsertAlignmentTolerance =
                0.025f;

            physics.AnchorClearanceRadius =
                0.18f;

            physics.SwingCollisionSkin =
                -0.02f;

            physics.BlockingEdgeTolerance =
                0.045f;


            plankObjects[
                plank.Id] =
                plankRoot;

            plankPhysics[
                plank.Id] =
                physics;
        }


        // -------------------------
        // TÜM DELİKLER SABİT
        // -------------------------

        for (int i = 0;
             i < puzzle.Holes.Count;
             i++)
        {
            HoleDefinition hole =
                puzzle.Holes[i];

            Hole runtimeHole =
                CreateHole(
                    hole.Id,
                    hole.Position,
                    hole.IsFreeStartHole,
                    root.transform
                );

            holesByDefinitionId[
                hole.Id] =
                runtimeHole;

            allRuntimeHoles.Add(
                runtimeHole
            );
        }


        // -------------------------
        // TAHTA SOCKET TANIMLARI
        // -------------------------

        for (int i = 0;
             i < puzzle.Planks.Count;
             i++)
        {
            PlankDefinition plank =
                puzzle.Planks[i];

            GameObject plankRoot =
                plankObjects[
                    plank.Id];

            GeneratedPlankPhysics physics =
                plankPhysics[
                    plank.Id];

            List<int> socketHoleIds =
                new List<int>();

            List<Vector2> socketLocalPositions =
                new List<Vector2>();

            HashSet<int> createdHoleIds =
                new HashSet<int>();

            int[] screwIds =
            {
                plank.ScrewAId,
                plank.ScrewBId,
                plank.ScrewCId
            };

            for (int s = 0;
                 s < screwIds.Length;
                 s++)
            {
                int screwId =
                    screwIds[s];

                if (screwId < 0)
                    continue;

                ScrewDefinition screw =
                    FindScrew(
                        puzzle,
                        screwId
                    );

                if (screw == null)
                    continue;

                int holeId =
                    screw.OriginalHoleId;

                if (!createdHoleIds.Add(
                        holeId))
                {
                    continue;
                }

                HoleDefinition hole =
                    FindHole(
                        puzzle,
                        holeId
                    );

                if (hole == null)
                    continue;

                Vector3 local3 =
                    plankRoot.transform
                        .InverseTransformPoint(
                            new Vector3(
                                hole.Position.x,
                                hole.Position.y,
                                0f
                            )
                        );

                socketHoleIds.Add(
                    holeId
                );

                socketLocalPositions.Add(
                    new Vector2(
                        local3.x,
                        local3.y
                    )
                );

                // Bu, tahtanın GERÇEK fiziksel deliğinin
                // sadece görselidir. Tahta sallanır veya
                // düşerse onunla birlikte hareket eder.
                // Vida takma hedefi olan sabit anchor
                // deliğinden ayrıdır.
                CreatePlankSocketVisual(
                    plankRoot.transform,
                    holeId,
                    new Vector2(
                        local3.x,
                        local3.y
                    )
                );
            }

            physics.SocketHoleIds =
                socketHoleIds.ToArray();

            physics.SocketLocalPositions =
                socketLocalPositions.ToArray();
        }


        // -------------------------
        // SCREWS
        // -------------------------

        for (int i = 0;
             i < puzzle.Screws.Count;
             i++)
        {
            ScrewDefinition screw =
                puzzle.Screws[i];

            Hole currentHole;

            if (!holesByDefinitionId
                    .TryGetValue(
                        screw.CurrentHoleId,
                        out currentHole))
            {
                continue;
            }


            GameObject screwRoot =
                new GameObject(
                    "Screw_" +
                    screw.Id.ToString("00")
                );

            screwRoot.transform.SetParent(
                root.transform
            );

            screwRoot.transform.position =
                new Vector3(
                    currentHole.transform.position.x,
                    currentHole.transform.position.y,
                    -0.15f
                );


            CircleCollider2D circleCollider =
                screwRoot.AddComponent
                    <CircleCollider2D>();

            circleCollider.isTrigger =
                false;

            circleCollider.radius =
                0.25f;


            Rigidbody2D screwBody =
                screwRoot.AddComponent
                    <Rigidbody2D>();

            screwBody.bodyType =
                RigidbodyType2D.Kinematic;

            screwBody.gravityScale =
                0f;

            screwBody.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            screwBody.interpolation =
                RigidbodyInterpolation2D.Interpolate;


            PhysicsMaterial2D screwMaterial =
                new PhysicsMaterial2D(
                    "Screw_Slide"
                );

            screwMaterial.friction =
                0f;

            screwMaterial.bounciness =
                0f;

            circleCollider.sharedMaterial =
                screwMaterial;


            GameObject screwVisual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

            screwVisual.name =
                "Visual";

            screwVisual.transform.SetParent(
                screwRoot.transform,
                false
            );

            screwVisual.transform.localPosition =
                Vector3.zero;

            screwVisual.transform.localScale =
                new Vector3(
                    0.27f,
                    0.27f,
                    0.14f
                );

            SphereCollider visualCollider =
                screwVisual.GetComponent
                    <SphereCollider>();

            if (visualCollider != null)
            {
                Object.DestroyImmediate(
                    visualCollider
                );
            }

            MeshRenderer screwRenderer =
                screwVisual.GetComponent
                    <MeshRenderer>();

            Shader screwShader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            Material screwMaterialVisual =
                new Material(
                    screwShader
                );

            screwMaterialVisual.SetColor(
                "_BaseColor",
                new Color(
                    0.82f,
                    0.84f,
                    0.86f
                )
            );

            screwRenderer.sharedMaterial =
                screwMaterialVisual;


            Screw runtimeScrew =
                screwRoot.AddComponent<Screw>();

            runtimeScrew.OwnerPlank =
                null;

            runtimeScrew.CanSelect =
                true;

            runtimeScrew.VisualTransform =
                screwVisual.transform;

            runtimeScrew.CurrentHole =
                currentHole;

            currentHole.SetOccupyingScrew(
                runtimeScrew
            );
        }


        Hole[] allHoleArray =
            allRuntimeHoles.ToArray();

        foreach (
            KeyValuePair
                <int, GeneratedPlankPhysics>
                pair
            in plankPhysics)
        {
            pair.Value.AllHoles =
                allHoleArray;
        }


        EditorSceneManager.SaveScene(
            scene,
            scenePath
        );

        EditorSceneManager.OpenScene(
            scenePath,
            OpenSceneMode.Single
        );

        Debug.Log(
            "LEVEL 1 KURULDU | " +
            "Tahta: " +
            puzzle.Planks.Count +
            " | Vida: " +
            puzzle.Screws.Count +
            " | Sabit Delik: " +
            allRuntimeHoles.Count
        );
    }


    [MenuItem(
        "Tools/Generated Levels/Build Level 3 Preview")]
    public static void BuildLevel3Preview()
    {
        PuzzleDefinition puzzle =
            PuzzleCandidateGenerator.Generate(
                3,
                30003
            );

        if (puzzle == null)
            return;

        if (!EditorSceneManager
                .SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string scenePath =
            "Assets/Scenes/" +
            "Generated_Level_03_Preview.unity";

        Scene scene =
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );


        GameObject cameraObject =
            new GameObject(
                "Main Camera"
            );

        Camera camera =
            cameraObject
                .AddComponent<Camera>();

        camera.orthographic =
            true;

        camera.orthographicSize =
            4.5f;

        camera.clearFlags =
            CameraClearFlags.SolidColor;

        camera.backgroundColor =
            new Color(
                0.25f,
                0.32f,
                0.42f
            );

        cameraObject.tag =
            "MainCamera";

        cameraObject.transform.position =
            new Vector3(
                0f,
                0f,
                -10f
            );


        GameObject root =
            new GameObject(
                "Generated_Level_03"
            );


        Dictionary<int, GameObject>
            plankObjects =
                new Dictionary
                    <int, GameObject>();

        Dictionary<int, GeneratedPlankPhysics>
            plankPhysics =
                new Dictionary
                    <int, GeneratedPlankPhysics>();

        Dictionary<int, Hole>
            holesByDefinitionId =
                new Dictionary<int, Hole>();

        List<Hole> allRuntimeHoles =
            new List<Hole>();


        // -------------------------
        // PLANKS
        // -------------------------

        for (int i = 0;
             i < puzzle.Planks.Count;
             i++)
        {
            PlankDefinition plank =
                puzzle.Planks[i];

            Vector2 runtimeSize =
                GetRuntimePlankSize(
                    puzzle,
                    plank
                );

            GameObject plankRoot =
                new GameObject(
                    "Plank_" +
                    (i + 1)
                        .ToString("00")
                );

            plankRoot.transform.SetParent(
                root.transform
            );

            // Tahtanın BoxCollider2D'si fizik için aktif kalır
            // ama mouse tıklamasını yutmasın.
            // Böylece tahta üstündeki gerçek boş deliğe
            // tıklama doğrudan Hole collider'ına ulaşır.
            plankRoot.layer =
                2; // Ignore Raycast

            plankRoot.transform.position =
                new Vector3(
                    plank.Position.x,
                    plank.Position.y,
                    0f
                );

            plankRoot.transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    plank.Rotation
                );


            GameObject visual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Quad
                );

            visual.name =
                "Visual";

            visual.transform.SetParent(
                plankRoot.transform,
                false
            );

            visual.transform.localPosition =
                Vector3.zero;

            visual.transform.localRotation =
                Quaternion.identity;

            visual.transform.localScale =
                new Vector3(
                    runtimeSize.x,
                    runtimeSize.y,
                    1f
                );

            MeshCollider oldCollider =
                visual.GetComponent
                    <MeshCollider>();

            if (oldCollider != null)
            {
                Object.DestroyImmediate(
                    oldCollider
                );
            }

            MeshRenderer renderer =
                visual.GetComponent
                    <MeshRenderer>();

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            Material material =
                new Material(
                    shader
                );

            material.SetColor(
                "_BaseColor",
                new Color(
                    0.34f,
                    0.62f,
                    0.28f
                )
            );

            renderer.sharedMaterial =
                material;


            BoxCollider2D boxCollider =
                plankRoot.AddComponent
                    <BoxCollider2D>();

            boxCollider.isTrigger =
                false;

            boxCollider.size =
                runtimeSize;


            Rigidbody2D body =
                plankRoot.AddComponent
                    <Rigidbody2D>();

            body.bodyType =
                RigidbodyType2D.Kinematic;

            body.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            body.interpolation =
                RigidbodyInterpolation2D.Interpolate;


            GeneratedPlankPhysics physics =
                plankRoot.AddComponent
                    <GeneratedPlankPhysics>();

            physics.PlankId =
                plank.Id;

            physics.HoleAlignmentTolerance =
                0.10f;

            physics.ScrewInsertAlignmentTolerance =
                0.025f;

            physics.AnchorClearanceRadius =
                0.18f;

            physics.SwingCollisionSkin =
                -0.02f;

            physics.BlockingEdgeTolerance =
                0.045f;


            plankObjects[
                plank.Id] =
                plankRoot;

            plankPhysics[
                plank.Id] =
                physics;
        }


        // -------------------------
        // TÜM DELİKLER SABİT
        // -------------------------

        for (int i = 0;
             i < puzzle.Holes.Count;
             i++)
        {
            HoleDefinition hole =
                puzzle.Holes[i];

            Hole runtimeHole =
                CreateHole(
                    hole.Id,
                    hole.Position,
                    hole.IsFreeStartHole,
                    root.transform
                );

            holesByDefinitionId[
                hole.Id] =
                runtimeHole;

            allRuntimeHoles.Add(
                runtimeHole
            );
        }


        // -------------------------
        // TAHTA SOCKET TANIMLARI
        // -------------------------

        for (int i = 0;
             i < puzzle.Planks.Count;
             i++)
        {
            PlankDefinition plank =
                puzzle.Planks[i];

            GameObject plankRoot =
                plankObjects[
                    plank.Id];

            GeneratedPlankPhysics physics =
                plankPhysics[
                    plank.Id];

            List<int> socketHoleIds =
                new List<int>();

            List<Vector2> socketLocalPositions =
                new List<Vector2>();

            HashSet<int> createdHoleIds =
                new HashSet<int>();

            int[] screwIds =
            {
                plank.ScrewAId,
                plank.ScrewBId,
                plank.ScrewCId
            };

            for (int s = 0;
                 s < screwIds.Length;
                 s++)
            {
                int screwId =
                    screwIds[s];

                if (screwId < 0)
                    continue;

                ScrewDefinition screw =
                    FindScrew(
                        puzzle,
                        screwId
                    );

                if (screw == null)
                    continue;

                int holeId =
                    screw.OriginalHoleId;

                if (!createdHoleIds.Add(
                        holeId))
                {
                    continue;
                }

                HoleDefinition hole =
                    FindHole(
                        puzzle,
                        holeId
                    );

                if (hole == null)
                    continue;

                Vector3 local3 =
                    plankRoot.transform
                        .InverseTransformPoint(
                            new Vector3(
                                hole.Position.x,
                                hole.Position.y,
                                0f
                            )
                        );

                socketHoleIds.Add(
                    holeId
                );

                socketLocalPositions.Add(
                    new Vector2(
                        local3.x,
                        local3.y
                    )
                );

                // Bu, tahtanın GERÇEK fiziksel deliğinin
                // sadece görselidir. Tahta sallanır veya
                // düşerse onunla birlikte hareket eder.
                // Vida takma hedefi olan sabit anchor
                // deliğinden ayrıdır.
                CreatePlankSocketVisual(
                    plankRoot.transform,
                    holeId,
                    new Vector2(
                        local3.x,
                        local3.y
                    )
                );
            }

            physics.SocketHoleIds =
                socketHoleIds.ToArray();

            physics.SocketLocalPositions =
                socketLocalPositions.ToArray();
        }


        // -------------------------
        // SCREWS
        // -------------------------

        for (int i = 0;
             i < puzzle.Screws.Count;
             i++)
        {
            ScrewDefinition screw =
                puzzle.Screws[i];

            Hole currentHole;

            if (!holesByDefinitionId
                    .TryGetValue(
                        screw.CurrentHoleId,
                        out currentHole))
            {
                continue;
            }


            GameObject screwRoot =
                new GameObject(
                    "Screw_" +
                    screw.Id.ToString("00")
                );

            screwRoot.transform.SetParent(
                root.transform
            );

            screwRoot.transform.position =
                new Vector3(
                    currentHole.transform.position.x,
                    currentHole.transform.position.y,
                    -0.15f
                );


            CircleCollider2D circleCollider =
                screwRoot.AddComponent
                    <CircleCollider2D>();

            circleCollider.isTrigger =
                false;

            circleCollider.radius =
                0.25f;


            Rigidbody2D screwBody =
                screwRoot.AddComponent
                    <Rigidbody2D>();

            screwBody.bodyType =
                RigidbodyType2D.Kinematic;

            screwBody.gravityScale =
                0f;

            screwBody.collisionDetectionMode =
                CollisionDetectionMode2D.Continuous;

            screwBody.interpolation =
                RigidbodyInterpolation2D.Interpolate;


            PhysicsMaterial2D screwMaterial =
                new PhysicsMaterial2D(
                    "Screw_Slide"
                );

            screwMaterial.friction =
                0f;

            screwMaterial.bounciness =
                0f;

            circleCollider.sharedMaterial =
                screwMaterial;


            GameObject screwVisual =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

            screwVisual.name =
                "Visual";

            screwVisual.transform.SetParent(
                screwRoot.transform,
                false
            );

            screwVisual.transform.localPosition =
                Vector3.zero;

            screwVisual.transform.localScale =
                new Vector3(
                    0.27f,
                    0.27f,
                    0.14f
                );

            SphereCollider visualCollider =
                screwVisual.GetComponent
                    <SphereCollider>();

            if (visualCollider != null)
            {
                Object.DestroyImmediate(
                    visualCollider
                );
            }

            MeshRenderer screwRenderer =
                screwVisual.GetComponent
                    <MeshRenderer>();

            Shader screwShader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            Material screwMaterialVisual =
                new Material(
                    screwShader
                );

            screwMaterialVisual.SetColor(
                "_BaseColor",
                new Color(
                    0.82f,
                    0.84f,
                    0.86f
                )
            );

            screwRenderer.sharedMaterial =
                screwMaterialVisual;


            Screw runtimeScrew =
                screwRoot.AddComponent<Screw>();

            runtimeScrew.OwnerPlank =
                null;

            runtimeScrew.CanSelect =
                true;

            runtimeScrew.VisualTransform =
                screwVisual.transform;

            runtimeScrew.CurrentHole =
                currentHole;

            currentHole.SetOccupyingScrew(
                runtimeScrew
            );
        }


        Hole[] allHoleArray =
            allRuntimeHoles.ToArray();

        foreach (
            KeyValuePair
                <int, GeneratedPlankPhysics>
                pair
            in plankPhysics)
        {
            pair.Value.AllHoles =
                allHoleArray;
        }


        EditorSceneManager.SaveScene(
            scene,
            scenePath
        );

        EditorSceneManager.OpenScene(
            scenePath,
            OpenSceneMode.Single
        );

        Debug.Log(
            "LEVEL 3 KURULDU | " +
            "Tahta: " +
            puzzle.Planks.Count +
            " | Vida: " +
            puzzle.Screws.Count +
            " | Sabit Delik: " +
            allRuntimeHoles.Count
        );
    }


    private static Vector2
        GetRuntimePlankSize(
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
        {
            plank.ScrewAId,
            plank.ScrewBId,
            plank.ScrewCId
        };

        Quaternion inverseRotation =
            Quaternion.Euler(
                0f,
                0f,
                -plank.Rotation
            );

        for (int i = 0;
             i < screwIds.Length;
             i++)
        {
            if (screwIds[i] < 0)
                continue;

            ScrewDefinition screw =
                FindScrew(
                    puzzle,
                    screwIds[i]
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

            Vector2 worldOffset =
                hole.Position -
                plank.Position;

            Vector3 local3 =
                inverseRotation *
                new Vector3(
                    worldOffset.x,
                    worldOffset.y,
                    0f
                );

            requiredHalfX =
                Mathf.Max(
                    requiredHalfX,
                    Mathf.Abs(local3.x) +
                    0.22f
                );

            requiredHalfY =
                Mathf.Max(
                    requiredHalfY,
                    Mathf.Abs(local3.y) +
                    0.22f
                );
        }

        return
            new Vector2(
                requiredHalfX * 2f,
                requiredHalfY * 2f
            );
    }



    private static void CreatePlankSocketVisual(
        Transform plankParent,
        int holeId,
        Vector2 localPosition)
    {
        GameObject socketRoot =
            new GameObject(
                "PlankSocketVisual_" +
                holeId.ToString("00")
            );

        socketRoot.transform.SetParent(
            plankParent,
            false
        );

        socketRoot.transform.localPosition =
            new Vector3(
                localPosition.x,
                localPosition.y,
                -0.06f
            );

        socketRoot.transform.localRotation =
            Quaternion.identity;


        GameObject visual =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        visual.name =
            "Visual";

        visual.transform.SetParent(
            socketRoot.transform,
            false
        );

        visual.transform.localPosition =
            Vector3.zero;

        // Tahtanın fiziksel deliği.
        // Önceki 0.25 biraz küçük görünüyordu;
        // 0.32 ile daha okunur hale getiriyoruz.
        visual.transform.localScale =
            new Vector3(
                0.32f,
                0.32f,
                0.08f
            );

        SphereCollider collider =
            visual.GetComponent
                <SphereCollider>();

        if (collider != null)
        {
            Object.DestroyImmediate(
                collider
            );
        }

        MeshRenderer renderer =
            visual.GetComponent
                <MeshRenderer>();

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        Material material =
            new Material(
                shader
            );

        material.SetColor(
            "_BaseColor",
            new Color(
                0.20f,
                0.12f,
                0.07f
            )
        );

        renderer.sharedMaterial =
            material;
    }


    private static Hole CreateHole(
        int definitionId,
        Vector2 worldPosition,
        bool isStorageHole,
        Transform parent)
    {
        GameObject holeRoot =
            new GameObject(
                "Hole_" +
                definitionId.ToString("00")
            );

        holeRoot.transform.position =
            new Vector3(
                worldPosition.x,
                worldPosition.y,
                0.08f
            );

        holeRoot.transform.SetParent(
            parent,
            true
        );


        CircleCollider2D circleCollider =
            holeRoot.AddComponent
                <CircleCollider2D>();

        circleCollider.isTrigger =
            true;

        circleCollider.radius =
            0.24f;


        GameObject visual =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        visual.name =
            "Visual";

        visual.transform.SetParent(
            holeRoot.transform,
            false
        );

        visual.transform.localPosition =
            Vector3.zero;

        visual.transform.localScale =
            new Vector3(
                0.34f,
                0.34f,
                0.12f
            );

        SphereCollider visualCollider =
            visual.GetComponent
                <SphereCollider>();

        if (visualCollider != null)
        {
            Object.DestroyImmediate(
                visualCollider
            );
        }

        MeshRenderer renderer =
            visual.GetComponent
                <MeshRenderer>();

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        Material material =
            new Material(
                shader
            );

        material.SetColor(
            "_BaseColor",
            new Color(
                0.20f,
                0.12f,
                0.07f
            )
        );

        renderer.sharedMaterial =
            material;


        Hole runtimeHole =
            holeRoot.AddComponent<Hole>();

        runtimeHole.Configure(
            definitionId,
            isStorageHole
        );

        return
            runtimeHole;
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


    private static PuzzleDefinition
        LoadLevel1()
    {
        string path =
            Path.Combine(
                Application.dataPath,
                "GeneratedLevels/Level_01.json"
            );

        if (!File.Exists(path))
        {
            Debug.LogError(
                "Level_01.json bulunamadi."
            );

            return null;
        }

        string json =
            File.ReadAllText(
                path
            );

        PuzzleDefinition puzzle =
            JsonUtility.FromJson
                <PuzzleDefinition>(
                    json
                );

        if (puzzle == null)
        {
            Debug.LogError(
                "Puzzle okunamadi."
            );

            return null;
        }

        return
            puzzle;
    }
}
