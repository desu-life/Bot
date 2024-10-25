#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{

    public class BeatmapAttributes
    {
        // 不包含在json解析中，用作分辨mode
        public Mode Mode { get; set; }

        [JsonProperty(PropertyName = "max_combo")]
        // 共有部分
        public int MaxCombo { get; set; }

        [JsonProperty(PropertyName = "star_rating")]
        public double StarRating { get; set; }

        // osu, taiko, fruits包含
        [JsonProperty(PropertyName = "approach_rate")]
        public double ApproachRate { get; set; }

        // taiko, mania包含
        [JsonProperty(PropertyName = "great_hit_window")]
        public double GreatHitWindow { get; set; }

        // osu部分
        [JsonProperty(PropertyName = "aim_difficulty")]
        public double AimDifficulty { get; set; }

        [JsonProperty(PropertyName = "flashlight_difficulty")]
        public double FlashlightDifficulty { get; set; }

        [JsonProperty(PropertyName = "overall_difficulty")]
        public double OverallDifficulty { get; set; }

        [JsonProperty(PropertyName = "slider_factor")]
        public double SliderFactor { get; set; }

        [JsonProperty(PropertyName = "speed_difficulty")]
        public double SpeedDifficulty { get; set; }

        // taiko
        [JsonProperty(PropertyName = "stamina_difficulty")]
        public double StaminaDifficulty { get; set; }

        [JsonProperty(PropertyName = "rhythm_difficulty")]
        public double RhythmDifficulty { get; set; }

        [JsonProperty(PropertyName = "colour_difficulty")]
        public double ColourDifficulty { get; set; }

        // mania
        [JsonProperty(PropertyName = "score_multiplier")]
        public double ScoreMultiplier { get; set; }
    }



}