using System;

namespace WoodScrew.PhysicsCore
{
    public struct CoreVector2
    {
        public float X;
        public float Y;

        public CoreVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float SqrMagnitude
        {
            get { return X * X + Y * Y; }
        }

        public static CoreVector2 operator +(CoreVector2 a, CoreVector2 b)
        {
            return new CoreVector2(a.X + b.X, a.Y + b.Y);
        }

        public static CoreVector2 operator -(CoreVector2 a, CoreVector2 b)
        {
            return new CoreVector2(a.X - b.X, a.Y - b.Y);
        }

        public static CoreVector2 operator *(CoreVector2 a, float value)
        {
            return new CoreVector2(a.X * value, a.Y * value);
        }
    }
}
