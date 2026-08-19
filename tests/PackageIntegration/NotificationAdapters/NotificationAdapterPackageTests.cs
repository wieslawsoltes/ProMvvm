namespace ProMvvm.PackageIntegration;

public sealed class NotificationAdapterPackageTests
{
    [Fact]
    public void ExplicitAdapterObservesModelsWithoutInpcOrAServiceLocator()
    {
        var model = new AdapterModel();
        var values = new RecordingObserver<int>();
        var adapter = PropertyNotificationAdapters.Create<AdapterModel>(
            static (source, callback) => source.Subscribe(callback));

        using var subscription = model.WhenAnyValue(AdapterModelPropertyPaths.Value, adapter)
            .Subscribe(values);

        model.Value = 2;
        model.Value = 2;

        Assert.Equal([1, 2], values.Values);
    }
}
