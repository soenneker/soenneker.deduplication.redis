using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Deduplication.Redis.Abstract;
using Soenneker.Extensions.ValueTask;
using Soenneker.Hashing.XxHash;
using Soenneker.Redis.Util;
using Soenneker.Redis.Util.Abstract;

namespace Soenneker.Deduplication.Redis;

/// <inheritdoc cref="IRedisDedupe"/>
public sealed class RedisDedupe : IRedisDedupe
{
    private readonly IRedisUtil _redisUtil;

    public RedisDedupe(IRedisUtil redisUtil)
    {
        ArgumentNullException.ThrowIfNull(redisUtil);

        _redisUtil = redisUtil;
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

        return await _redisUtil.SetIfNotExists(redisKey, "1", expiration, cancellationToken).NoSync();
    }

    private async ValueTask<bool> ContainsHash(string cacheKey, ulong hash, CancellationToken cancellationToken)
    {
        string redisKey = BuildRedisKey(cacheKey, hash);

        string? value = await _redisUtil.GetString(redisKey, cancellationToken).NoSync();

        return value is not null;
    }

    private async ValueTask<bool> TryRemoveHash(string cacheKey, ulong hash, CancellationToken cancellationToken)
    {
        string redisKey = BuildRedisKey(cacheKey, hash);

        string? value = await _redisUtil.GetString(redisKey, cancellationToken).NoSync();

        if (value is null)
            return false;

        await _redisUtil.Remove(redisKey, cancellationToken: cancellationToken).NoSync();

        return true;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildRedisKey(string cacheKey, ulong hash)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be null or whitespace.", nameof(cacheKey));

        return RedisUtil.BuildKey(cacheKey, hash.ToString("x16"));
    }
}
