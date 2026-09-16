using System;

namespace WoodScrew.PhysicsCore
{
    public static class PhysicsCoreGeometry
    {
        private const float Deg2Rad =
            (float)(Math.PI / 180.0);

        public static CoreVector2 Rotate(
            CoreVector2 vector,
            float degrees)
        {
            double radians =
                degrees *
                Deg2Rad;

            float cos =
                (float)Math.Cos(radians);

            float sin =
                (float)Math.Sin(radians);

            return new CoreVector2(
                vector.X * cos -
                vector.Y * sin,
                vector.X * sin +
                vector.Y * cos
            );
        }

        public static CoreVector2
            GetCenterAfterPivotRotation(
                CoreOrientedRect start,
                CoreVector2 pivot,
                float angleDeltaDegrees)
        {
            CoreVector2 radius =
                start.Center -
                pivot;

            return
                pivot +
                Rotate(
                    radius,
                    angleDeltaDegrees
                );
        }

        public static bool PointInsideExpandedRect(
            CoreVector2 point,
            CoreOrientedRect rect,
            float expansion)
        {
            CoreVector2 local =
                Rotate(
                    point -
                    rect.Center,
                    -rect.RotationDegrees
                );

            float halfX =
                rect.Size.X *
                0.5f +
                expansion;

            float halfY =
                rect.Size.Y *
                0.5f +
                expansion;

            return
                Math.Abs(local.X) <=
                    halfX &&
                Math.Abs(local.Y) <=
                    halfY;
        }

        /// <summary>
        /// Exact circle-vs-oriented-rectangle overlap.
        /// This matches the runtime anchor-clearance rule:
        /// nearest point on the plank rectangle is found first,
        /// then the real circular clearance radius is tested.
        /// </summary>
        public static bool RectOverlapsCircle(
            CoreOrientedRect rect,
            CoreCircle circle,
            float skin = 0f)
        {
            CoreVector2 local =
                Rotate(
                    circle.Center -
                    rect.Center,
                    -rect.RotationDegrees
                );

            float halfX =
                rect.Size.X *
                0.5f;

            float halfY =
                rect.Size.Y *
                0.5f;

            float closestX =
                Clamp(
                    local.X,
                    -halfX,
                    halfX
                );

            float closestY =
                Clamp(
                    local.Y,
                    -halfY,
                    halfY
                );

            float dx =
                local.X -
                closestX;

            float dy =
                local.Y -
                closestY;

            float radius =
                Math.Max(
                    0f,
                    circle.Radius +
                    skin
                );

            return
                dx * dx +
                dy * dy <
                radius *
                radius;
        }

        private static float Clamp(
            float value,
            float min,
            float max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }
}
