using System.Collections.Generic;
using UnityEngine;

public class Screw : MonoBehaviour
{
    public static Screw SelectedScrew;

    public static readonly List<Screw>
        ActiveScrews =
            new List<Screw>();

    public Hole CurrentHole;
    public Plank OwnerPlank;

    public bool CanSelect = true;

    public Transform VisualTransform;

    private Rigidbody2D body;
    private Vector3 normalVisualScale;


    void OnEnable()
    {
        if (!ActiveScrews.Contains(this))
        {
            ActiveScrews.Add(this);
        }
    }


    void OnDisable()
    {
        ActiveScrews.Remove(this);

        if (SelectedScrew == this)
        {
            SelectedScrew =
                null;
        }
    }


    void Awake()
    {
        body =
            GetComponent<Rigidbody2D>();
    }


    void Start()
    {
        if (VisualTransform == null)
        {
            VisualTransform =
                transform;
        }

        normalVisualScale =
            VisualTransform.localScale;

        if (CurrentHole != null &&
            CurrentHole.OccupyingScrew != this)
        {
            CurrentHole.SetOccupyingScrew(
                this
            );
        }
    }


    void OnMouseDown()
    {
        if (!CanSelect)
            return;

        if (SelectedScrew != null &&
            SelectedScrew != this)
        {
            SelectedScrew
                .ResetSelectionVisual();
        }

        SelectedScrew =
            this;

        if (VisualTransform != null)
        {
            VisualTransform.localScale =
                normalVisualScale *
                1.18f;
        }
    }


    public void MoveToHole(
        Hole targetHole)
    {
        if (targetHole == null)
            return;

        if (!targetHole.CanReceiveScrew(
                this))
        {
            return;
        }

        GeneratedPlankPhysics
            .NotifyScrewRemoved(
                this
            );

        Hole previousHole =
            CurrentHole;

        if (previousHole != null &&
            previousHole.OccupyingScrew ==
            this)
        {
            previousHole.SetOccupyingScrew(
                null
            );
        }

        CurrentHole =
            null;

        Vector3 targetPosition =
            targetHole.transform.position;

        targetPosition.z =
            transform.position.z;

        if (body != null)
        {
            body.position =
                new Vector2(
                    targetPosition.x,
                    targetPosition.y
                );
        }

        transform.position =
            targetPosition;

        Physics2D.SyncTransforms();

        CurrentHole =
            targetHole;

        targetHole.SetOccupyingScrew(
            this
        );

        GeneratedPlankPhysics
            .NotifyScrewPlaced(
                targetHole,
                this
            );

        ResetSelectionVisual();

        SelectedScrew =
            null;

        if (OwnerPlank != null)
        {
            OwnerPlank.OnScrewMoved(
                this,
                targetHole
            );
        }
    }


    private void ResetSelectionVisual()
    {
        if (VisualTransform != null)
        {
            VisualTransform.localScale =
                normalVisualScale;
        }
    }
}
