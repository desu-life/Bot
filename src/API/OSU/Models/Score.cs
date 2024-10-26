#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{

    public class BeatmapScore // 只是比score多了个当前bid的排名
    {
        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("score")]
        public Score Score { get; set; }
    }


    public class Score
    {
        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("best_id", NullValueHandling = NullValueHandling.Ignore)]
        public long BestId { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("max_combo")]
        public uint MaxCombo { get; set; }

        [JsonProperty("mode")]
        [JsonConverter(typeof(JsonEnumConverter))]
        public Mode Mode { get; set; }

        [JsonProperty("mode_int")]
        public int ModeInt { get; set; }

        [JsonProperty("mods")]
        public string[] Mods { get; set; }

        [JsonProperty("passed")]
        public bool Passed { get; set; }

        [JsonProperty("perfect")]
        public bool Perfect { get; set; }

        [JsonProperty("pp", NullValueHandling = NullValueHandling.Ignore)]
        public double PP { get; set; }

        [JsonProperty("rank")]
        public string Rank { get; set; }

        [JsonProperty("replay")]
        public bool Replay { get; set; }

        [JsonProperty("score")]
        public uint Scores { get; set; }

        [JsonProperty("statistics")]
        public ScoreStatistics Statistics { get; set; }

        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("beatmap", NullValueHandling = NullValueHandling.Ignore)]
        public Beatmap? Beatmap { get; set; }

        [JsonProperty("beatmapset", NullValueHandling = NullValueHandling.Ignore)]
        public Beatmapset? Beatmapset { get; set; }

        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public User? User { get; set; }

        [JsonProperty("weight", NullValueHandling = NullValueHandling.Ignore)]
        public ScoreWeight? Weight { get; set; }

        public static implicit operator ScoreLazer(Score s)
        {
            var mods = s.Mods.Map(Mod.FromString).ToList();
            mods.Add(Mod.FromString("CL"));
            return new ScoreLazer
            {
                Accuracy = s.Accuracy,
                BestId = s.BestId,
                EndedAt = s.CreatedAt,
                Id = s.Id,
                MaxCombo = s.MaxCombo,
                ModeInt = s.Mode.ToNum(),
                Mods = mods.ToArray(),
                Passed = s.Passed,
                pp = s.PP,
                Rank = s.Rank,
                HasReplay = s.Replay,
                Score = 0,
                LegacyTotalScore = s.Scores,
                Statistics = s.Statistics,
                UserId = s.UserId,
                Beatmap = s.Beatmap,
                Beatmapset = s.Beatmapset,
                User = s.User,
                Weight = s.Weight,
                ConvertFromOld = true
            };
        }
    }

    public class ScoreStatistics
    {
        [JsonProperty("count_100", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountOk { get; set; }

        [JsonProperty("count_300", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountGreat { get; set; }

        [JsonProperty("count_50", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountMeh { get; set; }

        [JsonProperty("count_geki", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountGeki { get; set; }

        [JsonProperty("count_katu", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountKatu { get; set; }

        [JsonProperty("count_miss", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountMiss { get; set; }

        public static implicit operator ScoreStatisticsLazer(ScoreStatistics s)
        {
            return new ScoreStatisticsLazer
            {
                CountOk = s.CountOk,
                CountGreat = s.CountGreat,
                CountMeh = s.CountMeh,
                CountGeki = s.CountGeki,
                CountKatu = s.CountKatu,
                CountMiss = s.CountMiss
            };
        }
    }

    
    public class ScoreWeight
    {
        [JsonProperty("percentage")]
        public double Percentage { get; set; }

        [JsonProperty("pp", NullValueHandling = NullValueHandling.Ignore)]
        public double PP { get; set; }
    }
}