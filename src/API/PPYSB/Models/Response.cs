#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.RegularExpressions;
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.PPYSB;

public partial class Models
{
    public class ApiResponse {
        [JsonProperty("status")]
        public string Status { get; set; }
    }
    public class ApiResponseV2 {
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("meta")]
        public Dictionary<string, object> Meta { get; set; }
    }
   
}