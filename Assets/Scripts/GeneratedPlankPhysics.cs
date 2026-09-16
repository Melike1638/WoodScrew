using System.Collections.Generic;
using UnityEngine;
using WoodScrew.PhysicsCore;

public class GeneratedPlankPhysics : MonoBehaviour
{
    public static readonly
        List<GeneratedPlankPhysics>
        ActivePlanks =
            new List<GeneratedPlankPhysics>();

    public int PlankId = -1;

    // Eski alanlar başka sahneleri bozmasın diye
    // şimdilik tutuluyor.
    public Hole SupportHoleA;
    public Hole SupportHoleB;
    public Hole SupportHoleC;

    public Screw SupportScrewA;
    public Screw SupportScrewB;
    public Screw SupportScrewC;

    public Hole[] AllHoles;

    // Tahtanın gerçek vida geçiş noktaları.
    // Bunlar görünür/tıklanabilir Hole nesneleri değildir.
    // Tahtayla birlikte hareket eden geometrik socket'lerdir.
    public int[] SocketHoleIds;
    public Vector2[] SocketLocalPositions;

    [Header("Swing")]
    public float SwingGravityStrength = 42f;
    public float SwingDamping = 1.15f;
    public float MaxSwingSpeed = 18f;
    public float NeutralKickSpeed = 5.5f;

    [Header("Fall")]
    public float FallGravity = 1.25f;
    public float FallLinearDamping = 0.04f;
    public float FallAngularDamping = 3.5f;
    public float MaxFallSpeed = 3.0f;
    public float MaxFallAngularSpeed = 24f;

    [Header("Collision")]
    public float HoleAlignmentTolerance = 0.10f;

    // Vida ancak tahta deliği anchor ile neredeyse
    // tam merkezlenmişse o delikten geçebilir.
    public float ScrewInsertAlignmentTolerance = 0.025f;

    // Anchor deliği tahta tarafından kısmen bile
    // kapalıysa yeni vida takılamaz.
    public float AnchorClearanceRadius = 0.18f;

    public float SwingCollisionSkin = -0.02f;
    public float BlockingEdgeTolerance = 0.045f;

    public float DestroyBelowY = -6.5f;

    private Rigidbody2D body;
    private BoxCollider2D plankCollider;

    // Key = socket index
    // Value = o socket'e gerçekten takılmış vida
    private readonly Dictionary<int, Screw>
        attachedScrews =
            new Dictionary<int, Screw>();

    private readonly HashSet<Screw>
        ignoredAttachedScrews =
            new HashSet<Screw>();

    private readonly List<Collider2D>
        initiallyIgnoredPlanks =
            new List<Collider2D>();

    private enum MotionMode
    {
        Fixed,
        Swinging,
        Falling
    }

    private MotionMode motionMode =
        MotionMode.Fixed;

    private int pivotSocketIndex = -1;
    private Screw pivotScrew;

    private float swingAngularVelocity;

    private float stuckTime;
    private float slideDirection = 1f;


    void OnEnable()
    {
        if (!ActivePlanks.Contains(this))
        {
            ActivePlanks.Add(this);
        }
    }


    void OnDisable()
    {
        ActivePlanks.Remove(this);
    }


    void Awake()
    {
        body =
            GetComponent<Rigidbody2D>();

        plankCollider =
            GetComponent<BoxCollider2D>();

        body.bodyType =
            RigidbodyType2D.Kinematic;

        body.gravityScale =
            0f;

        body.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        body.interpolation =
            RigidbodyInterpolation2D.Interpolate;

        body.sleepMode =
            RigidbodySleepMode2D.NeverSleep;

        PhysicsMaterial2D material =
            new PhysicsMaterial2D(
                name + "_PlankSlide"
            );

        material.friction =
            0f;

        material.bounciness =
            0f;

        plankCollider.sharedMaterial =
            material;
    }


    void Start()
    {
        InitializeStartingAttachments();

        SyncAttachedScrewCollisions();

        IgnoreInitialPlankOverlaps();

        RefreshSupportState();
    }


