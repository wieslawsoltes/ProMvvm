namespace ProMvvm.Tests;

public sealed class PropertyPathTests
{
    [Fact]
    public void CreateRejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() =>
            PropertyPath.Create<ObservableModel, string?>("", static model => model.Name));
        Assert.Throws<ArgumentNullException>(() =>
            PropertyPath.Create<ObservableModel, string?>(nameof(ObservableModel.Name), null!));
    }

    [Fact]
    public void ThenRejectsInvalidArguments()
    {
        var path = PropertyPath.Create<ObservableModel, ObservableModel?>(
            nameof(ObservableModel.Child),
            static model => model.Child);

        Assert.Throws<ArgumentException>(() =>
            path.Then<string?>(" ", static child => child!.Name));
        Assert.Throws<ArgumentNullException>(() =>
            path.Then<string?>(nameof(ObservableModel.Name), null!));
    }
}
