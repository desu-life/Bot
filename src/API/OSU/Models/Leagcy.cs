#pragma warning disable CS8618 // 非null 字段未初始化
using KanonBot.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace KanonBot.API.OSU;

public partial class Models
{
    public class UserV1
    {
        [JsonProperty("user_id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long UserId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("join_date")]
        public DateTimeOffset JoinDate { get; set; }

        [JsonProperty("count300")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Count300 { get; set; }

        [JsonProperty("count100")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Count100 { get; set; }

        [JsonProperty("count50")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Count50 { get; set; }

        [JsonProperty("playcount")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Playcount { get; set; }

        [JsonProperty("ranked_score")]
        public string RankedScore { get; set; }

        [JsonProperty("total_score")]
        public string TotalScore { get; set; }

        [JsonProperty("pp_rank")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PpRank { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("pp_raw")]
        public string PpRaw { get; set; }

        [JsonProperty("accuracy")]
        public string Accuracy { get; set; }

        [JsonProperty("count_rank_ss")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankSs { get; set; }

        [JsonProperty("count_rank_ssh")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankSsh { get; set; }

        [JsonProperty("count_rank_s")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankS { get; set; }

        [JsonProperty("count_rank_sh")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankSh { get; set; }

        [JsonProperty("count_rank_a")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long CountRankA { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("total_seconds_played")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TotalSecondsPlayed { get; set; }

        [JsonProperty("pp_country_rank")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PpCountryRank { get; set; }

        [JsonProperty("events")]
        public object[] Events { get; set; }

        internal class ParseStringConverter : JsonConverter
        {
            public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

            public override object? ReadJson(
                JsonReader reader,
                Type t,
                object? existingValue,
                JsonSerializer serializer
            )
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;
                var value = serializer.Deserialize<string>(reader);
                if (Int64.TryParse(value, out long l))
                {
                    return l;
                }
                throw new Exception("Cannot unmarshal type long");
            }

            public override void WriteJson(
                JsonWriter writer,
                object? untypedValue,
                JsonSerializer serializer
            )
            {
                if (untypedValue == null)
                {
                    serializer.Serialize(writer, null);
                    return;
                }
                var value = (long)untypedValue;
                serializer.Serialize(writer, value.ToString());
                return;
            }

            public static readonly ParseStringConverter Singleton = new();
        }
    }

}