    private void InitializeStartingAttachments()
    {
        attachedScrews.Clear();

        if (SocketLocalPositions == null ||
            SocketHoleIds == null)
        {
            return;
        }

        int count =
            Mathf.Min(
                SocketLocalPositions.Length,
                SocketHoleIds.Length
            );

        for (int i = 0;
             i < count;
             i++)
        {
            Hole anchorHole =
                FindAnchorHole(
                    SocketHoleIds[i]
                );

            if (anchorHole == null ||
                anchorHole.OccupyingScrew ==
                null)
            {
                continue;
            }

            Vector2 socketWorld =
                GetSocketWorldPosition(
                    i
                );

            float distance =
                Vector2.Distance(
                    socketWorld,
                    anchorHole.transform.position
                );

            if (distance >
                HoleAlignmentTolerance)
            {
                continue;
            }

            attachedScrews[i] =
                anchorHole.OccupyingScrew;
        }
    }


    private Hole FindAnchorHole(
        int definitionId)
    {
        if (AllHoles != null)
        {
            for (int i = 0;
                 i < AllHoles.Length;
                 i++)
            {
                Hole hole =
                    AllHoles[i];

                if (hole != null &&
                    hole.DefinitionId ==
                    definitionId)
                {
                    return hole;
                }
            }
        }

        for (int i = 0;
             i < Hole.ActiveHoles.Count;
             i++)
        {
            Hole hole =
                Hole.ActiveHoles[i];

            if (hole != null &&
                hole.DefinitionId ==
                definitionId)
            {
                return hole;
            }
        }

        return null;
    }


    private Vector2 GetSocketWorldPosition(
        int socketIndex)
    {
        if (SocketLocalPositions == null ||
            socketIndex < 0 ||
            socketIndex >=
            SocketLocalPositions.Length)
        {
            return
                transform.position;
        }

        Vector3 local =
            SocketLocalPositions[
                socketIndex];

        return
            transform.TransformPoint(
                local
            );
    }


    private bool HasSocketAtWorldPosition(
        Vector2 worldPosition,
        float tolerance,
        out int socketIndex)
    {
        socketIndex =
            -1;

        if (SocketLocalPositions == null)
            return false;

        for (int i = 0;
             i < SocketLocalPositions.Length;
             i++)
        {
            float distance =
                Vector2.Distance(
                    GetSocketWorldPosition(i),
                    worldPosition
                );

            if (distance <= tolerance)
            {
                socketIndex =
                    i;

                return true;
            }
        }

        return false;
    }


    public bool ContainsWorldPoint(
        Vector2 worldPosition)
    {
        if (plankCollider == null)
            return false;

        return
            plankCollider.OverlapPoint(
                worldPosition
            );
    }


    private bool BlocksWorldPoint(
        Vector2 worldPosition)
    {
        if (plankCollider == null)
            return false;

        Vector3 local =
            transform.InverseTransformPoint(
                worldPosition
            );

        Vector2 halfSize =
            plankCollider.size *
            0.5f;

        float halfX =
            Mathf.Max(
                0f,
                halfSize.x -
                BlockingEdgeTolerance
            );

        float halfY =
            Mathf.Max(
                0f,
                halfSize.y -
                BlockingEdgeTolerance
            );

        return
            Mathf.Abs(local.x) <
                halfX &&
            Mathf.Abs(local.y) <
                halfY;
    }


    private bool BlocksInsertionArea(
        Vector2 worldPosition)
    {
        if (plankCollider == null)
            return false;

        Vector3 worldCenter3 =
            transform.TransformPoint(
                plankCollider.offset
            );

        Vector3 scale =
            transform.lossyScale;

        Vector2 worldSize =
            new Vector2(
                plankCollider.size.x *
                    Mathf.Abs(scale.x),
                plankCollider.size.y *
                    Mathf.Abs(scale.y)
            );

        CoreOrientedRect plankPose =
            new CoreOrientedRect(
                new CoreVector2(
                    worldCenter3.x,
                    worldCenter3.y
                ),
                new CoreVector2(
                    worldSize.x,
                    worldSize.y
                ),
                transform.eulerAngles.z
            );

        return
            WoodScrewPhysicsCore
                .IsAnchorBlockedAtPose(
                    new CoreVector2(
                        worldPosition.x,
                        worldPosition.y
                    ),
                    AnchorClearanceRadius,
                    plankPose
                );
    }


