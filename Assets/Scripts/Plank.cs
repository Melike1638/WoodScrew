using UnityEngine;
using System.Collections;

public class Plank : MonoBehaviour
{
    public Screw Screw01;
    public Screw Screw02;
    public Hole GoalHole;
    public Hole Screw01OldHole;

    public LevelFlow LevelFlow;

    private bool hasPivoted = false;
    private bool isAnimating = false;

    public void OnScrewMoved(Screw movedScrew, Hole newHole)
    {
        if (isAnimating)
            return;

        if (!hasPivoted &&
            movedScrew == Screw01 &&
            newHole == GoalHole)
        {
            Screw01.CanSelect = false;
            StartCoroutine(SwingDown());
        }
        else if (
            hasPivoted &&
            movedScrew == Screw02 &&
            newHole == Screw01OldHole)
        {
            Screw02.CanSelect = false;
            StartCoroutine(FallDown());
        }
    }

    IEnumerator SwingDown()
    {
        isAnimating = true;

        Vector3 pivotPoint =
            Screw02.transform.position;

        float angle = 0f;
        float angularVelocity = 0f;
        float gravityStrength = 260f;
        float damping = 0.985f;
        float duration = 1.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float gravityTorque =
                Mathf.Cos(angle * Mathf.Deg2Rad) *
                gravityStrength;

            angularVelocity +=
                gravityTorque * Time.deltaTime;

            angularVelocity *= damping;

            float angleStep =
                angularVelocity * Time.deltaTime;

            transform.RotateAround(
                pivotPoint,
                Vector3.forward,
                angleStep
            );

            angle += angleStep;

            yield return null;
        }

        hasPivoted = true;
        Screw02.CanSelect = true;
        isAnimating = false;
    }

    IEnumerator FallDown()
    {
        isAnimating = true;

        Vector3 startPosition =
            transform.position;

        Vector3 endPosition =
            startPosition + Vector3.down * 7f;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float gravityT = t * t;

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    gravityT
                );

            yield return null;
        }

        transform.position = endPosition;

        // Bölüm akışına:
        // "Bu tahta tamamlandı" diye haber verir.
        if (LevelFlow != null)
            LevelFlow.PlankCompleted();

        isAnimating = false;
    }
}