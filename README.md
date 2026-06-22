[![](https://img.shields.io/nuget/v/soenneker.deduplication.redis.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.redis/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.redis/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.redis/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.deduplication.redis.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.redis/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.redis/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.redis/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Deduplication.Redis

### Distributed duplicate suppression backed by Redis.

## Installation

```bash
dotnet add package Soenneker.Deduplication.Redis
```

---

# Overview

`Soenneker.Deduplication.Redis` provides a **distributed deduplication utility** for workloads where multiple app instances need to suppress duplicates through a shared Redis store.

It lets you determine whether a value has already been seen in a named Redis keyspace without storing the original input value.

Inputs are hashed using **XXH3 (XxHash3)** and stored under Redis keys built from:

```text
{cacheKey}:{hash(cacheValue)}
```

Typical usage pattern:

* First time value appears -> **`TryMarkSeen()` returns `true`**
* Value appears again before expiration -> **returns `false`**
* Value appears after Redis expiration -> **returns `true` again**

---

# Key Features

* **Distributed deduplication through Redis**
* **Atomic first-seen checks using Redis `SET NX`**
* **Per-call Redis keyspace via `cacheKey`**
* **Optional per-call expiration**
* **XXH3 hashing for speed**
* **UTF-8 + UTF-16 support**
* **DI-friendly utility with dependency-only constructor**
* **Backed by `Soenneker.Redis.Util`**

Redis stores only a compact hash marker, not the original input value.

---

# Quick Start

Register the utility:

```csharp
using Soenneker.Deduplication.Redis.Registrars;

services.AddRedisDedupeAsSingleton();
```

Use it from DI:

```csharp
using Soenneker.Deduplication.Redis.Abstract;

public sealed class LeadProcessor
{
    private readonly IRedisDedupe _dedupe;

    public LeadProcessor(IRedisDedupe dedupe)
    {
        _dedupe = dedupe;
    }

    public async ValueTask<bool> ShouldProcess(string email, CancellationToken cancellationToken = default)
    {
        return await _dedupe.TryMarkSeen(
            cacheKey: "leads:campaign-123",
            cacheValue: email,
            expiration: TimeSpan.FromHours(1),
            cancellationToken: cancellationToken);
    }
}
```

The first call for a value returns `true`. Duplicate calls for the same `cacheKey` and `cacheValue` return `false` until the Redis key expires or is removed.

---

# API

## TryMarkSeen

Checks if the value has already been seen and records it atomically if not.

```csharp
bool added = await dedupe.TryMarkSeen("webhooks:stripe", eventId, TimeSpan.FromHours(24), cancellationToken);
bool added2 = await dedupe.TryMarkSeen("imports:batch-42", rowKey.AsSpan(), TimeSpan.FromMinutes(30), cancellationToken);
bool added3 = await dedupe.TryMarkSeenUtf8("events:kafka", utf8Bytes, TimeSpan.FromMinutes(10), cancellationToken);
```

Return value:

| Result  | Meaning                                      |
| ------- | -------------------------------------------- |
| `true`  | Value was not seen and was added             |
| `false` | Value already exists in the Redis keyspace   |

Expiration is optional. Passing `null` stores the marker without automatic Redis expiration.

---

## Contains

Checks if a value exists in a Redis dedupe keyspace.

```csharp
bool exists = await dedupe.Contains("webhooks:stripe", eventId, cancellationToken);
bool exists2 = await dedupe.Contains("imports:batch-42", rowKey.AsSpan(), cancellationToken);
bool exists3 = await dedupe.ContainsUtf8("events:kafka", utf8Bytes, cancellationToken);
```

These methods do not modify Redis.

---

## TryRemove

Manually removes a value from a Redis dedupe keyspace.

```csharp
bool removed = await dedupe.TryRemove("webhooks:stripe", eventId, cancellationToken);
bool removed2 = await dedupe.TryRemove("imports:batch-42", rowKey.AsSpan(), cancellationToken);
bool removed3 = await dedupe.TryRemoveUtf8("events:kafka", utf8Bytes, cancellationToken);
```

---

# Redis Behavior

`TryMarkSeen()` uses Redis `SET NX` semantics through `Soenneker.Redis.Util`.

This means the set-and-check operation is atomic:

* Redis creates the marker only when it does not already exist
* Redis applies the expiration as part of the same write
* Concurrent callers across app instances get one winner

Stored key shape:

```text
{cacheKey}:{xxh3-64-cacheValue-hash}
```

Example:

```text
webhooks:stripe:8fc2099f174d8a1b
```

---

# Hashing & Collisions

Inputs are deduplicated using **64-bit XXH3 hashes**.

Only the hash is stored in Redis. This keeps keys compact and avoids persisting the original value.

Collisions are theoretically possible with any fixed-size hash, but XXH3 64-bit is suitable for typical event, webhook, ingestion, and request deduplication workloads.

---

# Configuration

This package uses `Soenneker.Redis.Util`, so Redis connection configuration follows that package's conventions.

`RedisDedupe` is intended to be resolved from DI. Runtime dedupe behavior is supplied per call:

| Parameter     | Description                                  |
| ------------- | -------------------------------------------- |
| `cacheKey`    | Redis keyspace for this dedupe set           |
| `cacheValue`  | Value to hash and check                      |
| `expiration`  | Optional Redis TTL for first-seen markers    |

---

# When to Use

Ideal for:

* Distributed event deduplication
* Webhook replay suppression
* Message processing pipelines
* Lead or request ingestion guards
* Cross-instance duplicate suppression
* Temporary idempotency markers

---

# When Not to Use

Not recommended if:

* You need permanent deduplication without Redis persistence guarantees
* You need to store or inspect original values
* Hash collision risk must be absolutely zero
* A single-process in-memory dedupe is sufficient
