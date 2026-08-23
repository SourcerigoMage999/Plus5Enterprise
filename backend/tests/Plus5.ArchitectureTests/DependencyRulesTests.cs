using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Plus5.ArchitectureTests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void DomainDoesNotDependOnOtherPlus5Projects()
    {
        AssertDependencies("Plus5.Domain");
    }

    [Fact]
    public void ApplicationDependsOnlyOnDomain()
    {
        AssertDependencies("Plus5.Application", "Plus5.Domain");
    }

    [Fact]
    public void InfrastructureDependsOnlyOnApplicationAndDomain()
    {
        AssertDependencies("Plus5.Infrastructure", "Plus5.Application", "Plus5.Domain");
    }

    [Fact]
    public void ApiDependsOnlyOnApplicationAndInfrastructure()
    {
        AssertDependencies("Plus5.Api", "Plus5.Application", "Plus5.Infrastructure");
    }

    private static void AssertDependencies(string assemblyName, params string[] allowedDependencies)
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        using var assemblyStream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(assemblyStream);
        var metadataReader = peReader.GetMetadataReader();

        var actualDependencies = metadataReader
            .AssemblyReferences
            .Select(handle => metadataReader.GetAssemblyReference(handle))
            .Select(reference => metadataReader.GetString(reference.Name))
            .Where(name => name.StartsWith("Plus5.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var forbiddenDependencies = actualDependencies
            .Except(allowedDependencies, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbiddenDependencies);
    }
}
