using CollectionManager.DataTypes;
using CollectionManager.Enums;
using CollectionManager.Modules.CollectionsManager;
using CollectionManager.Modules.FileIO;
using Flurl.Http;
using Ookii.Dialogs.Wpf;
using osuCollectionDownloader.Global;
using osuCollectionDownloader.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osuCollectionDownloader.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    [Reactive]
    public string TitlebarName { get; set; } = "osu!CollectorGrabber";

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

    private readonly OsuFileIo osuFileIo = new(new BeatmapExtension());

    public MainWindowViewModel()
    {
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
            TitlebarName = "osu!CollectorGrabber";
            Log.Error(ex, ex.Message);
        });

        StartCollectionDbDownload = ReactiveCommand.CreateFromTask(() => StartCollectionDbDownloadAsyncImpl(), canStartCollectionDbDownload);
        StartCollectionDbDownload.ThrownExceptions.Subscribe(ex =>
        {
            TitlebarName = "osu!CollectorGrabber";
            Log.Error(ex, ex.Message);
        });

        OpenOsuFolderDialog = ReactiveCommand.Create(OpenOsuDirectoryDialogImpl);
        OpenOsuFolderDialog.Subscribe(value => OsuDirectory = value);

        OpenCollectionDbOutputFolderDialog = ReactiveCommand.Create(OpenCollectionDbOutputFolderDialogImpl);
        OpenCollectionDbOutputFolderDialog.Subscribe(value => CollectionDbOutput = value);
    }

    private async Task StartDownloadAsyncImpl()
    {
        if (Process.GetProcessesByName("osu!").Any())
        {
            Process.GetProcessesByName("osu!").First().Kill();
            Log.Information("Closing osu! in order to import beatmaps.");
        }

        int totalBeatmapCount = 0;
        int beatmapPerPageCount = 0;
        long? cursor = 0;

        BeatmapCollection beatmapCollection;

        CollectionInfo collectionInfo = await $"https://osucollector.com/api/collections/{CollectionId}".GetJsonAsync<CollectionInfo>();

        do
        {
            beatmapCollection = await $"https://osucollector.com/api/collections/{CollectionId}/beatmapsv2?cursor={cursor}".GetJsonAsync<BeatmapCollection>();

            cursor = beatmapCollection.NextPageCursor;
            TitlebarName = $"osu!CollectorGrabber - Downloading: {collectionInfo.Name}";

            foreach (Models.Beatmap beatmap in beatmapCollection.Beatmaps)
            {
                if (beatmapPerPageCount >= 50)
                {
                    beatmapPerPageCount = 0;
                    continue;
                }

                try
                {
                    string beatmapFileName = $"{beatmap.BeatmapsetId} {ReplaceInvalidPathCharacters(beatmap.Beatmapset.ArtistUnicode)} - {ReplaceInvalidPathCharacters(beatmap.Beatmapset.TitleUnicode)}.osz";
                    string beatmapInOsuDirectory = Path.GetFullPath(Path.GetFileNameWithoutExtension(beatmapFileName), Path.GetFullPath("Songs", OsuDirectory));

                    if (!Directory.Exists(beatmapInOsuDirectory))
                    {
                        string beatmapInDownloadedBeatmapsDirectory = await $"https://beatconnect.io/b/{beatmap.BeatmapsetId}".DownloadFileAsync(Paths.DownloadedBeatmapsDirectory, beatmapFileName);

                        ZipFile.ExtractToDirectory(beatmapInDownloadedBeatmapsDirectory, beatmapInOsuDirectory);
                        File.Delete(beatmapInDownloadedBeatmapsDirectory);

                        Log.Information($"Imported {beatmap.Id} - {beatmap.Beatmapset.ArtistUnicode} - {beatmap.Beatmapset.TitleUnicode}");
                    }
                    else
                    {
                        Log.Information($"Skipping {beatmap.Id} - {beatmap.Beatmapset.ArtistUnicode} - {beatmap.Beatmapset.TitleUnicode} because it's already imported.");
                    }
                }
                catch
                {
                    continue;
                }
                finally
                {
                    beatmapPerPageCount++;
                    totalBeatmapCount++;
                }
            }

        } while (beatmapCollection.HasMore);

        Log.Information($"Finished downloading {totalBeatmapCount} beatmaps.");
        TitlebarName = "osu!CollectorGrabber";
    }

    private async Task StartCollectionDbDownloadAsyncImpl()
    {
        if (Process.GetProcessesByName("osu!").Any())
        {
            Process.GetProcessesByName("osu!").First().Kill();
            Log.Information("Closing osu! in order to import beatmaps.");
        }

        int beatmapPerPageCount = 0;
        long? cursor = 0;

        BeatmapCollection beatmapCollection;
        Beatmaps collectionDbBeatmaps = new();

        CollectionInfo collectionInfo = await $"https://osucollector.com/api/collections/{CollectionId}".GetJsonAsync<CollectionInfo>();

        do
        {
            beatmapCollection = await $"https://osucollector.com/api/collections/{CollectionId}/beatmapsv2?cursor={cursor}".GetJsonAsync<BeatmapCollection>();

            cursor = beatmapCollection.NextPageCursor;
            TitlebarName = $"osu!CollectorGrabber - Downloading: {collectionInfo.Name}";

            foreach (Models.Beatmap beatmap in beatmapCollection.Beatmaps)
            {
                if (beatmapPerPageCount >= 50)
                {
                    beatmapPerPageCount = 0;
                    continue;
                }

                try
                {
                    string beatmapFileName = $"{beatmap.BeatmapsetId} {ReplaceInvalidPathCharacters(beatmap.Beatmapset.ArtistUnicode)} - {ReplaceInvalidPathCharacters(beatmap.Beatmapset.TitleUnicode)}.osz";
                    string beatmapInOsuDirectory = Path.GetFullPath(Path.GetFileNameWithoutExtension(beatmapFileName), Path.GetFullPath("Songs", OsuDirectory));

                    if (!Directory.Exists(beatmapInOsuDirectory))
                    {
                        string beatmapInDownloadedBeatmapsDirectory = await $"https://beatconnect.io/b/{beatmap.BeatmapsetId}".DownloadFileAsync(Paths.DownloadedBeatmapsDirectory, beatmapFileName);

                        ZipFile.ExtractToDirectory(beatmapInDownloadedBeatmapsDirectory, beatmapInOsuDirectory);
                        File.Delete(beatmapInDownloadedBeatmapsDirectory);

                        Log.Information($"Imported {beatmap.Id} - {beatmap.Beatmapset.ArtistUnicode} - {beatmap.Beatmapset.TitleUnicode}");
                    }
                    else
                    {
                        Log.Information($"Skipping {beatmap.Id} - {beatmap.Beatmapset.ArtistUnicode} - {beatmap.Beatmapset.TitleUnicode} because it's already imported.");
                    }
                }
                catch
                {
                    continue;
                }
                finally
                {
                    BeatmapExtension beatmapData = new()
                    {
                        MapSetId = beatmap.BeatmapsetId,
                        MapId = beatmap.Id,
                        CircleSize = (float)beatmap.Cs,
                        OverallDifficulty = (float)beatmap.Accuracy,
                        ApproachRate = (float)beatmap.Ar,
                        HpDrainRate = (float)beatmap.Drain,
                        Circles = (short)beatmap.CountCircles,
                        Sliders = (short)beatmap.CountSliders,
                        Spinners = (short)beatmap.CountSpinners,
                        MainBpm = beatmap.Bpm,
                        DiffName = beatmap.Version,
                        Md5 = beatmap.Checksum,
                        ArtistRoman = beatmap.Beatmapset.Artist,
                        TitleRoman = beatmap.Beatmapset.Title,
                        ArtistUnicode = beatmap.Beatmapset.ArtistUnicode,
                        TitleUnicode = beatmap.Beatmapset.TitleUnicode,
                        Creator = beatmap.Beatmapset.Creator,
                        Source = beatmap.Beatmapset.Source,
                        Tags = beatmap.Beatmapset.Tags,
                        EditDate = beatmap.LastUpdated,
                        PlayMode = (PlayMode)beatmap.ModeInt,
                        DataDownloaded = true,
                    };

                    beatmapData.ModPpStars.Add(beatmapData.PlayMode, new() { { 0, Math.Round(beatmap.DifficultyRating, 2) } });
                    collectionDbBeatmaps.Add(beatmapData);

                    beatmapPerPageCount++;
                }
            }

        } while (beatmapCollection.HasMore);

        CollectionsManager collectionsManager = new(osuFileIo.OsuDatabase.LoadedMaps.Beatmaps);
        collectionsManager.EditCollection(CollectionEditArgs.AddCollections(GenerateCollection(collectionDbBeatmaps, collectionInfo.Name)));
        osuFileIo.CollectionLoader.SaveOsdbCollection(collectionsManager.LoadedCollections, Path.GetFullPath($"{ReplaceInvalidPathCharacters(collectionInfo.Name)}.osdb", CollectionDbOutput));

        Log.Information($"Finished downloading {collectionDbBeatmaps.Count} beatmaps & generated an .osdb file in {CollectionDbOutput}");
        TitlebarName = "osu!CollectorGrabber";
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
            Description = "Select a folder to save your collection.db file.",
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

    private Collections GenerateCollection(Beatmaps maps, string collectionName)
    {
        Collections collections = new();
        Collection collection = new(osuFileIo.LoadedMaps) { Name = collectionName };

        foreach (CollectionManager.DataTypes.Beatmap map in maps)
        {
            collection.AddBeatmap(map);
        }

        collections.Add(collection);
        return collections;
    }

    private static string ReplaceInvalidPathCharacters(string source)
    {
        return new StringBuilder(source)
            .Replace(":", "_")
            .Replace("?", string.Empty)
            .Replace("/", string.Empty)
            .Replace("\"", string.Empty)
            .Replace("<", string.Empty)
            .Replace(">", string.Empty)
            .Replace("|", string.Empty)
            .Replace("*", string.Empty)
            .Replace(".", string.Empty)
            .ToString();
    }
}