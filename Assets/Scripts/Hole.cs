using System.Collections.Generic;
using UnityEngine;

public class Hole : MonoBehaviour
{
    public static readonly List<Hole>
        ActiveHoles =
            new List<Hole>();

    public Screw OccupyingScrew;

    public int DefinitionId = -1;
    public bool IsStorageHole = false;

    private CircleCollider2D holeCollider;
    private MeshRenderer holeRenderer;


    void OnEnable()
    {
        if (!ActiveHoles.Contains(this))
        {
            ActiveHoles.Add(this);
        }
    }


    void OnDisable()
    {
        ActiveHoles.Remove(this);
    }


    void Awake()
    {
        holeCollider =
            GetComponent<CircleCollider2D>();

        holeRenderer =
            GetComponentInChildren<MeshRenderer>();

        if (holeCollider != null)
        {
            holeCollider.isTrigger =
                true;
        }
    }


    void Start()
    {
        RefreshState();
    }


    void Update()
    {
        RefreshState();
    }


    public void Configure(
        int definitionId,
        bool isStorageHole)
    {
        DefinitionId =
            definitionId;

        IsStorageHole =
            isStorageHole;

        RefreshState();
    }


    public void SetOccupyingScrew(
        Screw screw)
    {
        OccupyingScrew =
            screw;

        RefreshState();
    }


    public bool CanReceiveScrew(
        Screw movingScrew = null)
    {
        if (OccupyingScrew != null &&
            OccupyingScrew != movingScrew)
        {
            return false;
        }

        return
            GeneratedPlankPhysics
                .CanInsertScrewAtHole(
                    this,
                    movingScrew
                );
    }


    private void RefreshState()
    {
        bool empty =
            OccupyingScrew == null;

        // Vida çıkınca delik ASLA kaybolmaz.
        // Delik sabit anchor noktasıdır.
        if (holeCollider != null)
        {
            holeCollider.isTrigger =
                true;

            holeCollider.enabled =
                empty;
        }

        if (holeRenderer != null)
        {
            holeRenderer.enabled =
                empty;
        }
    }


    void OnMouseDown()
    {
        if (Screw.SelectedScrew == null)
            return;

        Screw selected =
            Screw.SelectedScrew;

        if (!CanReceiveScrew(
                selected))
        {
            return;
        }

        selected.MoveToHole(
            this
        );
    }
}