    public static bool IsHoleVisuallyExposed(
        Hole targetHole)
    {
        if (targetHole == null)
            return false;

        Vector2 worldPosition =
            targetHole.transform.position;

        for (int i = 0;
             i < ActivePlanks.Count;
             i++)
        {
            GeneratedPlankPhysics plank =
                ActivePlanks[i];

            if (plank == null ||
                plank.plankCollider == null)
            {
                continue;
            }

            if (!plank.BlocksWorldPoint(
                    worldPosition))
            {
                continue;
            }

            int socketIndex;

            if (!plank.HasSocketAtWorldPosition(
                    worldPosition,
                    plank.HoleAlignmentTolerance,
                    out socketIndex))
            {
                return false;
            }
        }

        return true;
    }


    public static bool CanInsertScrewAtHole(
        Hole targetHole,
        Screw movingScrew)
    {
        if (targetHole == null)
            return false;

        if (targetHole.OccupyingScrew != null &&
            targetHole.OccupyingScrew !=
            movingScrew)
        {
            return false;
        }

        Vector2 worldPosition =
            targetHole.transform.position;

        for (int i = 0;
             i < ActivePlanks.Count;
             i++)
        {
            GeneratedPlankPhysics plank =
                ActivePlanks[i];

            if (plank == null ||
                plank.plankCollider == null)
            {
                continue;
            }

            int socketIndex;

            // Tahta deliği anchor ile gerçekten
            // merkezlenmişse vida o delikten geçebilir.
            bool socketExactlyAligned =
                plank.HasSocketAtWorldPosition(
                    worldPosition,
                    plank.ScrewInsertAlignmentTolerance,
                    out socketIndex
                );

            if (socketExactlyAligned)
            {
                continue;
            }

            // Tam hizalı bir tahta deliği yoksa,
            // anchor'ın görünen dairesinin tamamı
            // tahtadan çıkmadan vida takılamaz.
            if (plank.BlocksInsertionArea(
                    worldPosition))
            {
                return false;
            }
        }

        return true;
    }


    public static void NotifyScrewRemoved(
        Screw screw)
    {
        if (screw == null)
            return;

        for (int i =
                 ActivePlanks.Count - 1;
             i >= 0;
             i--)
        {
            GeneratedPlankPhysics plank =
                ActivePlanks[i];

            if (plank == null)
                continue;

            plank.RemoveAttachment(
                screw
            );
        }
    }


    public static void NotifyScrewPlaced(
        Screw screw)
    {
        if (screw == null ||
            screw.CurrentHole == null)
        {
            return;
        }

        NotifyScrewPlaced(
            screw.CurrentHole,
            screw
        );
    }


    public static void NotifyScrewPlaced(
        Hole targetHole,
        Screw screw)
    {
        if (targetHole == null ||
            screw == null)
        {
            return;
        }

        Physics2D.SyncTransforms();

        for (int i =
                 ActivePlanks.Count - 1;
             i >= 0;
             i--)
        {
            GeneratedPlankPhysics plank =
                ActivePlanks[i];

            if (plank == null)
                continue;

            plank.TryAttachPlacedScrew(
                targetHole,
                screw
            );
        }
    }


    private void RemoveAttachment(
        Screw screw)
    {
        List<int> removeKeys =
            new List<int>();

        foreach (
            KeyValuePair<int, Screw>
                pair
            in attachedScrews)
        {
            if (pair.Value == screw)
            {
                removeKeys.Add(
                    pair.Key
                );
            }
        }

        if (removeKeys.Count == 0)
            return;

        for (int i = 0;
             i < removeKeys.Count;
             i++)
        {
            attachedScrews.Remove(
                removeKeys[i]
            );
        }

        SyncAttachedScrewCollisions();

        RefreshSupportState();
    }


    private void TryAttachPlacedScrew(
        Hole targetHole,
        Screw screw)
    {
        int socketIndex;

        if (!HasSocketAtWorldPosition(
                targetHole.transform.position,
                ScrewInsertAlignmentTolerance,
                out socketIndex))
        {
            return;
        }

        attachedScrews[
            socketIndex] =
            screw;

        SyncAttachedScrewCollisions();

        RefreshSupportState();
    }


    private void RefreshSupportState()
    {
        int singleSocketIndex;
        Screw singleScrew;

        int supportCount =
            GetUniqueSupportCount(
                out singleSocketIndex,
                out singleScrew
            );

        PlankMotionState coreState =
            WoodScrewPhysicsCore
                .GetMotionState(
                    supportCount
                );

        if (coreState ==
            PlankMotionState.Fixed)
        {
            EnterFixedMode();
            return;
        }

        if (coreState ==
            PlankMotionState.Pivoting)
        {
            EnterSwingMode(
                singleSocketIndex,
                singleScrew
            );

            return;
        }

        EnterFallMode();
    }


