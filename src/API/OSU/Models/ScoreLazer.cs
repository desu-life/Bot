#pragma warning disable CS8618 // 非null 字段未初始化
using System.Text.Json.Serialization;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class BeatmapScoreLazer // 只是比score多了个当前bid的排名
    {
        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("score")]
        public ScoreLazer Score { get; set; }
    }

    public class ScoreLazer
    {
        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }

        [JsonPropertyName("beatmap_id")]
        public long BeatmapId { get; set; }

        [JsonPropertyName("best_id")]
        public long? BestId { get; set; }

        [JsonPropertyName("build_id")]
        public long? BuildId { get; set; }

        [JsonPropertyName("classic_total_score")]
        public long ClassicTotalScore { get; set; }

        [JsonPropertyName("ended_at")]
        public DateTimeOffset EndedAt { get; set; }

        [JsonPropertyName("has_replay")]
        public bool HasReplay { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("is_perfect_combo")]
        public bool IsPerfectCombo { get; set; }

        [JsonPropertyName("legacy_perfect")]
        public bool LegacyPerfect { get; set; }

        [JsonPropertyName("legacy_score_id")]
        public long? LegacyScoreId { get; set; }

        [JsonPropertyName("legacy_total_score")]
        public uint LegacyTotalScore { get; set; }

        [JsonPropertyName("max_combo")]
        public uint MaxCombo { get; set; }

        [JsonPropertyName("maximum_statistics")]
        public ScoreStatisticsLazer? MaximumStatistics { get; set; }

        [JsonPropertyName("mods")]
        public Mod[] Mods { get; set; }

        [JsonPropertyName("passed")]
        public bool Passed { get; set; }

        [JsonPropertyName("pp")]
        public double? pp { get; set; }

        [JsonPropertyName("preserve")]
        public bool Preserve { get; set; }

        [JsonPropertyName("processed")]
        public bool Processed { get; set; }

        [JsonPropertyName("rank")]
        public string Rank { get; set; }

        [JsonPropertyName("ranked")]
        public bool Ranked { get; set; }

        [JsonPropertyName("ruleset_id")]
        public int ModeInt { get; set; }

        [JsonPropertyName("started_at")]
        public DateTimeOffset? StartedAt { get; set; }

        [JsonPropertyName("statistics")]
        public ScoreStatisticsLazer Statistics { get; set; }

        [JsonPropertyName("total_score")]
        public uint Score { get; set; }

        [JsonPropertyName("type")]
        public string Kind { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        // 下面是可选内容

        // SoloScoreJsonAttributesMultiplayer

        [JsonPropertyName("playlist_item_id")]
        public long? PlaylistItemId { get; set; }

        [JsonPropertyName("room_id")]
        public long? RoomId { get; set; }

        [JsonPropertyName("solo_score_id")]
        public long? SoloScoreId { get; set; }

        // ScoreJsonAvailableIncludes

        [JsonPropertyName("beatmap")]
        public Beatmap? Beatmap { get; set; }

        [JsonPropertyName("beatmapset")]
        public Beatmapset? Beatmapset { get; set; }

        [JsonPropertyName("user")]
        public User? User { get; set; }

        [JsonPropertyName("weight")]
        public ScoreWeight? Weight { get; set; }

        [JsonPropertyName("match")]
        public Match? Match { get; set; }

        [JsonPropertyName("rank_country")]
        public long? RankCountry { get; set; }

        [JsonPropertyName("rank_global")]
        public long? RankGlobal { get; set; }

        // ScoreJsonDefaultIncludes

        [JsonPropertyName("current_user_attributes")]
        public CurrentUserAttributes? CurrentUserAttributes { get; set; }

        // tool

        [JsonIgnore]
        public bool ConvertFromOld { get; init; } = false;

        [JsonIgnore]
        public Mode Mode => ModeInt.ToMode() ?? Mode.OSU;

        [JsonIgnore]
        public bool IsLazer => StartedAt.HasValue;

        [JsonIgnore]
        public bool IsClassic => !StartedAt.HasValue;

        [JsonIgnore]
        public uint ScoreAuto => IsClassic ? LegacyTotalScore : Score;

        [JsonIgnore]
        public string RankAuto => IsClassic ? LeagcyRank : Rank;

        [JsonIgnore]
        public double AccAuto => IsClassic ? LeagcyAcc : Accuracy;

        [JsonIgnore]
        public string JsonMods => GetJsonMods();

        [JsonIgnore]
        public ScoreStatisticsLazer ConvertStatistics => GetStatistics();

        [JsonIgnore]
        public string LeagcyRank => GetRank();

        [JsonIgnore]
        public double LeagcyAcc => GetLeagcyAcc();

        // private

        [JsonIgnore]
        public string? _JsonMods { get; set; } = null;

        [JsonIgnore]
        private double? _LeagcyAcc { get; set; } = null;

        [JsonIgnore]
        private string? _LeagcyRank { get; set; } = null;

        [JsonIgnore]
        private ScoreStatisticsLazer? _ConvertStatistics { get; set; } = null;

        private string GetJsonMods()
        {
            if (_JsonMods is not null)
            {
                return _JsonMods;
            }

            _JsonMods = Serializer.Json.Serialize(Mods);
            return _JsonMods;
        }

        private double GetLeagcyAcc()
        {
            if (_LeagcyAcc is not null)
            {
                return _LeagcyAcc.Value;
            }

            if (ConvertFromOld)
            {
                _LeagcyAcc = Accuracy;
                return Accuracy;
            }

            _LeagcyAcc = Statistics.Accuracy(Mode);
            return _LeagcyAcc.Value;
        }

        private ScoreStatisticsLazer GetStatistics()
        {
            if (_ConvertStatistics is not null)
            {
                return _ConvertStatistics;
            }

            if (ConvertFromOld)
            {
                _ConvertStatistics = Statistics;
                return Statistics;
            }

            if (Mode is Mode.Fruits)
            {
                _ConvertStatistics = new ScoreStatisticsLazer()
                {
                    CountGreat = Statistics.CountGreat,
                    CountOk = Statistics.LargeTickHit,
                    CountMeh = Statistics.SmallTickHit,
                    CountKatu = Statistics.SmallTickMiss,
                    CountGeki = Statistics.CountGeki,
                    CountMiss = Statistics.CountMiss + Statistics.LargeTickMiss,
                };
            }
            else
            {
                _ConvertStatistics = Statistics;
            }

            return _ConvertStatistics;
        }

        private string GetRank()
        {
            if (_LeagcyRank is not null)
            {
                return _LeagcyRank;
            }

            if (this.Rank == "F")
            {
                _LeagcyRank = "F";
                return _LeagcyRank;
            }

            switch (this.Mode)
            {
                case Mode.OSU:
                {
                    var totalHits = Statistics.TotalHits(this.Mode);
                    var greatRate = totalHits > 0 ? (double)Statistics.CountGreat / totalHits : 1.0;
                    var mehRate = totalHits > 0 ? (double)Statistics.CountMeh / totalHits : 1.0;

                    if (greatRate == 1.0)
                    {
                        _LeagcyRank = Mods.Any(it => it.IsVisualMod) ? "XH" : "X";
                    }
                    else if (greatRate > 0.9 && mehRate <= 0.01 && Statistics.CountMiss == 0)
                    {
                        _LeagcyRank = Mods.Any(it => it.IsVisualMod) ? "SH" : "S";
                    }
                    else if ((greatRate > 0.8 && Statistics.CountMiss == 0) || greatRate > 0.9)
                    {
                        _LeagcyRank = "A";
                    }
                    else if ((greatRate > 0.7 && Statistics.CountMiss == 0) || greatRate > 0.8)
                    {
                        _LeagcyRank = "B";
                    }
                    else if (greatRate > 0.6)
                    {
                        _LeagcyRank = "C";
                    }
                    else
                    {
                        _LeagcyRank = "D";
                    }
                    break;
                }
                case Mode.Taiko:
                {
                    var totalHits = Statistics.TotalHits(this.Mode);
                    var greatRate = totalHits > 0 ? (double)Statistics.CountGreat / totalHits : 1.0;
                    var acc = Statistics.Accuracy(this.Mode);

                    if (greatRate == 1.0)
                    {
                        _LeagcyRank = Mods.Any(it => it.IsVisualMod) ? "XH" : "X";
                    }
                    else if (greatRate > 0.9 && Statistics.CountMiss == 0)
                    {
                        _LeagcyRank = Mods.Any(it => it.IsVisualMod) ? "SH" : "S";
                    }
                    else if ((greatRate > 0.8 && Statistics.CountMiss == 0) || greatRate > 0.9)
                    {
                        _LeagcyRank = "A";
                    }
                    else if ((greatRate > 0.7 && Statistics.CountMiss == 0) || greatRate > 0.8)
                    {
                        _LeagcyRank = "B";
                    }
                    else if (greatRate > 0.6)
                    {
                        _LeagcyRank = "C";
                    }
                    else
                    {
                        _LeagcyRank = "D";
                    }
                    break;
                }
                case Mode.Fruits:
                {
                    var acc = Statistics.Accuracy(this.Mode);

                    if (acc == 1.0)
                    {
                        _LeagcyRank = Mods.Any(it => it.IsVisualMod) ? "XH" : "X";
                    }
                    else if (acc > 0.98)
                    {
                        _LeagcyRank = Mods.Any(it => it.IsVisualMod) ? "SH" : "S";
                    }
                    else if (acc > 0.94)
                    {
                        _LeagcyRank = "A";
                    }
                    else if (acc > 0.9)
                    {
                        _LeagcyRank = "B";
                    }
                    else if (acc > 0.85)
                    {
                        _LeagcyRank = "C";
                    }
                    else
                    {
                        _LeagcyRank = "D";
                    }
                    break;
                }
                case Mode.Mania:
                {
                    var acc = Statistics.Accuracy(this.Mode);

                    if (acc == 1.0)
                    {
                        _LeagcyRank = Mods.Any(it => it.IsVisualMod) ? "XH" : "X";
                    }
                    else if (acc > 0.95)
                    {
                        _LeagcyRank = Mods.Any(it => it.IsVisualMod) ? "SH" : "S";
                    }
                    else if (acc > 0.9)
                    {
                        _LeagcyRank = "A";
                    }
                    else if (acc > 0.8)
                    {
                        _LeagcyRank = "B";
                    }
                    else if (acc > 0.7)
                    {
                        _LeagcyRank = "C";
                    }
                    else
                    {
                        _LeagcyRank = "D";
                    }
                    break;
                }
                default:
                {
                    _LeagcyRank = Rank;
                    break;
                }
            }

            return _LeagcyRank;
        }
    }

    public class Match
    {
        [JsonPropertyName("pass")]
        public bool Pass { get; set; }

        [JsonPropertyName("slot")]
        public uint Slot { get; set; }

        [JsonPropertyName("team")]
        public uint Team { get; set; }
    }

    public class CurrentUserAttributes
    {
        [JsonPropertyName("pin")]
        public CurrentUserPin? Pin { get; set; }
    }

    public class CurrentUserPin
    {
        [JsonPropertyName("is_pinned")]
        public bool IsPinned { get; set; }

        [JsonPropertyName("score_id")]
        public long ScoreId { get; set; }
    }


    public class ScoreStatisticsLazer
    {
        [JsonPropertyName("ok")]
        public uint CountOk { get; set; }

        [JsonPropertyName("great")]
        public uint CountGreat { get; set; }

        [JsonPropertyName("meh")]
        public uint CountMeh { get; set; }

        [JsonPropertyName("perfect")]
        public uint CountGeki { get; set; }

        [JsonPropertyName("good")]
        public uint CountKatu { get; set; }

        [JsonPropertyName("miss")]
        public uint CountMiss { get; set; }

        [JsonPropertyName("large_tick_hit")]
        public uint LargeTickHit { get; set; }

        [JsonPropertyName("large_tick_miss")]
        public uint LargeTickMiss { get; set; }

        [JsonPropertyName("small_tick_hit")]
        public uint SmallTickHit { get; set; }

        [JsonPropertyName("small_tick_miss")]
        public uint SmallTickMiss { get; set; }

        [JsonPropertyName("ignore_hit")]
        public uint IgnoreHit { get; set; }

        [JsonPropertyName("ignore_miss")]
        public uint IgnoreMiss { get; set; }

        [JsonPropertyName("large_bonus")]
        public uint LargeBonus { get; set; }

        [JsonPropertyName("small_bonus")]
        public uint SmallBonus { get; set; }

        [JsonPropertyName("slider_tail_hit")]
        public uint SliderTailHit { get; set; }

        [JsonPropertyName("combo_break")]
        public uint ComboBreak { get; set; }

        [JsonPropertyName("legacy_combo_increase")]
        public uint LegacyComboIncrease { get; set; }

        public uint PassedObjects(Mode mode)
        {
            return mode switch
            {
                Mode.OSU => CountGreat + CountOk + CountMeh + CountMiss,
                Mode.Taiko => CountGreat + CountOk + CountMiss,
                Mode.Fruits
                    => CountGreat + CountOk + CountMiss,
                Mode.Mania
                    => CountGeki + CountKatu + CountGreat + CountOk + CountMeh + CountMiss,
                _ => 0
            };
        }

        public uint TotalHits(Mode mode)
        {
            return mode switch
            {
                Mode.OSU => CountGreat + CountOk + CountMeh + CountMiss,
                Mode.Taiko => CountGreat + CountOk + CountMiss,
                Mode.Fruits
                    => SmallTickHit
                        + LargeTickHit
                        + CountGreat
                        + CountMiss
                        + SmallTickMiss
                        + LargeTickMiss,
                Mode.Mania
                    => CountGeki + CountKatu + CountGreat + CountOk + CountMeh + CountMiss,
                _ => 0
            };
        }

        public double Accuracy(Mode mode)
        {
            var todalHits = TotalHits(mode);

            if (todalHits == 0)
            {
                return 0.0;
            }

            return mode switch
            {
                Mode.OSU
                    => (double)((6 * CountGreat) + (2 * CountOk) + CountMeh)
                        / (double)(6 * todalHits),
                Mode.Taiko => (double)((2 * CountGreat) + CountOk) / (double)(2 * todalHits),
                Mode.Fruits
                    => (double)(SmallTickHit + LargeTickHit + CountGreat) / (double)todalHits,
                Mode.Mania
                    => (double)(
                        6 * (CountGeki + CountGreat) + 4 * CountKatu + 2 * CountOk + CountMeh
                    ) / (double)(6 * todalHits),
                _ => 0
            };
        }
    }


}