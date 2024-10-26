#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class Mod
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

        public static Mod FromString(string mod)
        {
            return new Mod { Acronym = mod };
        }
    }
}