    private int GetUniqueSupportCount(
        out int singleSocketIndex,
        out Screw singleScrew)
    {
        singleSocketIndex =
            -1;

        singleScrew =
            null;

        HashSet<Screw> unique =
            new HashSet<Screw>();

        foreach (
            KeyValuePair<int, Screw>
                pair
            in attachedScrews)
        {
            Screw screw =
                pair.Value;

            if (screw == null)
                continue;

            if (!unique.Add(
                    screw))
            {
                continue;
            }

            singleSocketIndex =
                pair.Key;

            singleScrew =
                screw;
        }

        return
            unique.Count;
    }


    private void EnterFixedMode()
    {
        motionMode =
            MotionMode.Fixed;

        pivotSocketIndex =
            -1;

        pivotScrew =
            null;

        swingAngularVelocity =
            0f;

        body.bodyType =
            RigidbodyType2D.Kinematic;

        body.gravityScale =
            0f;

        body.linearVelocity =
            Vector2.zero;

        body.angularVelocity =
            0f;
    }


    private void EnterSwingMode(
        int socketIndex,
        Screw screw)
    {
        if (socketIndex < 0 ||
            screw == null)
        {
            EnterFallMode();
            return;
        }

        bool samePivot =
            motionMode ==
                MotionMode.Swinging &&
            pivotSocketIndex ==
                socketIndex &&
            pivotScrew ==
                screw;

        motionMode =
            MotionMode.Swinging;

        pivotSocketIndex =
            socketIndex;

        pivotScrew =
            screw;

        body.bodyType =
            RigidbodyType2D.Kinematic;

        body.gravityScale =
            0f;

        body.linearVelocity =
            Vector2.zero;

        body.angularVelocity =
            0f;

        // Vida ile tahta socket'i arasında
        // birikmiş en küçük kaymayı bile temizle.
        SnapPivotSocketToScrew();

        if (!samePivot)
        {
            Vector2 pivot =
                screw.transform.position;

            Vector2 center =
                body.position;

            Vector2 offset =
                center -
                pivot;

            swingAngularVelocity =
                0f;

            bool verticalBalance =
                Mathf.Abs(offset.x) <
                0.06f;

            Vector2 localSocket =
                SocketLocalPositions[
                    socketIndex];

            // Orta vida veya alt vida tek kaldıysa
            // ideal matematikte tork sıfır olabilir.
            // Yavaş ve deterministik bir başlangıç ver.
            if (verticalBalance &&
                localSocket.y <= 0.05f)
            {
                swingAngularVelocity =
                    GetPreferredDirection() *
                    NeutralKickSpeed;
            }
        }
    }


    private void SnapPivotSocketToScrew()
    {
        if (pivotSocketIndex < 0 ||
            pivotScrew == null ||
            SocketLocalPositions == null ||
            pivotSocketIndex >=
            SocketLocalPositions.Length)
        {
            return;
        }

        Vector2 pivot =
            pivotScrew.transform.position;

        Vector2 localSocket =
            SocketLocalPositions[
                pivotSocketIndex];

        Vector2 rotatedSocket =
            RotateVector(
                localSocket,
                body.rotation
            );

        Vector2 desiredCenter =
            pivot -
            rotatedSocket;

        body.position =
            desiredCenter;

        transform.position =
            new Vector3(
                desiredCenter.x,
                desiredCenter.y,
                transform.position.z
            );

        Physics2D.SyncTransforms();
    }


    private void EnterFallMode()
    {
        if (motionMode ==
            MotionMode.Falling)
        {
            return;
        }

        Vector2 inheritedVelocity =
            Vector2.zero;

        float inheritedAngular =
            0f;

        if (motionMode ==
                MotionMode.Swinging &&
            pivotScrew != null)
        {
            Vector2 pivot =
                pivotScrew.transform.position;

            Vector2 radius =
                body.position -
                pivot;

            float omega =
                swingAngularVelocity *
                Mathf.Deg2Rad;

            inheritedVelocity =
                new Vector2(
                    -omega * radius.y,
                    omega * radius.x
                );

            inheritedAngular =
                swingAngularVelocity;
        }

        motionMode =
            MotionMode.Falling;

        pivotSocketIndex =
            -1;

        pivotScrew =
            null;

        body.bodyType =
            RigidbodyType2D.Dynamic;

        body.gravityScale =
            FallGravity;

        body.linearDamping =
            FallLinearDamping;

        body.angularDamping =
            FallAngularDamping;

        body.constraints =
            RigidbodyConstraints2D.None;

        slideDirection =
            GetReleaseDirection();

        if (Mathf.Abs(
                inheritedVelocity.x) <
            0.06f)
        {
            inheritedVelocity.x =
                slideDirection *
                0.10f;
        }

        if (inheritedVelocity.y >
            -0.18f)
        {
            inheritedVelocity.y =
                -0.18f;
        }

        body.linearVelocity =
            inheritedVelocity;

        body.angularVelocity =
            inheritedAngular;

        stuckTime =
            0f;

        body.WakeUp();
    }


