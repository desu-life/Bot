#pragma warning disable CS8618 // 非null 字段未初始化
using System.ComponentModel;
using System.Text.RegularExpressions;
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace KanonBot.API.PPYSB;

public partial class Models
{
    public class ApiResponse {
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
    public class ApiResponseV2 {
        [JsonPropertyName("status")]
        public string Status { get; set; }
        
        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }
    }
   
}