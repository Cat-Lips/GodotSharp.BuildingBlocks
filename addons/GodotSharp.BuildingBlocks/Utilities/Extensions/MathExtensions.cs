using Godot;

namespace GodotSharp.BuildingBlocks
{
    public static class MathExtensions
    {
        public static float DistanceSquaredFrom(this in Vector3 point, in Aabb volume)
        {
            return point.DistanceSquaredTo(new(
                Math.Clamp(point.X, volume.Position.X, volume.End.X),
                Math.Clamp(point.Y, volume.Position.Y, volume.End.Y),
                Math.Clamp(point.Z, volume.Position.Z, volume.End.Z)));
        }

        public static float DistanceSquaredFrom(this in Vector3 point, in Rect2I area)
        {
            return point.DistanceSquaredTo(new(
                Math.Clamp(point.X, area.Position.X, area.End.X),
                point.Y,
                Math.Clamp(point.Z, area.Position.Y, area.End.Y)));
        }

        public static void Normalize(this ref Vector3 v)
        {
            var num = v.LengthSquared();
            if (num is 0f)
            {
                v.X = v.Y = v.Z = 0f;
                return;
            }

            var num2 = Mathf.Sqrt(num);
            v.X /= num2;
            v.Y /= num2;
            v.Z /= num2;
        }

        public static T GetRandom<T>(this T[] array)
            => array[Random.Shared.Next() % array.Length];
    }
}