    void FixedUpdate()
    {
        if (motionMode ==
            MotionMode.Swinging)
        {
            SimulateSwing();
        }
        else if (motionMode ==
                 MotionMode.Falling)
        {
            SimulateFall();
        }

        RestoreSeparatedPlankCollisions();

        if (transform.position.y <
            DestroyBelowY)
        {
            Destroy(
                gameObject
            );
        }
    }


    private void SimulateSwing()
    {
        if (pivotSocketIndex < 0 ||
            pivotScrew == null ||
            SocketLocalPositions == null ||
            pivotSocketIndex >=
                SocketLocalPositions.Length)
        {
            RefreshSupportState();
            return;
        }

        Vector2 pivot =
            pivotScrew.transform.position;

        Vector2 center =
            body.position;

        Vector2 offset =
            center -
            pivot;

        float angularAcceleration =
            -offset.x *
            SwingGravityStrength;

        swingAngularVelocity +=
            angularAcceleration *
            Time.fixedDeltaTime;

        float dampingFactor =
            Mathf.Clamp01(
                1f -
                SwingDamping *
                Time.fixedDeltaTime
            );

        swingAngularVelocity *=
            dampingFactor;

        swingAngularVelocity =
            Mathf.Clamp(
                swingAngularVelocity,
                -MaxSwingSpeed,
                MaxSwingSpeed
            );

        float deltaAngle =
            swingAngularVelocity *
            Time.fixedDeltaTime;

        if (Mathf.Abs(deltaAngle) <
            0.0001f)
        {
            SnapPivotSocketToScrew();
            return;
        }

        float newAngle =
            body.rotation +
            deltaAngle;

        Vector2 localPivotSocket =
            SocketLocalPositions[
                pivotSocketIndex];

        // Merkez, vida etrafında tahmini dönmüyor.
        // Doğrudan "socket tam vidanın üstünde kalacak"
        // denklemiyle hesaplanıyor.
        Vector2 newCenter =
            pivot -
            RotateVector(
                localPivotSocket,
                newAngle
            );

        // Vida çarpışması/tunneling kontrolü artık
        // PhysicsCore'un sweep kuralından geliyor.
        if (WouldSwingSweepHitScrew(
                pivot,
                deltaAngle))
        {
            swingAngularVelocity =
                0f;

            SnapPivotSocketToScrew();

            return;
        }

        // Tahta-tahta ve diğer Unity collider engelleri
        // mevcut runtime kontrolünde kalıyor.
        if (WouldPoseHitObstacle(
                newCenter,
                newAngle))
        {
            swingAngularVelocity =
                0f;

            SnapPivotSocketToScrew();

            return;
        }

        body.MovePosition(
            newCenter
        );

        body.MoveRotation(
            newAngle
        );
    }


