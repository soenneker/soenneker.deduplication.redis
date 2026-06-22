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
    private const string DefaultCacheKey = "dedupe";

    private readonly IRedisUtil _redisUtil;
    private readonly long _seed;

    public RedisDedupe(IRedisUtil redisUtil, string cacheKey = DefaultCacheKey, TimeSpan? expiration = null,
        long seed = 0)
    {
        ArgumentNullException.ThrowIfNull(redisUtil);

        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be null or whitespace.", nameof(cacheKey));

        if (expiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration), "Expiration must be greater than zero.");

        _redisUtil = redisUtil;
        CacheKey = cacheKey;
        Expiration = expiration;
        _seed = seed;
    }

    public string CacheKey { get; }

    public TimeSpan? Expiration { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryMarkSeen(string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryMarkSeen(value.AsSpan(), cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryMarkSeen(ReadOnlySpan<char> value, CancellationToken cancellationToken = default) =>
        TryMarkHashSeen(XxHash3Util.HashCharsToUInt64(value, _seed), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryMarkSeenUtf8(ReadOnlySpan<byte> utf8, CancellationToken cancellationToken = default) =>
        TryMarkHashSeen(XxHash3Util.HashUtf8ToUInt64(utf8, _seed), cancellationToken);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> Contains(string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Contains(value.AsSpan(), cancellationToken);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> Contains(ReadOnlySpan<char> value, CancellationToken cancellationToken = default) =>
        ContainsHash(XxHash3Util.HashCharsToUInt64(value, _seed), cancellationToken);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> ContainsUtf8(ReadOnlySpan<byte> utf8, CancellationToken cancellationToken = default) =>
        ContainsHash(XxHash3Util.HashUtf8ToUInt64(utf8, _seed), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryRemove(string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryRemove(value.AsSpan(), cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryRemove(ReadOnlySpan<char> value, CancellationToken cancellationToken = default) =>
        TryRemoveHash(XxHash3Util.HashCharsToUInt64(value, _seed), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> TryRemoveUtf8(ReadOnlySpan<byte> utf8, CancellationToken cancellationToken = default) =>
        TryRemoveHash(XxHash3Util.HashUtf8ToUInt64(utf8, _seed), cancellationToken);

    private async ValueTask<bool> TryMarkHashSeen(ulong hash, CancellationToken cancellationToken)
    {
        string redisKey = BuildRedisKey(hash);

        return await _redisUtil.SetIfNotExists(redisKey, "1", Expiration, cancellationToken).NoSync();
    }

    private async ValueTask<bool> ContainsHash(ulong hash, CancellationToken cancellationToken)
    {
        string redisKey = BuildRedisKey(hash);

        string? value = await _redisUtil.GetString(redisKey, cancellationToken).NoSync();

        return value is not null;
    }

    private async ValueTask<bool> TryRemoveHash(ulong hash, CancellationToken cancellationToken)
    {
        string redisKey = BuildRedisKey(hash);

        string? value = await _redisUtil.GetString(redisKey, cancellationToken).NoSync();

        if (value is null)
            return false;

        await _redisUtil.Remove(redisKey, cancellationToken: cancellationToken).NoSync();

        return true;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string BuildRedisKey(ulong hash) => RedisUtil.BuildKey(CacheKey, hash.ToString("x16"));
}