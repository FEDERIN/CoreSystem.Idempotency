using Xunit;

namespace Core.Idempotency.Testing;

public sealed class DummyTests
{
    [Fact]
    public void AlwaysPasses() => Assert.True(true);
}