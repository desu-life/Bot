#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class PPlusData
    {
        public UserData User { get; set; }

        public UserPerformances[]? Performances { get; set; }

        public class UserData
        {
            [JsonPropertyName("Rank")]
            public int Rank { get; set; }

            [JsonPropertyName("CountryRank")]
            public int CountryRank { get; set; }

            [JsonPropertyName("UserID")]
            public long UserId { get; set; }

            [JsonPropertyName("UserName")]
            public string UserName { get; set; }

            [JsonPropertyName("CountryCode")]
            public string CountryCode { get; set; }

            [JsonPropertyName("PerformanceTotal")]
            public double PerformanceTotal { get; set; }

            [JsonPropertyName("AimTotal")]
            public double AimTotal { get; set; }

            [JsonPropertyName("JumpAimTotal")]
            public double JumpAimTotal { get; set; }

            [JsonPropertyName("FlowAimTotal")]
            public double FlowAimTotal { get; set; }

            [JsonPropertyName("PrecisionTotal")]
            public double PrecisionTotal { get; set; }

            [JsonPropertyName("SpeedTotal")]
            public double SpeedTotal { get; set; }

            [JsonPropertyName("StaminaTotal")]
            public double StaminaTotal { get; set; }

            [JsonPropertyName("AccuracyTotal")]
            public double AccuracyTotal { get; set; }

            [JsonPropertyName("AccuracyPercentTotal")]
            public double AccuracyPercentTotal { get; set; }

            [JsonPropertyName("PlayCount")]
            public int PlayCount { get; set; }

            [JsonPropertyName("CountRankSS")]
            public int CountRankSS { get; set; }

            [JsonPropertyName("CountRankS")]
            public int CountRankS { get; set; }
        }

        public class UserDataNext
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("performances")]
            public UserPerformancesNext Performances { get; set; }
        }

        public class UserPerformancesNext
        {
            [JsonPropertyName("pp")]
            public double PerformanceTotal { get; set; }

            [JsonPropertyName("ppAim")]
            public double AimTotal { get; set; }

            [JsonPropertyName("ppJumpAim")]
            public double JumpAimTotal { get; set; }

            [JsonPropertyName("ppFlowAim")]
            public double FlowAimTotal { get; set; }

            [JsonPropertyName("ppPrecision")]
            public double PrecisionTotal { get; set; }

            [JsonPropertyName("ppSpeed")]
            public double SpeedTotal { get; set; }

            [JsonPropertyName("ppStamina")]
            public double StaminaTotal { get; set; }

            [JsonPropertyName("ppAcc")]
            public double AccuracyTotal { get; set; }
        }

        public class UserPerformances
        {
            [JsonPropertyName("SetID")]
            public long SetId { get; set; }

            [JsonPropertyName("Artist")]
            public string Artist { get; set; }

            [JsonPropertyName("Title")]
            public string Title { get; set; }

            [JsonPropertyName("Version")]
            public string Version { get; set; }

            [JsonPropertyName("MaxCombo")]
            public int MaxCombo { get; set; }

            [JsonPropertyName("UserID")]
            public long UserId { get; set; }

            [JsonPropertyName("BeatmapID")]
            public long BeatmapId { get; set; }

            [JsonPropertyName("Total")]
            public double TotalTotal { get; set; }

            [JsonPropertyName("Aim")]
            public double Aim { get; set; }

            [JsonPropertyName("JumpAim")]
            public double JumpAim { get; set; }

            [JsonPropertyName("FlowAim")]
            public double FlowAim { get; set; }

            [JsonPropertyName("Precision")]
            public double Precision { get; set; }

            [JsonPropertyName("Speed")]
            public double Speed { get; set; }

            [JsonPropertyName("Stamina")]
            public double Stamina { get; set; }

            [JsonPropertyName("HigherSpeed")]
            public double HigherSpeed { get; set; }

            [JsonPropertyName("Accuracy")]
            public double Accuracy { get; set; }

            [JsonPropertyName("Count300")]
            public int CountGreat { get; set; }

            [JsonPropertyName("Count100")]
            public int CountOk { get; set; }

            [JsonPropertyName("Count50")]
            public int CountMeh { get; set; }

            [JsonPropertyName("Misses")]
            public int CountMiss { get; set; }

            [JsonPropertyName("AccuracyPercent")]
            public double AccuracyPercent { get; set; }

            [JsonPropertyName("Combo")]
            public int Combo { get; set; }

            [JsonPropertyName("EnabledMods")]
            public int EnabledMods { get; set; }

            [JsonPropertyName("Rank")]
            public string Rank { get; set; }

            [JsonPropertyName("Date")]
            public DateTimeOffset Date { get; set; }
        }
    }
}
