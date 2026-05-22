#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{

    public class BeatmapAttributes
    {
        // 不包含在json解析中，用作分辨mode
        public Mode Mode { get; set; }

        [JsonPropertyName("max_combo")]
        // 共有部分
        public int MaxCombo { get; set; }

        [JsonPropertyName("star_rating")]
        public double StarRating { get; set; }

        // osu, taiko, fruits包含
        [JsonPropertyName("approach_rate")]
        public double ApproachRate { get; set; }

        // taiko, mania包含
        [JsonPropertyName("great_hit_window")]
        public double GreatHitWindow { get; set; }

        // osu部分
        [JsonPropertyName("aim_difficulty")]
        public double AimDifficulty { get; set; }

        [JsonPropertyName("flashlight_difficulty")]
        public double FlashlightDifficulty { get; set; }

        [JsonPropertyName("overall_difficulty")]
        public double OverallDifficulty { get; set; }

        [JsonPropertyName("slider_factor")]
        public double SliderFactor { get; set; }

        [JsonPropertyName("speed_difficulty")]
        public double SpeedDifficulty { get; set; }

        // taiko
        [JsonPropertyName("stamina_difficulty")]
        public double StaminaDifficulty { get; set; }

        [JsonPropertyName("rhythm_difficulty")]
        public double RhythmDifficulty { get; set; }

        [JsonPropertyName("colour_difficulty")]
        public double ColourDifficulty { get; set; }

        // mania
        [JsonPropertyName("score_multiplier")]
        public double ScoreMultiplier { get; set; }
    }



}