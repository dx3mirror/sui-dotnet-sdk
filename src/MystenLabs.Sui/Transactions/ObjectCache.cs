namespace MystenLabs.Sui.Transactions;

using System.Collections.Concurrent;
using MystenLabs.Sui.Bcs;

/// <summary>
/// Cached object metadata (object ID, version, digest, owner) for use when resolving transaction inputs.
/// </summary>
/// <param name="ObjectId">Object ID (normalized).</param>
/// <param name="Version">Object version (e.g. from RPC).</param>
/// <param name="Digest">Object digest (Base58).</param>
/// <param name="Owner">Owner address for owned objects; <c>null</c> for shared/immutable.</param>
/// <param name="InitialSharedVersion">Initial shared version for shared objects; <c>null</c> otherwise.</param>
public sealed record ObjectCacheEntry(
    string ObjectId,
    string Version,
    string Digest,
    string? Owner,
    string? InitialSharedVersion);

/// <summary>
/// Cache for object metadata (e.g. object ID → version, digest) used when building or resolving transactions.
/// </summary>
public interface IObjectCache
{
    /// <summary>
    /// Gets cached metadata for a single object by ID.
    /// </summary>
    /// <param name="objectId">Object ID (normalized).</param>
    /// <returns>Cached entry or <c>null</c> if not found.</returns>
    ObjectCacheEntry? GetObject(string objectId);

    /// <summary>
    /// Gets cached metadata for multiple objects by ID.
    /// </summary>
    /// <param name="objectIds">Object IDs (normalized).</param>
    /// <returns>Array of entries in the same order; <c>null</c> where not cached.</returns>
    ObjectCacheEntry?[] GetObjects(IReadOnlyList<string> objectIds);

    /// <summary>
    /// Adds or updates a single object entry in the cache.
    /// </summary>
    /// <param name="entry">Object metadata to cache.</param>
    void AddObject(ObjectCacheEntry entry);

    /// <summary>
    /// Adds or updates multiple object entries.
    /// </summary>
    /// <param name="entries">Object metadata to cache.</param>
    void AddObjects(IReadOnlyList<ObjectCacheEntry> entries);

    /// <summary>
    /// Removes a single object from the cache.
    /// </summary>
    /// <param name="objectId">Object ID to remove.</param>
    void DeleteObject(string objectId);

    /// <summary>
    /// Removes multiple objects from the cache.
    /// </summary>
    /// <param name="objectIds">Object IDs to remove.</param>
    void DeleteObjects(IReadOnlyList<string> objectIds);

    /// <summary>
    /// Clears all cached object entries.
    /// </summary>
    void Clear();
}

/// <summary>
/// In-memory implementation of <see cref="IObjectCache"/> using concurrent dictionaries (thread-safe).
/// </summary>
public sealed class InMemoryObjectCache : IObjectCache
{
    private readonly ConcurrentDictionary<string, ObjectCacheEntry> _ownedOrReceiving = new();
    private readonly ConcurrentDictionary<string, ObjectCacheEntry> _sharedOrImmutable = new();

    /// <inheritdoc />
    public ObjectCacheEntry? GetObject(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            return null;
        }

        if (_ownedOrReceiving.TryGetValue(objectId, out ObjectCacheEntry? owned))
        {
            return owned;
        }

        if (_sharedOrImmutable.TryGetValue(objectId, out ObjectCacheEntry? shared))
        {
            return shared;
        }

        return null;
    }

    /// <inheritdoc />
    public ObjectCacheEntry?[] GetObjects(IReadOnlyList<string> objectIds)
    {
        if (objectIds == null)
        {
            throw new ArgumentNullException(nameof(objectIds));
        }

        var result = new ObjectCacheEntry?[objectIds.Count];
        for (int index = 0; index < objectIds.Count; index++)
        {
            result[index] = GetObject(objectIds[index]);
        }

        return result;
    }

    /// <inheritdoc />
    public void AddObject(ObjectCacheEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (entry.Owner != null)
        {
            _ownedOrReceiving[entry.ObjectId] = entry;
        }
        else
        {
            _sharedOrImmutable[entry.ObjectId] = entry;
        }
    }

    /// <inheritdoc />
    public void AddObjects(IReadOnlyList<ObjectCacheEntry> entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        for (int index = 0; index < entries.Count; index++)
        {
            AddObject(entries[index]);
        }
    }

    /// <inheritdoc />
    public void DeleteObject(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            return;
        }

        _ownedOrReceiving.TryRemove(objectId, out _);
        _sharedOrImmutable.TryRemove(objectId, out _);
    }

    /// <inheritdoc />
    public void DeleteObjects(IReadOnlyList<string> objectIds)
    {
        if (objectIds == null)
        {
            throw new ArgumentNullException(nameof(objectIds));
        }

        for (int index = 0; index < objectIds.Count; index++)
        {
            DeleteObject(objectIds[index]);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _ownedOrReceiving.Clear();
        _sharedOrImmutable.Clear();
    }
}

/// <summary>
/// Build plugin that resolves <see cref="CallArgUnresolvedObject"/> inputs using a fixed <see cref="IObjectCache"/>.
/// Add via <see cref="TransactionBuilder.AddBuildPlugin"/> so that <see cref="TransactionBuilder.Object(string)"/> inputs are resolved from cache before <see cref="TransactionBuilder.Build"/>.
/// </summary>
public static class ObjectCachePlugin
{
    /// <summary>
    /// Creates a build plugin that resolves unresolved object inputs from the given cache only (no RPC).
    /// </summary>
    /// <param name="cache">Cache to look up object metadata (version, digest, initialSharedVersion).</param>
    /// <returns>Plugin that replaces <see cref="CallArgUnresolvedObject"/> with <see cref="CallArgObject"/> when found in cache.</returns>
    public static TransactionPlugin Create(IObjectCache cache)
    {
        if (cache == null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return async (data, options, next) =>
        {
            if (data?.Inputs == null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            for (int index = 0; index < data.Inputs.Count; index++)
            {
                if (data.Inputs[index] is not CallArgUnresolvedObject unresolved)
                {
                    continue;
                }

                ObjectCacheEntry? entry = cache.GetObject(unresolved.ObjectId);
                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"Object {unresolved.ObjectId} not found in cache. Add it via IObjectCache.AddObject (e.g. from GetObjectAsync response).");
                }

                bool mutable = unresolved.Mutable ?? true;
                data.Inputs[index] = TransactionResolvingHelpers.ToCallArgObject(entry, mutable);
            }

            await next().ConfigureAwait(false);
        };
    }
}
