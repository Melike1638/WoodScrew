using UnityEngine;

public class SimplePlank : MonoBehaviour
{
    public Screw Screw01;
    public Screw Screw02;

    public Hole SupportHole01;
    public Hole SupportHole02;

    public LevelFlow LevelFlow;

    private Rigidbody2D body;
    private HingeJoint2D hinge;
    private Collider2D plankCollider;

    private Screw currentPivotScrew;
    private Collider2D currentPivotCollider;

    private bool isReleased = false;
    private bool completionReported = false;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hinge = GetComponent<HingeJoint2D>();
        plankCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        hinge.enabled = false;
        body.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        if (isReleased)
        {
            // Tahta ekranın altına tamamen düştü.
            if (transform.position.y < -8f)
            {
                if (!completionReported)
                {
                    completionReported = true;

                    if (LevelFlow != null)
                        LevelFlow.PlankCompleted();
                }

                gameObject.SetActive(false);
            }

            return;
        }

        bool screw01Supporting =
            Screw01.CurrentHole == SupportHole01;

        bool screw02Supporting =
            Screw02.CurrentHole == SupportHole02;

        int supportCount = 0;

        if (screw01Supporting)
            supportCount++;

        if (screw02Supporting)
            supportCount++;

        // İki vida da yerindeyse tahta sabit.
        if (supportCount == 2)
        {
            MakeFixed();
            return;
        }

        // Tek vida kaldıysa o vida menteşe olur.
        if (supportCount == 1)
        {
            if (screw01Supporting)
                ActivateHinge(Screw01, SupportHole01);
            else
                ActivateHinge(Screw02, SupportHole02);

            return;
        }

        // İki vida da çıkınca tahta serbest düşer.
        ReleasePlank();
    }

    void MakeFixed()
    {
        if (hinge.enabled)
            hinge.enabled = false;

        RestorePivotCollision();

        body.bodyType = RigidbodyType2D.Kinematic;
    }

    void ActivateHinge(Screw pivotScrew, Hole pivotHole)
    {
        if (hinge.enabled && currentPivotScrew == pivotScrew)
            return;

        RestorePivotCollision();

        currentPivotScrew = pivotScrew;
        currentPivotCollider =
            pivotScrew.GetComponent<Collider2D>();

        if (plankCollider != null &&
            currentPivotCollider != null)
        {
            Physics2D.IgnoreCollision(
                plankCollider,
                currentPivotCollider,
                true
            );
        }

        Vector3 worldPivot =
            pivotHole.transform.position;

        Vector3 localPivot =
            transform.InverseTransformPoint(worldPivot);

        hinge.enabled = false;

        hinge.connectedBody = null;
        hinge.autoConfigureConnectedAnchor = false;

        hinge.anchor =
            new Vector2(localPivot.x, localPivot.y);

        hinge.connectedAnchor =
            new Vector2(worldPivot.x, worldPivot.y);

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 2.5f;

        hinge.enabled = true;

        body.WakeUp();
    }

    void ReleasePlank()
    {
        isReleased = true;

        hinge.enabled = false;

        RestorePivotCollision();

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 2.5f;

        body.WakeUp();
    }

    void RestorePivotCollision()
    {
        if (plankCollider != null &&
            currentPivotCollider != null)
        {
            Physics2D.IgnoreCollision(
                plankCollider,
                currentPivotCollider,
                false
            );
        }

        currentPivotCollider = null;
        currentPivotScrew = null;
    }
}