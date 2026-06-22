using Soenneker.Deduplication.Redis.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Deduplication.Redis.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class RedisDedupeTests : HostedUnitTest
{
    private readonly IRedisDedupe _util;

    public RedisDedupeTests(Host host) : base(host)
    {
        _util = Resolve<IRedisDedupe>(true);
    }

    [Test]
    public void Default()
    {

    }
}
