#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class Mod
    {
        [JsonPropertyName("acronym")]
        public string Acronym { get; set; }

        [JsonPropertyName("settings")]
        public JsonObject? Settings { get; set; }

        [JsonIgnore]
        public bool IsClassic => Acronym == "CL";

        [JsonIgnore]
        public bool IsVisualMod => Acronym == "HD" || Acronym == "FL";

        [JsonIgnore]
        public bool IsSpeedChangeMod =>
            Acronym == "DT" || Acronym == "NC" || Acronym == "HT" || Acronym == "DC";

        public static Mod FromString(string mod)
        {
            return new Mod { Acronym = mod };
        }
    }
}