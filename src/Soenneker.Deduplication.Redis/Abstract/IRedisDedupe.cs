using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Deduplication.Redis.Abstract;

/// <summary>
/// Distributed duplicate suppression backed by Redis.
/// </summary>
public interface IRedisDedupe
{
    /// <summary>
    /// Attempts to mark <paramref name="cacheValue"/> as seen within <paramref name="cacheKey"/>.
    /// </summary>
    /// <param name="cacheKey">The Redis keyspace for this dedupe set.</param>
    /// <param name="cacheValue">The value to hash and mark as seen.</param>
    /// <param name="expiration">The expiration applied when the value is first marked as seen. <c>null</c> means no automatic expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the value was not previously seen in the cache key; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryMarkSeen(string cacheKey, string cacheValue, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to mark <paramref name="cacheValue"/> as seen within <paramref name="cacheKey"/>.
    /// </summary>
    /// <param name="cacheKey">The Redis keyspace for this dedupe set.</param>
    /// <param name="cacheValue">The value to hash and mark as seen.</param>
    /// <param name="expiration">The expiration applied when the value is first marked as seen. <c>null</c> means no automatic expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the value was not previously seen in the cache key; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryMarkSeen(string cacheKey, ReadOnlySpan<char> cacheValue, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to mark <paramref name="cacheValue"/> as seen within <paramref name="cacheKey"/>.
    /// </summary>
    /// <param name="cacheKey">The Redis keyspace for this dedupe set.</param>
    /// <param name="cacheValue">The UTF-8 value to hash and mark as seen.</param>
    /// <param name="expiration">The expiration applied when the value is first marked as seen. <c>null</c> means no automatic expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the value was not previously seen in the cache key; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryMarkSeenUtf8(string cacheKey, ReadOnlySpan<byte> cacheValue, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether <paramref name="cacheValue"/> currently exists within <paramref name="cacheKey"/>.
    /// </summary>
    [Pure]
    ValueTask<bool> Contains(string cacheKey, string cacheValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether <paramref name="cacheValue"/> currently exists within <paramref name="cacheKey"/>.
    /// </summary>
    [Pure]
    ValueTask<bool> Contains(string cacheKey, ReadOnlySpan<char> cacheValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether <paramref name="cacheValue"/> currently exists within <paramref name="cacheKey"/>.
    /// </summary>
    [Pure]
    ValueTask<bool> ContainsUtf8(string cacheKey, ReadOnlySpan<byte> cacheValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove <paramref name="cacheValue"/> from <paramref name="cacheKey"/>.
    /// </summary>
    /// <returns><c>true</c> if the value existed before removal; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryRemove(string cacheKey, string cacheValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove <paramref name="cacheValue"/> from <paramref name="cacheKey"/>.
    /// </summary>
    /// <returns><c>true</c> if the value existed before removal; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryRemove(string cacheKey, ReadOnlySpan<char> cacheValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove <paramref name="cacheValue"/> from <paramref name="cacheKey"/>.
    /// </summary>
    /// <returns><c>true</c> if the value existed before removal; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryRemoveUtf8(string cacheKey, ReadOnlySpan<byte> cacheValue, CancellationToken cancellationToken = default);
}
