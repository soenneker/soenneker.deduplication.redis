[![](https://img.shields.io/nuget/v/soenneker.deduplication.redis.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.redis/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.redis/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.redis/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.deduplication.redis.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.redis/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.redis/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.redis/actions/workflows/codeql.yml)

# Soenneker.Deduplication.Redis

Distributed, time-bounded duplicate suppression using atomic Redis markers.

## Installation

```bash
dotnet add package Soenneker.Deduplication.Redis
```

## Registration

```csharp
using Soenneker.Deduplication.Redis.Registrars;

services.AddRedisDedupeAsSingleton();
```

This also registers `Soenneker.Redis.Client`; configure its Redis connection using that package’s normal configuration.

## Usage

```csharp
using Soenneker.Deduplication.Redis.Abstract;

public sealed class WebhookConsumer(IRedisDedupe dedupe)
{
    public async ValueTask Handle(string eventId, CancellationToken cancellationToken)
    {
        bool firstDelivery = await dedupe.TryMarkSeen(
            cacheKey: "dedupe:webhooks:stripe",
            cacheValue: eventId,
            expiration: TimeSpan.FromHours(24),
            cancellationToken);

        if (!firstDelivery)
            return;

        await ProcessWebhook(cancellationToken);
    }
}
```

`TryMarkSeen` uses Redis `SET ... NX` with the TTL in the same command. Concurrent callers across processes get one `true` result while the marker exists; later callers get `false`. Redis connection and command failures propagate instead of being reported as duplicates.

## Lookup and removal

```csharp
bool seen = await dedupe.Contains("dedupe:webhooks:stripe", eventId, cancellationToken);
bool removed = await dedupe.TryRemove("dedupe:webhooks:stripe", eventId, cancellationToken);
```

`Contains` does not refresh the TTL. `TryRemove` performs one atomic Redis delete and returns whether a marker was deleted.

String and `ReadOnlySpan<char>` overloads hash UTF-16 characters. `TryMarkSeenUtf8`, `ContainsUtf8`, and `TryRemoveUtf8` hash the supplied bytes. Use the same representation for every operation on a value; the UTF-16 string `"abc"` and its UTF-8 bytes are different dedupe keys.

## Redis keys and retention

The stored key is:

```text
{cacheKey}:{lowercase 16-character XXH3-64 hash}
```

Only the 64-bit hash marker is stored as the value, but `cacheKey` remains visible in Redis. Choose a stable, non-secret namespace. Different inputs can theoretically collide, so use a full-key design when collision tolerance is unacceptable.

Always set an expiration for open-ended inputs unless permanent markers and their storage growth are intentional. A non-positive expiration is rejected; `null` creates a marker without automatic expiry.

## Delivery semantics

This package is a duplicate-suppression primitive, not an exactly-once transaction. The marker and your business operation are separate: a process can fail after claiming a value but before completing the work. For durable message processing, combine it with an inbox/outbox or another transactional idempotency design. Redis eviction, data loss, manual deletion, or TTL expiry can also make a value eligible again.
