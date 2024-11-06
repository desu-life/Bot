#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.OsuPerformance;
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class ScoreV1
    {
        [JsonProperty("beatmap_id")]
        public long BeatmapId { get; set; }

        [JsonProperty("score_id")]
        public long ScoreId { get; set; }

        [JsonProperty("score")]
        public uint Score { get; set; }

        [JsonProperty("maxcombo")]
        public uint MaxCombo { get; set; }

        [JsonProperty("count50")]
        public uint Count50 { get; set; }

        [JsonProperty("count100")]
        public uint Count100 { get; set; }

        [JsonProperty("count300")]
        public uint Count300 { get; set; }

        [JsonProperty("countmiss")]
        public uint CountMiss { get; set; }

        [JsonProperty("countkatu")]
        public uint CountKatu { get; set; }

        [JsonProperty("countgeki")]
        public uint CountGeki { get; set; }

        [JsonProperty("perfect")]
        public short Perfect { get; set; }

        [JsonProperty("enabled_mods")]
        public uint EnabledMods { get; set; }

        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }

        [JsonProperty("rank")]
        public string Rank { get; set; }

        [JsonProperty("pp")]
        public double PP { get; set; }

        [JsonProperty("replay_available")]
        public long ReplayAvailable { get; set; }

        public ScoreLazer ToLazerScore(Mode mode)
        {
            var s = this;
            var rmods = RosuPP.Mods.FromBits(s.EnabledMods, mode.ToRosu());
            var js = RosuPP.OwnedString.Empty();
            rmods.Json(ref js);
            var mods = Json.Deserialize<List<Models.Mod>>(js.ToCstr());
            mods?.Add(Mod.FromString("CL"));

            ScoreStatisticsLazer statistics = new ScoreStatistics { 
                CountGreat = s.Count300,
                CountKatu = s.CountKatu,
                CountMeh = s.Count50,
                CountOk = s.Count100,
                CountGeki = s.CountGeki,
                CountMiss = s.CountMiss,
            };
            return new ScoreLazer
            {
                Accuracy = statistics.Accuracy(mode),
                EndedAt = s.Date,
                Id = s.ScoreId,
                MaxCombo = s.MaxCombo,
                ModeInt = mode.ToNum(),
                Mods = mods?.ToArray() ?? [],
                Passed = s.Rank != "F",
                pp = s.PP,
                Rank = s.Rank,
                HasReplay = s.ReplayAvailable == 1,
                Score = 0,
                LegacyTotalScore = s.Score,
                Statistics = statistics,
                UserId = s.UserId,
                BeatmapId = s.BeatmapId,
                LegacyScoreId = s.ScoreId,
                Beatmap = null,
                Beatmapset = null,
                User = null,
                Weight = null,
                ConvertFromOld = true
            };
        }
    }

    public class UserV1
    {
        [JsonProperty("user_id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long UserId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("join_date")]
        public DateTimeOffset JoinDate { get; set; }

        [JsonProperty("count300")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Count300 { get; set; }

        [JsonProperty("count100")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Count100 { get; set; }

        [JsonProperty("count50")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Count50 { get; set; }

        [JsonProperty("playcount")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Playcount { get; set; }

        [JsonProperty("ranked_score")]
        public string RankedScore { get; set; }

        [JsonProperty("total_score")]
        public string TotalScore { get; set; }

        [JsonProperty("pp_rank")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PpRank { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("pp_raw")]
        public string PpRaw { get; set; }

        [JsonProperty("accuracy")]
        public string Accuracy { get; set; }

        [JsonProperty("count_rank_ss")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankSs { get; set; }

        [JsonProperty("count_rank_ssh")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankSsh { get; set; }

        [JsonProperty("count_rank_s")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankS { get; set; }

        [JsonProperty("count_rank_sh")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankSh { get; set; }

        [JsonProperty("count_rank_a")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankA { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("total_seconds_played")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TotalSecondsPlayed { get; set; }

        [JsonProperty("pp_country_rank")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PpCountryRank { get; set; }

        [JsonProperty("events")]
        public object[] Events { get; set; }

    }
    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object? ReadJson(
            JsonReader reader,
            Type t,
            object? existingValue,
            JsonSerializer serializer
        )
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            var value = serializer.Deserialize<string>(reader);
            if (Int64.TryParse(value, out long l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(
            JsonWriter writer,
            object? untypedValue,
            JsonSerializer serializer
        )
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new();
    }

}