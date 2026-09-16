using System.Collections.Generic;

namespace WoodScrew.PhysicsCore
{
    /// <summary>
    /// Single source of truth for puzzle-result rules.
    /// No GameObject, MonoBehaviour, Rigidbody2D,
    /// Collider2D or Physics2D calls.
    /// </summary>
    public static class WoodScrewPhysicsCore
    {
        // Rule 2, 3, 4:
        // 2+ supports = fixed
        // 1 support   = pivot
        // 0 support   = fall
        public static PlankMotionState
            GetMotionState(
                int supportCount)
        {
            if (supportCount >= 2)
            {
                return
                    PlankMotionState.Fixed;
            }

            if (supportCount == 1)
            {
                return
                    PlankMotionState.Pivoting;
            }

            return
                PlankMotionState.Falling;
        }

        // Rule 3:
        // Exactly one support means that screw is the pivot.
        public static bool TryGetPivotScrewId(
            IReadOnlyList<int>
                supportingScrewIds,
            out int pivotScrewId)
        {
            pivotScrewId =
                -1;

            if (supportingScrewIds == null ||
                supportingScrewIds.Count != 1)
            {
                return false;
            }

            pivotScrewId =
                supportingScrewIds[0];

            return true;
        }

        // Rule 1 - current pose:
        // If the real circular anchor area touches the plank,
        // the anchor is not a legal target.
        public static bool IsAnchorBlockedAtPose(
            CoreVector2 anchorPosition,
            float anchorRadius,
            CoreOrientedRect plankPose)
        {
            CoreCircle anchor =
                new CoreCircle(
                    anchorPosition,
                    anchorRadius
                );

            return
                PhysicsCoreGeometry
                    .RectOverlapsCircle(
                        plankPose,
                        anchor
                    );
        }

        // Rule 1 - swing path:
        // If the plank covers the anchor at any sampled point
        // of the swing, the target is blocked during that motion.
        public static bool IsAnchorBlockedDuringSwing(
            CoreVector2 anchorPosition,
            float anchorRadius,
            CoreOrientedRect startPose,
            CoreVector2 pivot,
            float totalAngleDeltaDegrees,
            int sweepSteps = 32)
        {
            if (sweepSteps < 1)
            {
                sweepSteps =
                    1;
            }

            for (int step = 0;
                 step <= sweepSteps;
                 step++)
            {
                float t =
                    (float)step /
                    sweepSteps;

                float angleDelta =
                    totalAngleDeltaDegrees *
                    t;

                CoreVector2 center =
                    PhysicsCoreGeometry
                        .GetCenterAfterPivotRotation(
                            startPose,
                            pivot,
                            angleDelta
                        );

                CoreOrientedRect pose =
                    new CoreOrientedRect(
                        center,
                        startPose.Size,
                        startPose.RotationDegrees +
                        angleDelta
                    );

                if (IsAnchorBlockedAtPose(
                        anchorPosition,
                        anchorRadius,
                        pose))
                {
                    return true;
                }
            }

            return false;
        }

        // Rule 5:
        // Sweep collision catches a circle obstacle that lies
        // between the start and end poses.
        public static bool SweepHitsCircle(
            CoreOrientedRect startPose,
            CoreVector2 pivot,
            float totalAngleDeltaDegrees,
            CoreCircle obstacle,
            float collisionSkin = 0f,
            int sweepSteps = 48)
        {
            if (sweepSteps < 1)
            {
                sweepSteps =
                    1;
            }

            for (int step = 0;
                 step <= sweepSteps;
                 step++)
            {
                float t =
                    (float)step /
                    sweepSteps;

                float angleDelta =
                    totalAngleDeltaDegrees *
                    t;

                CoreVector2 center =
                    PhysicsCoreGeometry
                        .GetCenterAfterPivotRotation(
                            startPose,
                            pivot,
                            angleDelta
                        );

                CoreOrientedRect pose =
                    new CoreOrientedRect(
                        center,
                        startPose.Size,
                        startPose.RotationDegrees +
                        angleDelta
                    );

                if (PhysicsCoreGeometry
                    .RectOverlapsCircle(
                        pose,
                        obstacle,
                        collisionSkin))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
