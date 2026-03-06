using FluentAssertions;
using Oras.Tui;
using Xunit;

namespace Oras.Tests.Tui;

/// <summary>
/// Tests for TuiCache — in-memory TTL cache with set/get, expiration,
/// pattern invalidation, and clear operations.
/// </summary>
public sealed class TuiCacheTests
{
    [Fact]
    public void Set_Get_RoundTrip_ReturnsValue()
    {
        var cache = new TuiCache();
        cache.Set("key1", "value1");

        var (value, found, fromCache) = cache.Get<string>("key1");

        value.Should().Be("value1");
        found.Should().BeTrue();
        fromCache.Should().BeTrue();
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsNotFound()
    {
        var cache = new TuiCache();

        var (value, found, fromCache) = cache.Get<string>("missing");

        value.Should().BeNull();
        found.Should().BeFalse();
        fromCache.Should().BeFalse();
    }

    [Fact]
    public void Set_Get_GenericTypes_WorksWithDifferentTypes()
    {
        var cache = new TuiCache();
        cache.Set("int-key", 42);
        cache.Set("list-key", new List<string> { "a", "b" });

        var (intVal, intFound, _) = cache.Get<int>("int-key");
        var (listVal, listFound, _) = cache.Get<List<string>>("list-key");

        intVal.Should().Be(42);
        intFound.Should().BeTrue();
        listVal.Should().HaveCount(2);
        listFound.Should().BeTrue();
    }

    [Fact]
    public async Task Get_ExpiredEntry_ReturnsNotFound()
    {
        var cache = new TuiCache();
        cache.Set("expires-fast", "data", ttl: TimeSpan.FromMilliseconds(50));

        // Wait for expiration
        await Task.Delay(100);

        var (value, found, fromCache) = cache.Get<string>("expires-fast");

        value.Should().BeNull();
        found.Should().BeFalse();
        fromCache.Should().BeFalse();
    }

    [Fact]
    public void Get_NotYetExpired_ReturnsValue()
    {
        var cache = new TuiCache();
        cache.Set("long-lived", "data", ttl: TimeSpan.FromMinutes(10));

        var (value, found, _) = cache.Get<string>("long-lived");

        value.Should().Be("data");
        found.Should().BeTrue();
    }

    [Fact]
    public void Invalidate_RemovesSpecificKey()
    {
        var cache = new TuiCache();
        cache.Set("keep", "a");
        cache.Set("remove", "b");

        cache.Invalidate("remove");

        cache.Get<string>("keep").Found.Should().BeTrue();
        cache.Get<string>("remove").Found.Should().BeFalse();
    }

    [Fact]
    public void InvalidatePattern_RemovesMatchingKeys()
    {
        var cache = new TuiCache();
        cache.Set("registry:docker.io:tags", "tag-data");
        cache.Set("registry:docker.io:manifests", "manifest-data");
        cache.Set("registry:gcr.io:tags", "gcr-data");

        cache.InvalidatePattern("docker.io");

        cache.Get<string>("registry:docker.io:tags").Found.Should().BeFalse();
        cache.Get<string>("registry:docker.io:manifests").Found.Should().BeFalse();
        cache.Get<string>("registry:gcr.io:tags").Found.Should().BeTrue();
    }

    [Fact]
    public void InvalidatePattern_CaseInsensitive()
    {
        var cache = new TuiCache();
        cache.Set("Registry:Docker.IO:tags", "data");

        cache.InvalidatePattern("docker.io");

        cache.Get<string>("Registry:Docker.IO:tags").Found.Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var cache = new TuiCache();
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        cache.Clear();

        cache.Get<int>("a").Found.Should().BeFalse();
        cache.Get<int>("b").Found.Should().BeFalse();
        cache.Get<int>("c").Found.Should().BeFalse();
    }

    [Fact]
    public void Set_OverwritesExistingEntry()
    {
        var cache = new TuiCache();
        cache.Set("key", "old");
        cache.Set("key", "new");

        var (value, _, _) = cache.Get<string>("key");
        value.Should().Be("new");
    }

    [Fact]
    public void Constructor_DefaultTtl_EntriesLiveForDefaultDuration()
    {
        // Default TTL is 5 minutes — entries should be available immediately
        var cache = new TuiCache();
        cache.Set("default-ttl", "value");

        var (value, found, _) = cache.Get<string>("default-ttl");
        found.Should().BeTrue();
        value.Should().Be("value");
    }

    [Fact]
    public async Task Constructor_CustomTtl_UsedAsDefault()
    {
        var cache = new TuiCache(defaultTtl: TimeSpan.FromMilliseconds(50));
        cache.Set("short-default", "data");

        await Task.Delay(100);

        cache.Get<string>("short-default").Found.Should().BeFalse();
    }
}
