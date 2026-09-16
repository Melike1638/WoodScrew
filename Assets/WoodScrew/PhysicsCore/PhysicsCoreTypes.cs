namespace WoodScrew.PhysicsCore
{
    public enum PlankMotionState
    {
        Falling = 0,
        Pivoting = 1,
        Fixed = 2
    }

    public struct CoreOrientedRect
    {
        public CoreVector2 Center;
        public CoreVector2 Size;
        public float RotationDegrees;

        public CoreOrientedRect(
            CoreVector2 center,
            CoreVector2 size,
            float rotationDegrees)
        {
            Center = center;
            Size = size;
            RotationDegrees = rotationDegrees;
        }
    }

    public struct CoreCircle
    {
        public CoreVector2 Center;
        public float Radius;

        public CoreCircle(CoreVector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
