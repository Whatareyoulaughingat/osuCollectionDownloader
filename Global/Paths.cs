using System;
using System.IO;

namespace osuCollectionDownloader.Global;

public static class Paths
{
    public static string BaseDirectory => Path.GetFullPath("osu! Collection Downloader", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    public static string DownloadedBeatmapsDirectory => Path.GetFullPath("Downloaded Beatmaps", BaseDirectory);

    public static string LogsDirectory => Path.GetFullPath("Logs", BaseDirectory);
}
