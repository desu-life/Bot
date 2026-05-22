#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{

    public class BeatmapScore // 只是比score多了个当前bid的排名
    {
        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("score")]
        public Score Score { get; set; }
    }


    public class Score
    {
        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }

        [JsonPropertyName("best_id")]
        public long BestId { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("max_combo")]
        public uint MaxCombo { get; set; }

        [JsonPropertyName("mode")]
        public Mode Mode { get; set; }

        [JsonPropertyName("mode_int")]
        public int ModeInt { get; set; }

        [JsonPropertyName("mods")]
        public string[] Mods { get; set; }

        [JsonPropertyName("passed")]
        public bool Passed { get; set; }

        [JsonPropertyName("perfect")]
        public bool Perfect { get; set; }

        [JsonPropertyName("pp")]
        public double PP { get; set; }

        [JsonPropertyName("rank")]
        public string Rank { get; set; }

        [JsonPropertyName("replay")]
        public bool Replay { get; set; }

        [JsonPropertyName("score")]
        public uint Scores { get; set; }

        [JsonPropertyName("statistics")]
        public ScoreStatistics Statistics { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("beatmap")]
        public Beatmap? Beatmap { get; set; }

        [JsonPropertyName("beatmapset")]
        public Beatmapset? Beatmapset { get; set; }

        [JsonPropertyName("user")]
        public User? User { get; set; }

        [JsonPropertyName("weight")]
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
        [JsonPropertyName("count_100")]
        public uint CountOk { get; set; }

        [JsonPropertyName("count_300")]
        public uint CountGreat { get; set; }

        [JsonPropertyName("count_50")]
        public uint CountMeh { get; set; }

        [JsonPropertyName("count_geki")]
        public uint CountGeki { get; set; }

        [JsonPropertyName("count_katu")]
        public uint CountKatu { get; set; }

        [JsonPropertyName("count_miss")]
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
        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("pp")]
        public double PP { get; set; }
    }
}