using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProMvvm.SourceGenerators;

namespace ProMvvm.SourceGenerators.Tests;

public sealed class ProMvvmGeneratorTests
{
    [Fact]
    public void GeneratesAccessibleOrdinaryAndGeneratedProperties()
    {
        const string source = """
            using ProMvvm;

            namespace CommunityToolkit.Mvvm.ComponentModel
            {
                public sealed class ObservablePropertyAttribute : System.Attribute { }
            }

            namespace ReactiveUI.SourceGenerators
            {
                public sealed class ReactiveAttribute : System.Attribute { }
            }

            namespace Demo
            {
                [GeneratePropertyPaths]
                public class Model
                {
                    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
                    private string _searchText = "";

                    [ReactiveUI.SourceGenerators.Reactive]
                    private int m_count;

                    public int Value { get; set; }
                    internal string Name { get; set; } = "";
                    private int Hidden { get; set; }
                    public static int StaticValue { get; set; }
                    public int this[int index] => index;

                    public string SearchText => _searchText;
                    public int Count => m_count;
                }
            }
            """;

        var result = Run(source, "Consumer");
        var generated = Assert.Single(result.Results[0].GeneratedSources).SourceText.ToString();

        Assert.Empty(result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Contains("public static class ModelPropertyPaths", generated, StringComparison.Ordinal);
        Assert.Contains("PropertyPath<global::Demo.Model, int> Value", generated, StringComparison.Ordinal);
        Assert.Contains("PropertyPath<global::Demo.Model, string> SearchText", generated, StringComparison.Ordinal);
        Assert.Contains("PropertyPath<global::Demo.Model, int> Count", generated, StringComparison.Ordinal);
        Assert.Contains("PropertyPath<global::Demo.Model, string> Name", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(" Hidden ", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(" StaticValue ", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(" Item ", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsGenericTargetsAndGeneratesRuntimeAritiesOnlyForRuntimeAssembly()
    {
        const string genericSource = """
            using ProMvvm;
            [GeneratePropertyPaths]
            public class GenericModel<T>
            {
                public T Value { get; set; } = default!;
            }
            """;

        var consumerResult = Run(genericSource, "Consumer");
        Assert.Contains(consumerResult.Diagnostics, static diagnostic => diagnostic.Id == "PMVVM001");
        Assert.Empty(consumerResult.Results[0].GeneratedSources);

        var runtimeResult = Run(string.Empty, "ProMvvm");
        var generated = Assert.Single(runtimeResult.Results[0].GeneratedSources).SourceText.ToString();
        Assert.Contains("T12", generated, StringComparison.Ordinal);
        Assert.Contains("IPropertyNotificationAdapter notificationAdapter", generated, StringComparison.Ordinal);
        Assert.Contains("CombineLatestObservable<T1, T2, T3, TResult>", generated, StringComparison.Ordinal);
    }

    private static GeneratorDriverRunResult Run(string source, string assemblyName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ??
                                throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        var references = trustedAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(GeneratePropertyPathsAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ProMvvmGenerator().AsSourceGenerator()],
            parseOptions: (CSharpParseOptions)syntaxTree.Options);

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }
}
