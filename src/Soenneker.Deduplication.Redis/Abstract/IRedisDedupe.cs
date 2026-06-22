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
    /// The Redis key prefix used for this dedupe scope.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// The expiration applied when a value is first marked as seen. <c>null</c> means values do not expire automatically.
    /// </summary>
    TimeSpan? Expiration { get; }

    /// <summary>
    /// Attempts to mark the specified value as seen.
    /// </summary>
    /// <returns><c>true</c> if the value was not previously seen; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryMarkSeen(string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to mark the specified character span as seen.
    /// </summary>
    /// <returns><c>true</c> if the value was not previously seen; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryMarkSeen(ReadOnlySpan<char> value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to mark the specified UTF-8 payload as seen.
    /// </summary>
    /// <returns><c>true</c> if the value was not previously seen; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryMarkSeenUtf8(ReadOnlySpan<byte> utf8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified value currently exists in Redis.
    /// </summary>
    [Pure]
    ValueTask<bool> Contains(string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified character span currently exists in Redis.
    /// </summary>
    [Pure]
    ValueTask<bool> Contains(ReadOnlySpan<char> value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified UTF-8 payload currently exists in Redis.
    /// </summary>
    [Pure]
    ValueTask<bool> ContainsUtf8(ReadOnlySpan<byte> utf8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove the specified value from Redis.
    /// </summary>
    /// <returns><c>true</c> if the value existed before removal; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryRemove(string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove the specified character span from Redis.
    /// </summary>
    /// <returns><c>true</c> if the value existed before removal; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryRemove(ReadOnlySpan<char> value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove the specified UTF-8 payload from Redis.
    /// </summary>
    /// <returns><c>true</c> if the value existed before removal; otherwise <c>false</c>.</returns>
    ValueTask<bool> TryRemoveUtf8(ReadOnlySpan<byte> utf8, CancellationToken cancellationToken = default);
}
