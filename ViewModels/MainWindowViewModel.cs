using Ookii.Dialogs.Wpf;
using osuCollectionDownloader.Handlers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Splat;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace osuCollectionDownloader.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public static MainWindowViewModel Instance { get; private set; }

    [Reactive]
    public string TitlebarName { get; set; } = "osu! Collection Downloader";

    [Reactive]
    public string OsuDirectory { get; set; }

    [Reactive]
    public string CollectionId { get; set; }

    [Reactive]
    public string CollectionDbOutput { get; set; }

    [ObservableAsProperty]
    public bool IsDownloading { get; set; }

    public ReactiveCommand<Unit, string> OpenOsuFolderDialog { get; }

    public ReactiveCommand<Unit, string> OpenCollectionDbOutputFolderDialog { get; }

    public ReactiveCommand<Unit, Unit> StartDownload { get; }

    public ReactiveCommand<Unit, Unit> StartCollectionDbDownload { get; }

    private DownloadHandler Downloader { get; }

    public MainWindowViewModel()
    {
        Instance = this;

        Locator.CurrentMutable.RegisterConstant(new DownloadHandler());
        Downloader = Locator.Current.GetService<DownloadHandler>();

        IObservable<bool> canStartDownload = this.WhenAnyValue(x => x.OsuDirectory, x => x.CollectionId,
                                                               selector: (osuDirectory, collectionId) => !string.IsNullOrWhiteSpace(osuDirectory) &&
                                                                                                         !string.IsNullOrWhiteSpace(collectionId) &&
                                                                                                         osuDirectory.Contains("osu!") &&
                                                                                                         collectionId.All(id => char.IsDigit(id)));

        IObservable<bool> canStartCollectionDbDownload = this.WhenAnyValue(x => x.OsuDirectory, x => x.CollectionId, x => x.CollectionDbOutput,
                                                                           selector: (osuDirectory, collectionId, collectionDbOutput) => !string.IsNullOrWhiteSpace(osuDirectory) &&
                                                                                                                                         !string.IsNullOrWhiteSpace(collectionId) &&
                                                                                                                                         !string.IsNullOrWhiteSpace(collectionDbOutput) &&
                                                                                                                                         osuDirectory.Contains("osu!") &&
                                                                                                                                         collectionId.All(id => char.IsDigit(id)));

        this.WhenAnyObservable(x => x.StartDownload.IsExecuting, x => x.StartCollectionDbDownload.IsExecuting)
            .ToPropertyEx(this, x => x.IsDownloading);

        StartDownload = ReactiveCommand.CreateFromTask(() => StartDownloadAsyncImpl(), canStartDownload);
        StartDownload.ThrownExceptions.Subscribe(ex =>
        {
            TitlebarName = "osu! Collection Downloader";
            Log.Error(ex, ex.Message);
        });

        StartCollectionDbDownload = ReactiveCommand.CreateFromTask(() => StartCollectionDbDownloadAsyncImpl(), canStartCollectionDbDownload);
        StartCollectionDbDownload.ThrownExceptions.Subscribe(ex =>
        {
            TitlebarName = "osu! Collection Downloader";
            Log.Error(ex, ex.Message);
        });

        OpenOsuFolderDialog = ReactiveCommand.Create(OpenOsuDirectoryDialogImpl);
        OpenOsuFolderDialog.Subscribe(value => OsuDirectory = value);

        OpenCollectionDbOutputFolderDialog = ReactiveCommand.Create(OpenCollectionDbOutputFolderDialogImpl);
        OpenCollectionDbOutputFolderDialog.Subscribe(value => CollectionDbOutput = value);
    }

    private async Task StartDownloadAsyncImpl()
        => await Downloader.DownloadBeatmapsAsync().ConfigureAwait(false);

    private async Task StartCollectionDbDownloadAsyncImpl()
    {
        await Downloader.DownloadBeatmapsAsync().ConfigureAwait(false);
        Downloader.GenerateCollection();
    }

    private string OpenOsuDirectoryDialogImpl()
    {
        VistaFolderBrowserDialog osuDirectoryFolderDialog = new()
        {
            RootFolder = Environment.SpecialFolder.ApplicationData,
            Description = "Select your osu! game directory.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
            Multiselect = false,
        };

        bool? result = osuDirectoryFolderDialog.ShowDialog();

        if (!result.HasValue || result == false)
        {
            return string.Empty;
        }

        return osuDirectoryFolderDialog.SelectedPath;
    }

    private string OpenCollectionDbOutputFolderDialogImpl()
    {
        VistaFolderBrowserDialog collectionDbOutputFolderDialog = new()
        {
            RootFolder = Environment.SpecialFolder.ApplicationData,
            Description = "Select a folder to save your collection.osdb file.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            Multiselect = false,
        };

        bool? result = collectionDbOutputFolderDialog.ShowDialog();

        if (!result.HasValue || result == false)
        {
            return string.Empty;
        }

        return collectionDbOutputFolderDialog.SelectedPath;
    }
}