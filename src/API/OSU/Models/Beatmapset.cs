#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class Beatmapset
    {
        [JsonProperty(PropertyName = "artist")]
        public string Artist { get; set; }

        [JsonProperty(PropertyName = "artist_unicode")]
        public string ArtistUnicode { get; set; }

        [JsonProperty(PropertyName = "covers")]
        public BeatmapCovers Covers { get; set; }

        [JsonProperty(PropertyName = "creator")]
        public string Creator { get; set; }

        [JsonProperty(PropertyName = "favourite_count")]
        public long FavouriteCount { get; set; }

        [JsonProperty(PropertyName = "hype", NullValueHandling = NullValueHandling.Ignore)]
        public BeatmapHype? Hype { get; set; }

        [JsonProperty(PropertyName = "id")]
        public long Id { get; set; }

        [JsonProperty(PropertyName = "nsfw")]
        public bool IsNsfw { get; set; }

        [JsonProperty(PropertyName = "offset")]
        public long Offset { get; set; }

        [JsonProperty(PropertyName = "play_count")]
        public long PlayCount { get; set; }

        [JsonProperty(PropertyName = "preview_url")]
        public string PreviewUrl { get; set; }

        [JsonProperty(PropertyName = "source")]
        public string Source { get; set; }

        [JsonProperty(PropertyName = "spotlight")]
        public bool Spotlight { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "title_unicode")]
        public string TitleUnicode { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public long UserId { get; set; }

        [JsonProperty(PropertyName = "video")]
        public bool Video { get; set; }

        [JsonProperty(PropertyName = "availability")]
        public BeatmapAvailability Availability { get; set; }

        [JsonProperty(PropertyName = "bpm")]
        public long BPM { get; set; }

        [JsonProperty(PropertyName = "can_be_hyped")]
        public bool CanBeHyped { get; set; }

        [JsonProperty(PropertyName = "discussion_enabled")]
        public bool DiscussionEnabled { get; set; }

        [JsonProperty(PropertyName = "discussion_locked")]
        public bool DiscussionLocked { get; set; }

        [JsonProperty(PropertyName = "is_scoreable")]
        public bool IsScoreable { get; set; }

        [JsonProperty(PropertyName = "last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonProperty(
            PropertyName = "legacy_thread_url",
            NullValueHandling = NullValueHandling.Ignore
        )]
        public Uri? LegacyThreadUrl { get; set; }

        [JsonProperty(PropertyName = "nominations_summary")]
        public NominationsSummary NominationsSummary { get; set; }

        [JsonProperty(PropertyName = "ranked")]
        public long Ranked { get; set; }

        [JsonProperty(PropertyName = "ranked_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? RankedDate { get; set; }

        [JsonProperty(PropertyName = "storyboard")]
        public bool Storyboard { get; set; }

        [JsonProperty(
            PropertyName = "submitted_date",
            NullValueHandling = NullValueHandling.Ignore
        )]
        public DateTimeOffset? SubmittedDate { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public string Tags { get; set; }

        [JsonProperty(PropertyName = "ratings")]
        public long[] Ratings { get; set; }

        [JsonProperty(PropertyName = "beatmaps", NullValueHandling = NullValueHandling.Ignore)]
        public Beatmap[]? Beatmaps { get; set; }

        // [JsonProperty(PropertyName = "track_id")]
        // public JObject TrackId { get; set; }
    }

    public class BeatmapAvailability
    {
        [JsonProperty(PropertyName = "download_disabled")]
        public bool DownloadDisabled { get; set; }

        [JsonProperty(
            PropertyName = "more_information",
            NullValueHandling = NullValueHandling.Ignore
        )]
        public string? MoreInformation { get; set; }
    }

    
    public class BeatmapCovers
    {
        [JsonProperty(PropertyName = "cover")]
        public string Cover { get; set; }

        [JsonProperty(PropertyName = "cover@2x")]
        public string Cover2x { get; set; }

        [JsonProperty(PropertyName = "card")]
        public string Card { get; set; }

        [JsonProperty(PropertyName = "card@2x")]
        public string Card2x { get; set; }

        [JsonProperty(PropertyName = "list")]
        public string List { get; set; }

        [JsonProperty(PropertyName = "list@2x")]
        public string List2x { get; set; }

        [JsonProperty(PropertyName = "slimcover")]
        public string SlimCover { get; set; }

        [JsonProperty(PropertyName = "slimcover@2x")]
        public string SlimCover2x { get; set; }
    }


    public class BeatmapHype
    {
        [JsonProperty(PropertyName = "current")]
        public int DownloadDisabled { get; set; }

        [JsonProperty(PropertyName = "required")]
        public int MoreInformation { get; set; }
    }

    public class NominationsSummary
    {
        [JsonProperty(PropertyName = "current")]
        public int Current { get; set; }

        [JsonProperty(PropertyName = "required")]
        public int NominationsSummaryRequired { get; set; }
    }

}