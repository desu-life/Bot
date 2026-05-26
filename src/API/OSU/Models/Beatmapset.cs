#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class Beatmapset
    {
        [JsonPropertyName("artist")]
        public string Artist { get; set; }

        [JsonPropertyName("artist_unicode")]
        public string ArtistUnicode { get; set; }

        [JsonPropertyName("covers")]
        public BeatmapCovers Covers { get; set; }

        [JsonPropertyName("creator")]
        public string Creator { get; set; }

        [JsonPropertyName("favourite_count")]
        public long FavouriteCount { get; set; }

        [JsonPropertyName("hype")]
        public BeatmapHype? Hype { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("nsfw")]
        public bool IsNsfw { get; set; }

        [JsonPropertyName("offset")]
        public long Offset { get; set; }

        [JsonPropertyName("play_count")]
        public long PlayCount { get; set; }

        [JsonPropertyName("preview_url")]
        public string PreviewUrl { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("spotlight")]
        public bool Spotlight { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("title_unicode")]
        public string TitleUnicode { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("video")]
        public bool Video { get; set; }

        [JsonPropertyName("availability")]
        public BeatmapAvailability Availability { get; set; }

        [JsonPropertyName("bpm")]
        public long BPM { get; set; }

        [JsonPropertyName("can_be_hyped")]
        public bool CanBeHyped { get; set; }

        [JsonPropertyName("discussion_enabled")]
        public bool DiscussionEnabled { get; set; }

        [JsonPropertyName("discussion_locked")]
        public bool DiscussionLocked { get; set; }

        [JsonPropertyName("is_scoreable")]
        public bool IsScoreable { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonPropertyName("legacy_thread_url")]
        public Uri? LegacyThreadUrl { get; set; }

        [JsonPropertyName("nominations_summary")]
        public NominationsSummary NominationsSummary { get; set; }

        [JsonPropertyName("ranked")]
        public long Ranked { get; set; }

        [JsonPropertyName("ranked_date")]
        public DateTimeOffset? RankedDate { get; set; }

        [JsonPropertyName("storyboard")]
        public bool Storyboard { get; set; }

        [JsonPropertyName("submitted_date")]
        public DateTimeOffset? SubmittedDate { get; set; }

        [JsonPropertyName("tags")]
        public string Tags { get; set; }

        [JsonPropertyName("ratings")]
        public long[] Ratings { get; set; }

        [JsonPropertyName("beatmaps")]
        public Beatmap[]? Beatmaps { get; set; }

        // [JsonPropertyName("track_id")]
        // public JsonObject TrackId { get; set; }
    }

    public class BeatmapAvailability
    {
        [JsonPropertyName("download_disabled")]
        public bool DownloadDisabled { get; set; }

        [JsonPropertyName("more_information")]
        public string? MoreInformation { get; set; }
    }

    
    public class BeatmapCovers
    {
        [JsonPropertyName("cover")]
        public string Cover { get; set; }

        [JsonPropertyName("cover@2x")]
        public string Cover2x { get; set; }

        [JsonPropertyName("card")]
        public string Card { get; set; }

        [JsonPropertyName("card@2x")]
        public string Card2x { get; set; }

        [JsonPropertyName("list")]
        public string List { get; set; }

        [JsonPropertyName("list@2x")]
        public string List2x { get; set; }

        [JsonPropertyName("slimcover")]
        public string SlimCover { get; set; }

        [JsonPropertyName("slimcover@2x")]
        public string SlimCover2x { get; set; }
    }


    public class BeatmapHype
    {
        [JsonPropertyName("current")]
        public int DownloadDisabled { get; set; }

        [JsonPropertyName("required")]
        public int MoreInformation { get; set; }
    }

    public class NominationsSummary
    {
        [JsonPropertyName("current")]
        public int Current { get; set; }

        [JsonPropertyName("required")]
        public int NominationsSummaryRequired { get; set; }
    }

}