    private bool WouldSwingSweepHitScrew(
        Vector2 pivot,
        float deltaAngle)
    {
        if (plankCollider == null)
            return false;

        Vector3 scale =
            transform.lossyScale;

        Vector2 worldSize =
            new Vector2(
                plankCollider.size.x *
                    Mathf.Abs(scale.x),
                plankCollider.size.y *
                    Mathf.Abs(scale.y)
            );

        Vector3 worldCenter3 =
            transform.TransformPoint(
                plankCollider.offset
            );

        CoreOrientedRect startPose =
            new CoreOrientedRect(
                new CoreVector2(
                    worldCenter3.x,
                    worldCenter3.y
                ),
                new CoreVector2(
                    worldSize.x,
                    worldSize.y
                ),
                body.rotation
            );

        CoreVector2 corePivot =
            new CoreVector2(
                pivot.x,
                pivot.y
            );

        for (int i = 0;
             i < Screw.ActiveScrews.Count;
             i++)
        {
            Screw screw =
                Screw.ActiveScrews[i];

            if (screw == null ||
                IsAttachedScrew(
                    screw))
            {
                continue;
            }

            Collider2D screwCollider =
                screw.GetComponent<Collider2D>();

            if (screwCollider == null ||
                screwCollider.isTrigger ||
                !screwCollider.enabled)
            {
                continue;
            }

            CircleCollider2D circle =
                screwCollider as
                    CircleCollider2D;

            float radius;

            if (circle != null)
            {
                Vector3 screwScale =
                    circle.transform.lossyScale;

                float maxScale =
                    Mathf.Max(
                        Mathf.Abs(
                            screwScale.x),
                        Mathf.Abs(
                            screwScale.y)
                    );

                radius =
                    circle.radius *
                    maxScale;
            }
            else
            {
                // Şu an vidalar CircleCollider2D kullanıyor.
                // Farklı collider gelirse güvenli fallback.
                Bounds bounds =
                    screwCollider.bounds;

                radius =
                    Mathf.Max(
                        bounds.extents.x,
                        bounds.extents.y
                    );
            }

            CoreCircle obstacle =
                new CoreCircle(
                    new CoreVector2(
                        screw.transform.position.x,
                        screw.transform.position.y
                    ),
                    radius
                );

            if (WoodScrewPhysicsCore
                .SweepHitsCircle(
                    startPose,
                    corePivot,
                    deltaAngle,
                    obstacle,
                    SwingCollisionSkin,
                    12))
            {
                return true;
            }
        }

        return false;
    }


    private bool WouldPoseHitObstacle(
        Vector2 newCenter,
        float newAngle)
    {
        Vector2 size =
            Vector2.Scale(
                plankCollider.size,
                new Vector2(
                    Mathf.Abs(
                        transform.lossyScale.x),
                    Mathf.Abs(
                        transform.lossyScale.y)
                )
            );

        size.x =
            Mathf.Max(
                0.01f,
                size.x -
                SwingCollisionSkin
            );

        size.y =
            Mathf.Max(
                0.01f,
                size.y -
                SwingCollisionSkin
            );

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                newCenter,
                size,
                newAngle
            );

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider2D hit =
                hits[i];

            if (hit == null ||
                hit == plankCollider)
            {
                continue;
            }

            if (hit.isTrigger)
                continue;

            Screw screw =
                hit.GetComponent<Screw>();

            if (screw != null)
            {
                // Vida engeli PhysicsCore sweep tarafından
                // tek doğruluk kaynağı olarak ele alınıyor.
                continue;
            }

            GeneratedPlankPhysics other =
                hit.GetComponent
                    <GeneratedPlankPhysics>();

            if (other != null)
            {
                if (IsInitiallyIgnoredPlank(
                        hit))
                {
                    continue;
                }

                return true;
            }

