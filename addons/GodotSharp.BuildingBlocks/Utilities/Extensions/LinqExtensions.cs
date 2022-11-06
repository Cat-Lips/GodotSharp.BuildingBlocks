
namespace GodotSharp.BuildingBlocks
{
    public static class LinqExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var i = -1;
            foreach (var item in source)
                action(item, ++i);
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
            => source is null || !source.Any();

        public static TValue TryGet<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key)
            => key is not null && source.TryGetValue(key, out var value) ? value : default;

        public static bool TryGet<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key, out TValue value)
        {
            value = default;
            return key is not null && source.TryGetValue(key, out value);
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T exception)
        {
            foreach (var item in source)
            {
                if (!Equals(item, exception))
                    yield return item;
            }
        }

        public static byte[] Combine(this byte[][] arrays)
        {
            var bytes = new byte[arrays.Sum(a => a.Length)];
            var offset = 0;

            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, bytes, offset, array.Length);
                offset += array.Length;
            }

            return bytes;
        }

        public static IEnumerable<T> SelectRecursive<T>(this T root, Func<T, IEnumerable<T>> select)
        {
            foreach (var child in select(root))
            {
                yield return child;

                foreach (var sub in child.SelectRecursive(select))
                    yield return sub;
            }
        }
    }
}
