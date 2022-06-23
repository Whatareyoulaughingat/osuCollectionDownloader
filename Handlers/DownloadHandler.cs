using CollectionManager.DataTypes;
using CollectionManager.Enums;
using CollectionManager.Modules.CollectionsManager;
using CollectionManager.Modules.FileIO;
using Flurl.Http;
using osuCollectionDownloader.Extensions;
using osuCollectionDownloader.Global;
using osuCollectionDownloader.Models;
using osuCollectionDownloader.ViewModels;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace osuCollectionDownloader.Handlers;

public class DownloadHandler
{
    private BeatmapCollectionInfo currentCollectionInfo;

    private BeatmapCollection currentBeatmapCollection;

    private readonly OsuFileIo osuFileIo = new(new BeatmapExtension());

    public async Task DownloadBeatmapsAsync()
    {
        if (Process.GetProcessesByName("osu!").Any())
        {
            Process.GetProcessesByName("osu!").First().Kill();
            Log.Information("Closed osu! in order to import beatmaps.");
        }

        int totalBeatmapsDownloaded = 0;
        long? nextPageCursor = 0;

        currentCollectionInfo = await $"https://osucollector.com/api/collections/{MainWindowViewModel.Instance.CollectionId}".GetJsonAsync<BeatmapCollectionInfo>();

        MainWindowViewModel.Instance.TitlebarName = $"osu! Collection Downloader - Importing: {currentCollectionInfo.Name}";

        do
        {
            currentBeatmapCollection = await $"https://osucollector.com/api/collections/{MainWindowViewModel.Instance.CollectionId}/beatmapsv2?cursor={nextPageCursor}".GetJsonAsync<BeatmapCollection>();
            nextPageCursor = currentBeatmapCollection.NextPageCursor;

            foreach (Models.Beatmap beatmap in currentBeatmapCollection.Beatmaps)
            {
                string beatmapFileName = $"{beatmap.BeatmapsetId} {beatmap.Beatmapset.ArtistUnicode.ReplaceInvalidPathChars()} - {beatmap.Beatmapset.TitleUnicode.ReplaceInvalidPathChars()}.osz";
                string beatmapInOsuDirectory = Path.GetFullPath(Path.GetFileNameWithoutExtension(beatmapFileName), Path.GetFullPath("Songs", MainWindowViewModel.Instance.OsuDirectory));
                string beatmapInDownloadedBeatmapsDirectory;

                if (Directory.Exists(beatmapInOsuDirectory))
                {
                    Log.Information($"Skipped {beatmap.Id} - {beatmap.Beatmapset.ArtistUnicode} - {beatmap.Beatmapset.TitleUnicode} because it's already imported.");
                    continue;
                }

                try
                {
                    beatmapInDownloadedBeatmapsDirectory = await $"https://beatconnect.io/b/{beatmap.BeatmapsetId}".DownloadFileAsync(Paths.DownloadedBeatmapsDirectory, beatmapFileName);
                }
                catch (FlurlHttpException flurlHttpEx) when (flurlHttpEx.StatusCode == 404)
                {
                    beatmapInDownloadedBeatmapsDirectory = await $"https://kitsu.moe/api/d/{beatmap.BeatmapsetId}".DownloadFileAsync(Paths.DownloadedBeatmapsDirectory, beatmapFileName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    break;
                }

                ZipFile.ExtractToDirectory(beatmapInDownloadedBeatmapsDirectory, beatmapInOsuDirectory);
                File.Delete(beatmapInDownloadedBeatmapsDirectory);

                totalBeatmapsDownloaded++;
                Log.Information($"Imported {beatmap.Id} - {beatmap.Beatmapset.ArtistUnicode} - {beatmap.Beatmapset.TitleUnicode}");
            }

        } while (currentBeatmapCollection.HasMore);

        Log.Information($"Finished downloading {totalBeatmapsDownloaded} beatmaps.");
        MainWindowViewModel.Instance.TitlebarName = "osu! Collection Downloader";
    }

    public void GenerateCollection()
    {
        Collections GenerateCollectionDb(Beatmaps maps, string collectionName)
        {
            Collections osuCollections = new();
            Collection currentCollection = new(osuFileIo.LoadedMaps) { Name = collectionName };

            foreach (CollectionManager.DataTypes.Beatmap map in maps)
            {
                currentCollection.AddBeatmap(map);
            }

            osuCollections.Add(currentCollection);
            return osuCollections;
        }

        Beatmaps collectionDbBeatmaps = new();

        foreach (Models.Beatmap beatmap in currentBeatmapCollection.Beatmaps)
        {
            BeatmapExtension beatmapData = new()
            {
                MapSetId = beatmap.BeatmapsetId,
                MapId = beatmap.Id,
                CircleSize = beatmap.Cs,
                OverallDifficulty = beatmap.Accuracy,
                ApproachRate = beatmap.Ar,
                HpDrainRate = beatmap.Drain,
                Circles = beatmap.CountCircles,
                Sliders = beatmap.CountSliders,
                Spinners = beatmap.CountSpinners,
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
        }

        CollectionsManager osuCollectionsManager = new(osuFileIo.OsuDatabase.LoadedMaps.Beatmaps);
        osuCollectionsManager.EditCollection(CollectionEditArgs.AddCollections(GenerateCollectionDb(collectionDbBeatmaps, currentCollectionInfo.Name)));

        osuFileIo.CollectionLoader.SaveOsdbCollection(osuCollectionsManager.LoadedCollections, Path.GetFullPath($"{currentCollectionInfo.Name.ReplaceInvalidPathChars()}.osdb", MainWindowViewModel.Instance.CollectionDbOutput));

        Log.Information($"Generated an .osdb file in {MainWindowViewModel.Instance.CollectionDbOutput}");
        MainWindowViewModel.Instance.TitlebarName = "osu! Collection Downloader";
    }
}