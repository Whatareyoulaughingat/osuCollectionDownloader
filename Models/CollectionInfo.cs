using System.Text.Json.Serialization;

namespace osuCollectionDownloader.Models;

public record CollectionInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("beatmapCount")] int BeatmapCount
);

