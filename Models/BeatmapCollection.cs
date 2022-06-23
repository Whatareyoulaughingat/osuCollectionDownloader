using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace osuCollectionDownloader.Models;

public record Beatmap(
    [property: JsonPropertyName("ar")] float Ar,
    [property: JsonPropertyName("mode_int")] int ModeInt,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("beatmapset_id")] int BeatmapsetId,
    [property: JsonPropertyName("difficulty_rating")] float DifficultyRating,
    [property: JsonPropertyName("bpm")] float Bpm,
    [property: JsonPropertyName("checksum")] string Checksum,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("cs")] float Cs,
    [property: JsonPropertyName("drain")] float Drain,
    [property: JsonPropertyName("count_spinners")] short CountSpinners,
    [property: JsonPropertyName("count_sliders")] short CountSliders,
    [property: JsonPropertyName("beatmapset")] Beatmapset Beatmapset,
    [property: JsonPropertyName("count_circles")] short CountCircles,
    [property: JsonPropertyName("accuracy")] float Accuracy,
    [property: JsonPropertyName("last_updated")] DateTime LastUpdated
);

public record Beatmapset(
    [property: JsonPropertyName("creator")] string Creator,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("artist_unicode")] string ArtistUnicode,
    [property: JsonPropertyName("bpm")] double Bpm,
    [property: JsonPropertyName("artist")] string Artist,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("title_unicode")] string TitleUnicode,
    [property: JsonPropertyName("tags")] string Tags
);

public record BeatmapCollection(
    [property: JsonPropertyName("nextPageCursor")] int? NextPageCursor,
    [property: JsonPropertyName("hasMore")] bool HasMore,
    [property: JsonPropertyName("beatmaps")] IReadOnlyList<Beatmap> Beatmaps
);