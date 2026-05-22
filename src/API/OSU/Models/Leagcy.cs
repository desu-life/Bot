#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.OsuPerformance;
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class ScoreV1
    {
        [JsonPropertyName("beatmap_id")]
        public long BeatmapId { get; set; }

        [JsonPropertyName("score_id")]
        public long ScoreId { get; set; }

        [JsonPropertyName("score")]
        public uint Score { get; set; }

        [JsonPropertyName("maxcombo")]
        public uint MaxCombo { get; set; }

        [JsonPropertyName("count50")]
        public uint Count50 { get; set; }

        [JsonPropertyName("count100")]
        public uint Count100 { get; set; }

        [JsonPropertyName("count300")]
        public uint Count300 { get; set; }

        [JsonPropertyName("countmiss")]
        public uint CountMiss { get; set; }

        [JsonPropertyName("countkatu")]
        public uint CountKatu { get; set; }

        [JsonPropertyName("countgeki")]
        public uint CountGeki { get; set; }

        [JsonPropertyName("perfect")]
        public short Perfect { get; set; }

        [JsonPropertyName("enabled_mods")]
        public uint EnabledMods { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }

        [JsonPropertyName("rank")]
        public string Rank { get; set; }

        [JsonPropertyName("pp")]
        public double PP { get; set; }

        [JsonPropertyName("replay_available")]
        public long ReplayAvailable { get; set; }

        public ScoreLazer ToLazerScore(Mode mode)
        {
            var s = this;
            using var rmods = RosuPP.Mods.FromBits(s.EnabledMods, mode.ToRosu());
            using var js = RosuPP.OwnedString.Empty();
            rmods.Json(js);
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
        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("join_date")]
        public DateTimeOffset JoinDate { get; set; }

        [JsonPropertyName("count300")]
        public long Count300 { get; set; }

        [JsonPropertyName("count100")]
        public long Count100 { get; set; }

        [JsonPropertyName("count50")]
        public long Count50 { get; set; }

        [JsonPropertyName("playcount")]
        public long Playcount { get; set; }

        [JsonPropertyName("ranked_score")]
        public string RankedScore { get; set; }

        [JsonPropertyName("total_score")]
        public string TotalScore { get; set; }

        [JsonPropertyName("pp_rank")]
        public long PpRank { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; }

        [JsonPropertyName("pp_raw")]
        public string PpRaw { get; set; }

        [JsonPropertyName("accuracy")]
        public string Accuracy { get; set; }

        [JsonPropertyName("count_rank_ss")]
        public long CountRankSs { get; set; }

        [JsonPropertyName("count_rank_ssh")]
        public long CountRankSsh { get; set; }

        [JsonPropertyName("count_rank_s")]
        public long CountRankS { get; set; }

        [JsonPropertyName("count_rank_sh")]
        public long CountRankSh { get; set; }

        [JsonPropertyName("count_rank_a")]
        public long CountRankA { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("total_seconds_played")]
        public long TotalSecondsPlayed { get; set; }

        [JsonPropertyName("pp_country_rank")]
        public long PpCountryRank { get; set; }

        [JsonPropertyName("events")]
        public object[] Events { get; set; }

    }
}