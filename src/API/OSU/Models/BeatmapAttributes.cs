#pragma warning disable CS8618 // 非null 字段未初始化
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using KanonBot.Serializer;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class BeatmapAttributes
    {
        [JsonPropertyName("max_combo")]
        public int MaxCombo { get; set; }

        [JsonPropertyName("star_rating")]
        public double StarRating { get; set; }

        // osu部分
        [JsonPropertyName("aim_difficulty")]
        public double AimDifficulty { get; set; }

        [JsonPropertyName("aim_difficult_slider_count")]
        public double AimDifficultSliderCount { get; set; }

        [JsonPropertyName("speed_difficulty")]
        public double SpeedDifficulty { get; set; }

        [JsonPropertyName("speed_note_count")]
        public double SpeedNoteCount { get; set; }

        [JsonPropertyName("slider_factor")]
        public double SliderFactor { get; set; }

        [JsonPropertyName("aim_difficult_strain_count")]
        public double AimDifficultStrainCount { get; set; }

        [JsonPropertyName("speed_difficult_strain_count")]
        public double SpeedDifficultStrainCount { get; set; }

        // taiko
        [JsonPropertyName("mono_stamina_factor")]
        public double MonoStaminaFactor { get; set; }
    }
}
