#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class PPlusData
    {
        public UserData User { get; set; }

        public UserPerformances[]? Performances { get; set; }

        public class UserData
        {
            [JsonProperty("Rank")]
            public int Rank { get; set; }

            [JsonProperty("CountryRank")]
            public int CountryRank { get; set; }

            [JsonProperty("UserID")]
            public long UserId { get; set; }

            [JsonProperty("UserName")]
            public string UserName { get; set; }

            [JsonProperty("CountryCode")]
            public string CountryCode { get; set; }

            [JsonProperty("PerformanceTotal")]
            public double PerformanceTotal { get; set; }

            [JsonProperty("AimTotal")]
            public double AimTotal { get; set; }

            [JsonProperty("JumpAimTotal")]
            public double JumpAimTotal { get; set; }

            [JsonProperty("FlowAimTotal")]
            public double FlowAimTotal { get; set; }

            [JsonProperty("PrecisionTotal")]
            public double PrecisionTotal { get; set; }

            [JsonProperty("SpeedTotal")]
            public double SpeedTotal { get; set; }

            [JsonProperty("StaminaTotal")]
            public double StaminaTotal { get; set; }

            [JsonProperty("AccuracyTotal")]
            public double AccuracyTotal { get; set; }

            [JsonProperty("AccuracyPercentTotal")]
            public double AccuracyPercentTotal { get; set; }

            [JsonProperty("PlayCount")]
            public int PlayCount { get; set; }

            [JsonProperty("CountRankSS")]
            public int CountRankSS { get; set; }

            [JsonProperty("CountRankS")]
            public int CountRankS { get; set; }
        }

        public class UserPerformances
        {
            [JsonProperty("SetID")]
            public long SetId { get; set; }

            [JsonProperty("Artist")]
            public string Artist { get; set; }

            [JsonProperty("Title")]
            public string Title { get; set; }

            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("MaxCombo")]
            public int MaxCombo { get; set; }

            [JsonProperty("UserID")]
            public long UserId { get; set; }

            [JsonProperty("BeatmapID")]
            public long BeatmapId { get; set; }

            [JsonProperty("Total")]
            public double TotalTotal { get; set; }

            [JsonProperty("Aim")]
            public double Aim { get; set; }

            [JsonProperty("JumpAim")]
            public double JumpAim { get; set; }

            [JsonProperty("FlowAim")]
            public double FlowAim { get; set; }

            [JsonProperty("Precision")]
            public double Precision { get; set; }

            [JsonProperty("Speed")]
            public double Speed { get; set; }

            [JsonProperty("Stamina")]
            public double Stamina { get; set; }

            [JsonProperty("HigherSpeed")]
            public double HigherSpeed { get; set; }

            [JsonProperty("Accuracy")]
            public double Accuracy { get; set; }

            [JsonProperty("Count300")]
            public int CountGreat { get; set; }

            [JsonProperty("Count100")]
            public int CountOk { get; set; }

            [JsonProperty("Count50")]
            public int CountMeh { get; set; }

            [JsonProperty("Misses")]
            public int CountMiss { get; set; }

            [JsonProperty("AccuracyPercent")]
            public double AccuracyPercent { get; set; }

            [JsonProperty("Combo")]
            public int Combo { get; set; }

            [JsonProperty("EnabledMods")]
            public int EnabledMods { get; set; }

            [JsonProperty("Rank")]
            public string Rank { get; set; }

            [JsonProperty("Date")]
            public DateTimeOffset Date { get; set; }
        }
    }
}