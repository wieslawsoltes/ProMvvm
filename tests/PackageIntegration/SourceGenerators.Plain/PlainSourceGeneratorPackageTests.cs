namespace ProMvvm.PackageIntegration;

public sealed class PlainSourceGeneratorPackageTests
{
    [Fact]
    public void ExplicitGeneratorProducesDescriptorsAndArityTwelveOverloads()
    {
        var model = new PlainModel();
        var values = new RecordingObserver<int>();

        using var subscription = model.WhenAnyValue(
                PlainModelPropertyPaths.Count, PlainModelPropertyPaths.Count,
                PlainModelPropertyPaths.Count, PlainModelPropertyPaths.Count,
                PlainModelPropertyPaths.Count, PlainModelPropertyPaths.Count,
                PlainModelPropertyPaths.Count, PlainModelPropertyPaths.Count,
                PlainModelPropertyPaths.Count, PlainModelPropertyPaths.Count,
                PlainModelPropertyPaths.Count, PlainModelPropertyPaths.Count,
                static (a, b, c, d, e, f, g, h, i, j, k, l) =>
                    a + b + c + d + e + f + g + h + i + j + k + l)
            .Subscribe(values);

        model.Count = 2;

        Assert.Equal(13, values.Values.Count);
        Assert.Equal(12, values.Values[0]);
        Assert.Equal(24, values.Values[^1]);
    }
}
