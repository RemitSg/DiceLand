using NUnit.Framework;
using UnityEngine;

public class BattleComponentResolverTests
{
    private sealed class ResolverTestComponent : MonoBehaviour
    {
    }

    [Test]
    public void ResolveSceneOrHost_ReturnsExistingComponent()
    {
        GameObject host = new GameObject("host");
        ResolverTestComponent existing = host.AddComponent<ResolverTestComponent>();

        ResolverTestComponent resolved = BattleComponentResolver.ResolveSceneOrHost(
            current: existing,
            host: host,
            includeInactive: false,
            addIfMissing: true);

        Assert.AreSame(existing, resolved);
        Object.DestroyImmediate(host);
    }

    [Test]
    public void ResolveSceneOrHost_AddsComponentToHostWhenMissing()
    {
        GameObject host = new GameObject("host");

        ResolverTestComponent resolved = BattleComponentResolver.ResolveSceneOrHost<ResolverTestComponent>(
            current: null,
            host: host,
            includeInactive: false,
            addIfMissing: true);

        Assert.IsNotNull(resolved);
        Assert.AreSame(resolved, host.GetComponent<ResolverTestComponent>());
        Object.DestroyImmediate(host);
    }
}
