#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{



    public class BeatmapScoreLazer // 只是比score多了个当前bid的排名
    {
        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("score")]
        public ScoreLazer Score { get; set; }
    }

    public class ScoreMod
    {
        [JsonProperty("acronym")]
        public string Acronym { get; set; }

        [JsonProperty("settings", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Settings { get; set; }

        [JsonIgnore]
        public bool IsClassic => Acronym == "CL";

        [JsonIgnore]
        public bool IsVisualMod => Acronym == "HD" || Acronym == "FL";

        [JsonIgnore]
        public bool IsSpeedChangeMod =>
            Acronym == "DT" || Acronym == "NC" || Acronym == "HT" || Acronym == "DC";

        public static ScoreMod FromString(string mod)
        {
            return new ScoreMod { Acronym = mod };
        }
    }

    public class ScoreLazer
    {
        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("beatmap_id")]
        public long BeatmapId { get; set; }

        [JsonProperty("best_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? BestId { get; set; }

        [JsonProperty("build_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? BuildId { get; set; }

        [JsonProperty("classic_total_score")]
        public long ClassicTotalScore { get; set; }

        [JsonProperty("ended_at")]
        public DateTimeOffset EndedAt { get; set; }

        [JsonProperty("has_replay")]
        public bool HasReplay { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("is_perfect_combo")]
        public bool IsPerfectCombo { get; set; }

        [JsonProperty("legacy_perfect")]
        public bool LegacyPerfect { get; set; }

        [JsonProperty("legacy_score_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? LegacyScoreId { get; set; }

        [JsonProperty("legacy_total_score")]
        public uint LegacyTotalScore { get; set; }

        [JsonProperty("max_combo")]
        public uint MaxCombo { get; set; }

        [JsonProperty("maximum_statistics")]
        public ScoreStatisticsLazer MaximumStatistics { get; set; }

        [JsonProperty("mods")]
        public ScoreMod[] Mods { get; set; }

        [JsonProperty("passed")]
        public bool Passed { get; set; }

        [JsonProperty("pp", NullValueHandling = NullValueHandling.Ignore)]
        public double? pp { get; set; }

        [JsonProperty("preserve")]
        public bool Preserve { get; set; }

        [JsonProperty("processed")]
        public bool Processed { get; set; }

        [JsonProperty("rank")]
        public string Rank { get; set; }

        [JsonProperty("ranked")]
        public bool Ranked { get; set; }

        [JsonProperty("ruleset_id")]
        public int ModeInt { get; set; }

        [JsonProperty("started_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StartedAt { get; set; }

        [JsonProperty("statistics")]
        public ScoreStatisticsLazer Statistics { get; set; }

        [JsonProperty("total_score")]
        public uint Score { get; set; }

        [JsonProperty("type")]
        public string Kind { get; set; }

        [JsonProperty("user_id")]
        public long UserId { get; set; }

        // 下面是可选内容

        // SoloScoreJsonAttributesMultiplayer

        [JsonProperty("playlist_item_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? PlaylistItemId { get; set; }

        [JsonProperty("room_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? RoomId { get; set; }

        [JsonProperty("solo_score_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? SoloScoreId { get; set; }

        // ScoreJsonAvailableIncludes

        [JsonProperty("beatmap", NullValueHandling = NullValueHandling.Ignore)]
        public Beatmap? Beatmap { get; set; }

        [JsonProperty("beatmapset", NullValueHandling = NullValueHandling.Ignore)]
        public Beatmapset? Beatmapset { get; set; }

        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public User? User { get; set; }

        [JsonProperty("weight", NullValueHandling = NullValueHandling.Ignore)]
        public ScoreWeight? Weight { get; set; }

        [JsonProperty("match", NullValueHandling = NullValueHandling.Ignore)]
        public Match? Match { get; set; }

        [JsonProperty("rank_country", NullValueHandling = NullValueHandling.Ignore)]
        public long? RankCountry { get; set; }

        [JsonProperty("rank_global", NullValueHandling = NullValueHandling.Ignore)]
        public long? RankGlobal { get; set; }

        // ScoreJsonDefaultIncludes

        [JsonProperty("current_user_attributes", NullValueHandling = NullValueHandling.Ignore)]
        public CurrentUserAttributes? CurrentUserAttributes { get; set; }

        // tool

        [JsonIgnore]
        public bool ConvertFromOld { get; init; } = false;

        [JsonIgnore]
        public Mode Mode => ModeInt.ToMode() ?? Mode.Unknown;

        [JsonIgnore]
        public bool IsClassic => !StartedAt.HasValue;

        [JsonIgnore]
        public uint ScoreAuto => IsClassic ? LegacyTotalScore : Score;

        [JsonIgnore]
        public string RankAuto => IsClassic ? LeagcyRank : Rank;

        [JsonIgnore]
        public double AccAuto => IsClassic ? LeagcyAcc : Accuracy;

        [JsonIgnore]
        public ScoreStatisticsLazer ConvertStatistics => GetStatistics();

        [JsonIgnore]
        public string LeagcyRank => GetRank();

        [JsonIgnore]
        public double LeagcyAcc => GetLeagcyAcc();

        // private

        [JsonIgnore]
        private double? _LeagcyAcc { get; set; } = null;

        [JsonIgnore]
        private string? _LeagcyRank { get; set; } = null;

        [JsonIgnore]
        private ScoreStatisticsLazer? _ConvertStatistics { get; set; } = null;

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
        [JsonProperty("pass")]
        public bool Pass { get; set; }

        [JsonProperty("slot")]
        public uint Slot { get; set; }

        [JsonProperty("team")]
        public uint Team { get; set; }
    }

    public class CurrentUserAttributes
    {
        [JsonProperty("pin", NullValueHandling = NullValueHandling.Ignore)]
        public CurrentUserPin? Pin { get; set; }
    }

    public class CurrentUserPin
    {
        [JsonProperty("is_pinned")]
        public bool IsPinned { get; set; }

        [JsonProperty("score_id")]
        public long ScoreId { get; set; }
    }


    public class ScoreStatisticsLazer
    {
        [JsonProperty("ok", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountOk { get; set; }

        [JsonProperty("great", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountGreat { get; set; }

        [JsonProperty("meh", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountMeh { get; set; }

        [JsonProperty("perfect", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountGeki { get; set; }

        [JsonProperty("good", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountKatu { get; set; }

        [JsonProperty("miss", NullValueHandling = NullValueHandling.Ignore)]
        public uint CountMiss { get; set; }

        [JsonProperty("large_tick_hit", NullValueHandling = NullValueHandling.Ignore)]
        public uint LargeTickHit { get; set; }

        [JsonProperty("large_tick_miss", NullValueHandling = NullValueHandling.Ignore)]
        public uint LargeTickMiss { get; set; }

        [JsonProperty("small_tick_hit", NullValueHandling = NullValueHandling.Ignore)]
        public uint SmallTickHit { get; set; }

        [JsonProperty("small_tick_miss", NullValueHandling = NullValueHandling.Ignore)]
        public uint SmallTickMiss { get; set; }

        [JsonProperty("ignore_hit", NullValueHandling = NullValueHandling.Ignore)]
        public uint IgnoreHit { get; set; }

        [JsonProperty("ignore_miss", NullValueHandling = NullValueHandling.Ignore)]
        public uint IgnoreMiss { get; set; }

        [JsonProperty("large_bonus", NullValueHandling = NullValueHandling.Ignore)]
        public uint LargeBonus { get; set; }

        [JsonProperty("small_bonus", NullValueHandling = NullValueHandling.Ignore)]
        public uint SmallBonus { get; set; }

        [JsonProperty("slider_tail_hit", NullValueHandling = NullValueHandling.Ignore)]
        public uint SliderTailHit { get; set; }

        [JsonProperty("combo_break", NullValueHandling = NullValueHandling.Ignore)]
        public uint ComboBreak { get; set; }

        [JsonProperty("legacy_combo_increase", NullValueHandling = NullValueHandling.Ignore)]
        public uint LegacyComboIncrease { get; set; }

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