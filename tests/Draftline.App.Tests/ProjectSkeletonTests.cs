using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Components;

namespace Draftline.App.Tests;

/// <summary>
/// The frame the rest of the editor is built on. The app assembly has to exist, expose a root
/// component the host can mount, and target the framework the solution is pinned to - a skeleton
/// that fails any of these has nothing for the later stories to grow into.
/// </summary>
public sealed class ProjectSkeletonTests
{
    [Fact]
    public void The_root_component_the_host_mounts_is_a_razor_component()
    {
        var root = typeof(Draftline.App.App);

        Assert.True(
            typeof(IComponent).IsAssignableFrom(root),
            "Program.cs mounts Draftline.App.App on #app, so that type has to be a component.");
    }

    [Fact]
    public void The_app_assembly_targets_net10()
    {
        var target = typeof(Draftline.App.App).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.NotNull(target);
        Assert.Contains("v10.0", target!.FrameworkName, StringComparison.Ordinal);
    }
}
