namespace MystenLabs.Sui.Transactions;

using MystenLabs.Sui.JsonRpc.Models;

/// <summary>
/// Extension methods for <see cref="IObjectCache"/> (e.g. apply transaction effects to update cache).
/// </summary>
public static class ObjectCacheExtensions
{
    /// <summary>
    /// Updates the cache from transaction block effects: adds created and mutated object refs,
    /// removes deleted object IDs. Entries are added with owner and initialSharedVersion null (owned/immutable refs).
    /// </summary>
    /// <param name="cache">Cache to update.</param>
    /// <param name="effects">Transaction effects (e.g. from <see cref="SuiTransactionBlockResponse.Effects"/>).</param>
    /// <exception cref="ArgumentNullException">If <paramref name="cache"/> or <paramref name="effects"/> is null.</exception>
    public static void ApplyEffects(this IObjectCache cache, SuiTransactionBlockEffects effects)
    {
        if (cache == null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (effects == null)
        {
            throw new ArgumentNullException(nameof(effects));
        }

        ApplyCreatedOrMutated(cache, effects.Created);
        ApplyCreatedOrMutated(cache, effects.Mutated);

        if (effects.Deleted != null)
        {
            foreach (SuiObjectRefEffect deleted in effects.Deleted)
            {
                if (!string.IsNullOrEmpty(deleted?.ObjectId))
                {
                    cache.DeleteObject(deleted.ObjectId);
                }
            }
        }
    }

    private static void ApplyCreatedOrMutated(IObjectCache cache, SuiEffectRefEntry[]? entries)
    {
        if (entries == null)
        {
            return;
        }

        foreach (SuiEffectRefEntry entry in entries)
        {
            SuiObjectRefEffect? refEffect = entry?.Reference;
            if (refEffect == null || string.IsNullOrEmpty(refEffect.ObjectId))
            {
                continue;
            }

            string version = refEffect.Version ?? "0";
            string digest = refEffect.Digest ?? "";
            var cacheEntry = new ObjectCacheEntry(refEffect.ObjectId, version, digest, null, null);
            cache.AddObject(cacheEntry);
        }
    }

    /// <summary>
    /// Updates the cache from a transaction block response: if <paramref name="response"/> has effects,
    /// applies them via <see cref="ApplyEffects(IObjectCache, SuiTransactionBlockEffects)"/>.
    /// </summary>
    /// <param name="cache">Cache to update.</param>
    /// <param name="response">Transaction block response (e.g. from executeTransactionBlock or getTransactionBlock).</param>
    /// <exception cref="ArgumentNullException">If <paramref name="cache"/> or <paramref name="response"/> is null.</exception>
    public static void ApplyEffects(this IObjectCache cache, SuiTransactionBlockResponse response)
    {
        if (cache == null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        if (response.Effects != null)
        {
            cache.ApplyEffects(response.Effects);
        }
    }
}
