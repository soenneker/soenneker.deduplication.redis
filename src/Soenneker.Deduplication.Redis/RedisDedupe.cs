using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Deduplication.Redis.Abstract;
using Soenneker.Hashing.XxHash;
using Soenneker.Redis.Client.Abstract;
using StackExchange.Redis;

namespace Soenneker.Deduplication.Redis;

public sealed class RedisDedupe : IRedisDedupe
{
    private readonly IRedisClient _redisClient;

    public RedisDedupe(IRedisClient redisClient)
    {
        ArgumentNullException.ThrowIfNull(redisClient);

        _redisClient = redisClient;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryMarkSeen(string cacheKey, string cacheValue, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cacheValue);

        return TryMarkSeen(cacheKey, cacheValue.AsSpan(), expiration, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryMarkSeen(string cacheKey, ReadOnlySpan<char> cacheValue, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        TryMarkHashSeen(cacheKey, XxHash3Util.HashCharsToUInt64(cacheValue), expiration, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryMarkSeenUtf8(string cacheKey, ReadOnlySpan<byte> cacheValue, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        TryMarkHashSeen(cacheKey, XxHash3Util.HashUtf8ToUInt64(cacheValue), expiration, cancellationToken);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> Contains(string cacheKey, string cacheValue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cacheValue);

        return Contains(cacheKey, cacheValue.AsSpan(), cancellationToken);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> Contains(string cacheKey, ReadOnlySpan<char> cacheValue, CancellationToken cancellationToken = default) =>
        ContainsHash(cacheKey, XxHash3Util.HashCharsToUInt64(cacheValue), cancellationToken);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> ContainsUtf8(string cacheKey, ReadOnlySpan<byte> cacheValue, CancellationToken cancellationToken = default) =>
        ContainsHash(cacheKey, XxHash3Util.HashUtf8ToUInt64(cacheValue), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryRemove(string cacheKey, string cacheValue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cacheValue);

        return TryRemove(cacheKey, cacheValue.AsSpan(), cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryRemove(string cacheKey, ReadOnlySpan<char> cacheValue, CancellationToken cancellationToken = default) =>
        TryRemoveHash(cacheKey, XxHash3Util.HashCharsToUInt64(cacheValue), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryRemoveUtf8(string cacheKey, ReadOnlySpan<byte> cacheValue, CancellationToken cancellationToken = default) =>
        TryRemoveHash(cacheKey, XxHash3Util.HashUtf8ToUInt64(cacheValue), cancellationToken);

    private async ValueTask<bool> TryMarkHashSeen(string cacheKey, ulong hash, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        if (expiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration), "Expiration must be greater than zero.");

        string redisKey = BuildRedisKey(cacheKey, hash);
        ConnectionMultiplexer connection = await _redisClient.Get(cancellationToken).ConfigureAwait(false);
        IDatabase database = connection.GetDatabase();

        return await database.StringSetAsync(redisKey, "1", expiration, when: When.NotExists)
            .WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<bool> ContainsHash(string cacheKey, ulong hash, CancellationToken cancellationToken)
    {
        string redisKey = BuildRedisKey(cacheKey, hash);
        ConnectionMultiplexer connection = await _redisClient.Get(cancellationToken).ConfigureAwait(false);
        IDatabase database = connection.GetDatabase();

        return await database.KeyExistsAsync(redisKey).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<bool> TryRemoveHash(string cacheKey, ulong hash, CancellationToken cancellationToken)
    {
        string redisKey = BuildRedisKey(cacheKey, hash);
        ConnectionMultiplexer connection = await _redisClient.Get(cancellationToken).ConfigureAwait(false);
        IDatabase database = connection.GetDatabase();

        return await database.KeyDeleteAsync(redisKey).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildRedisKey(string cacheKey, ulong hash)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be null or whitespace.", nameof(cacheKey));

        return $"{cacheKey}:{hash:x16}";
    }
}
