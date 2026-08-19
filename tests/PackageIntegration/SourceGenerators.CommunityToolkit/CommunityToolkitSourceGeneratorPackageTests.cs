namespace ProMvvm.PackageIntegration;

public sealed class CommunityToolkitSourceGeneratorPackageTests
{
    [Fact]
    public void ExplicitGeneratorDiscoversToolkitObservablePropertyFields()
    {
        var model = new ToolkitModel();
        var values = new RecordingObserver<string>();

        using var subscription = model.WhenAnyValue(
                ToolkitModelPropertyPaths.Name,
                ToolkitModelPropertyPaths.Count,
                ToolkitModelPropertyPaths.Child,
                static (name, count, child) => $"{name}:{count}:{child.Value}")
            .Subscribe(values);

        model.Count = 2;

        Assert.Equal(["toolkit:1:2", "toolkit:2:2"], values.Values);
    }
}
