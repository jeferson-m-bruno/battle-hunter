namespace BattleHunter.Core.Tests;

public class CoreAssemblyTests
{
    [Fact]
    public void Given_CoreAssembly_When_Loaded_Then_HasExpectedName()
    {
        var assembly = typeof(CoreAssembly).Assembly;

        Assert.Equal(CoreAssembly.Name, assembly.GetName().Name);
    }

    [Fact]
    public void Given_CoreAssembly_When_Inspected_Then_DoesNotReferenceUnityEngine()
    {
        var referenced = typeof(CoreAssembly).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(referenced, name => name.StartsWith("UnityEngine", StringComparison.Ordinal));
    }
}
