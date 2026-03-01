namespace MystenLabs.Sui.Utils;

/// <summary>
/// Splits a sequence into fixed-size chunks (batches).
/// </summary>
public static class Chunk
{
    /// <summary>
    /// Splits an array into chunks of the specified size. The last chunk may contain fewer elements.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="array">Source array.</param>
    /// <param name="size">Maximum number of elements per chunk. Must be positive.</param>
    /// <returns>Array of chunks; each inner array has at most <paramref name="size"/> elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than 1.</exception>
    public static T[][] ToChunks<T>(T[] array, int size)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (size < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Chunk size must be at least 1.");
        }

        int count = array.Length;
        if (count == 0)
        {
            return [];
        }

        int chunkCount = (count + size - 1) / size;
        var result = new T[chunkCount][];

        for (int index = 0; index < chunkCount; index++)
        {
            int start = index * size;
            int length = Math.Min(size, count - start);
            var chunk = new T[length];
            Array.Copy(array, start, chunk, 0, length);
            result[index] = chunk;
        }

        return result;
    }

    /// <summary>
    /// Splits a read-only list into chunks of the specified size. The last chunk may contain fewer elements.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="list">Source list.</param>
    /// <param name="size">Maximum number of elements per chunk. Must be positive.</param>
    /// <returns>Array of chunks; each inner array has at most <paramref name="size"/> elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than 1.</exception>
    public static T[][] ToChunks<T>(IReadOnlyList<T> list, int size)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (size < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Chunk size must be at least 1.");
        }

        int count = list.Count;
        if (count == 0)
        {
            return [];
        }

        int chunkCount = (count + size - 1) / size;
        var result = new T[chunkCount][];

        for (int index = 0; index < chunkCount; index++)
        {
            int start = index * size;
            int length = Math.Min(size, count - start);
            var chunk = new T[length];
            for (int offset = 0; offset < length; offset++)
            {
                chunk[offset] = list[start + offset];
            }

            result[index] = chunk;
        }

        return result;
    }
}
