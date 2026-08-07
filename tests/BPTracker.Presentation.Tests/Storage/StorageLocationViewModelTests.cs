using BPTracker.Presentation.Storage;
using BPTracker.TestSupport;

namespace BPTracker.Presentation.Tests.Storage;

public sealed class StorageLocationViewModelTests
{
    private readonly TestStorageLocation _location = new();

    private StorageLocationViewModel CreateViewModel() => new(_location);

    [Fact]
    public void ShowsTheFolderTheUserCanFind() =>
        CreateViewModel().DataFolder.ShouldBe(_location.DataFolder);

    [Fact]
    public void ShowsWhichFileThisDeviceWrites() =>
        CreateViewModel().DeviceJournalPath.ShouldEndWith("readings-test0001.ndjson");

    [Fact]
    public void ShowsTheDeviceId() =>
        CreateViewModel().DeviceId.ShouldBe("test0001");

    [Fact]
    public void ChangingTheFolderUpdatesWhatIsShown()
    {
        var viewModel = CreateViewModel();

        viewModel.ChangeFolder(@"D:\Sync\BP").ShouldBeTrue();

        viewModel.DataFolder.ShouldBe(@"D:\Sync\BP");
        viewModel.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public void ChangingTheFolderRaisesChangedSoTheHostReloads()
    {
        var viewModel = CreateViewModel();
        var raised = false;
        viewModel.Changed += (_, _) => raised = true;

        viewModel.ChangeFolder(@"D:\Sync\BP");

        raised.ShouldBeTrue();
    }

    [Fact]
    public void ChangingTheFolderNotifiesBoundPaths()
    {
        var viewModel = CreateViewModel();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.ChangeFolder(@"D:\Sync\BP");

        changed.ShouldContain(nameof(StorageLocationViewModel.DataFolder));
        changed.ShouldContain(nameof(StorageLocationViewModel.DeviceJournalPath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankFolderIsIgnored(string? folder)
    {
        var viewModel = CreateViewModel();

        viewModel.ChangeFolder(folder).ShouldBeFalse();
        viewModel.DataFolder.ShouldBe(_location.DataFolder);
    }

    [Fact]
    public void AnUnusableFolderIsReportedRatherThanThrown()
    {
        _location.FailWith = new UnauthorizedAccessException("no permission");
        var viewModel = CreateViewModel();

        viewModel.ChangeFolder(@"D:\Locked").ShouldBeFalse();

        viewModel.StatusMessage.ShouldNotBeNull().ShouldContain("no permission");
    }

    [Fact]
    public void DetachStopsListening()
    {
        var viewModel = CreateViewModel();
        var raised = false;
        viewModel.Changed += (_, _) => raised = true;

        viewModel.Detach();
        _location.SetDataFolder(@"D:\Elsewhere");

        raised.ShouldBeFalse();
    }

    [Fact]
    public void ConstructorRejectsNullLocation() =>
        Should.Throw<ArgumentNullException>(() => new StorageLocationViewModel(null!));
}