            return true;
        }

        return false;
    }


    private static Vector2 RotateVector(
        Vector2 vector,
        float angleDegrees)
    {
        float radians =
            angleDegrees *
            Mathf.Deg2Rad;

        float cosine =
            Mathf.Cos(
                radians
            );

        float sine =
            Mathf.Sin(
                radians
            );

        return
            new Vector2(
                vector.x * cosine -
                vector.y * sine,
                vector.x * sine +
                vector.y * cosine
            );
    }


    private void SimulateFall()
    {
        body.angularVelocity =
            Mathf.Clamp(
                body.angularVelocity,
                -MaxFallAngularSpeed,
                MaxFallAngularSpeed
            );

        Vector2 velocity =
            body.linearVelocity;

        if (velocity.y <
            -MaxFallSpeed)
        {
            velocity.y =
                -MaxFallSpeed;
        }

        body.linearVelocity =
            velocity;

        if (body.linearVelocity
                .sqrMagnitude >
            0.018f)
        {
            stuckTime =
                0f;

            return;
        }

        stuckTime +=
            Time.fixedDeltaTime;

        if (stuckTime <
            0.28f)
        {
            return;
        }

        velocity =
            body.linearVelocity;

        velocity.x =
            slideDirection *
            0.30f;

        velocity.y =
            -0.16f;

        body.linearVelocity =
            velocity;

        body.angularVelocity =
            slideDirection *
            3f;

        body.WakeUp();

        stuckTime =
            0f;
    }


    private bool IsAttachedScrew(
        Screw screw)
    {
        if (screw == null)
            return false;

        foreach (
            KeyValuePair<int, Screw>
                pair
            in attachedScrews)
        {
            if (pair.Value == screw)
                return true;
        }

        return false;
    }


    private void SyncAttachedScrewCollisions()
    {
        HashSet<Screw> attached =
            new HashSet<Screw>();

        foreach (
            KeyValuePair<int, Screw>
                pair
            in attachedScrews)
        {
            if (pair.Value != null)
            {
                attached.Add(
                    pair.Value
                );
            }
        }

        List<Screw> restore =
            new List<Screw>();

        foreach (Screw screw
                 in ignoredAttachedScrews)
        {
            if (!attached.Contains(
                    screw))
            {
                restore.Add(
                    screw
                );
            }
        }

        for (int i = 0;
             i < restore.Count;
             i++)
        {
            SetCollisionWithScrew(
                restore[i],
                false
            );

            ignoredAttachedScrews.Remove(
                restore[i]
            );
        }

        foreach (Screw screw
                 in attached)
        {
            if (ignoredAttachedScrews
                .Contains(
                    screw))
            {
                continue;
            }

            SetCollisionWithScrew(
                screw,
                true
            );

            ignoredAttachedScrews.Add(
                screw
            );
        }
    }


    private void SetCollisionWithScrew(
        Screw screw,
        bool ignore)
    {
        if (screw == null ||
            plankCollider == null)
        {
            return;
        }

        Collider2D screwCollider =
            screw.GetComponent<Collider2D>();

        if (screwCollider == null)
            return;

        Physics2D.IgnoreCollision(
            plankCollider,
            screwCollider,
            ignore
        );
    }


    private float GetPreferredDirection()
    {
        if (transform.position.x <
            -0.05f)
        {
            return 1f;
        }

        if (transform.position.x >
            0.05f)
        {
            return -1f;
        }

        return
            (PlankId % 2 == 0)
                ? 1f
                : -1f;
    }


    private float GetReleaseDirection()
    {
        if (transform.position.x <
            -0.05f)
        {
            return -1f;
        }

        if (transform.position.x >
            0.05f)
        {
            return 1f;
        }

        return
            (PlankId % 2 == 0)
                ? 1f
                : -1f;
    }


    private void IgnoreInitialPlankOverlaps()
    {
        if (plankCollider == null)
            return;

        for (int i = 0;
             i < ActivePlanks.Count;
             i++)
        {
            GeneratedPlankPhysics other =
                ActivePlanks[i];

            if (other == null ||
                other == this)
            {
                continue;
            }

            Collider2D otherCollider =
                other.plankCollider;

            if (otherCollider == null)
                continue;

            ColliderDistance2D distance =
                Physics2D.Distance(
                    plankCollider,
                    otherCollider
                );

            if (!distance.isOverlapped)
                continue;

            Physics2D.IgnoreCollision(
                plankCollider,
                otherCollider,
                true
            );

            if (!initiallyIgnoredPlanks
                    .Contains(
                        otherCollider))
            {
                initiallyIgnoredPlanks.Add(
                    otherCollider
                );
            }
        }
    }


    private bool IsInitiallyIgnoredPlank(
        Collider2D other)
    {
        return
            initiallyIgnoredPlanks
                .Contains(
                    other
                );
    }


    private void RestoreSeparatedPlankCollisions()
    {
        if (plankCollider == null)
            return;

        for (int i =
                 initiallyIgnoredPlanks.Count - 1;
             i >= 0;
             i--)
        {
            Collider2D other =
                initiallyIgnoredPlanks[i];

            if (other == null)
            {
                initiallyIgnoredPlanks
                    .RemoveAt(i);

                continue;
            }

            ColliderDistance2D distance =
                Physics2D.Distance(
                    plankCollider,
                    other
                );

            if (distance.isOverlapped)
                continue;

            Physics2D.IgnoreCollision(
                plankCollider,
                other,
                false
            );

            initiallyIgnoredPlanks
                .RemoveAt(i);
        }
    }


    void OnDestroy()
    {
        foreach (Screw screw
                 in ignoredAttachedScrews)
        {
            SetCollisionWithScrew(
                screw,
                false
            );
        }

        ignoredAttachedScrews.Clear();
    }
}
