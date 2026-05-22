#pragma warning disable CS8618 // 非null 字段未初始化

using System.ComponentModel;
using KanonBot.Message;
using KanonBot.Serializer;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

// 部分参考 https://github.com/DeepOceanSoft/Sora

namespace KanonBot.Drivers;
public partial class OneBot
{
    public class Models
    {

        public class Anonymous
        {
            /// <summary>
            /// 匿名用户 flag
            /// </summary>
            [JsonPropertyName("flag")]
            public string Flag { get; init; }

            /// <summary>
            /// 匿名用户 ID
            /// </summary>
            [JsonPropertyName("id")]
            public long Id { get; init; }

            /// <summary>
            /// 匿名用户名称
            /// </summary>
            [JsonPropertyName("name")]
            public string Name { get; init; }
        }

        public readonly struct Segment
        {
            /// <summary>
            /// 消息段类型
            /// </summary>
            [JsonPropertyName("type")]
            public Enums.SegmentType msgType { get; init; }

            /// <summary>
            /// 消息段JSON
            /// </summary>
            [JsonPropertyName("data")]
            public JsonObject rawData { get; init; }
        }

        public struct SendMessage
        {
            [JsonPropertyName("message_type")]
            public Enums.MessageType MessageType { get; set; }
            [JsonPropertyName("user_id")]
            public long? UserId { get; set; }
            [JsonPropertyName("group_id")]
            public long? GroupId { get; set; }
            [JsonPropertyName("message")]
            public List<Segment> Message { get; set; }
            [JsonPropertyName("auto_escape")]
            public bool AutoEscape { get; set; }
        }

        public class CQRequest
        {

            [JsonPropertyName("action")]
            public Enums.Actions action { get; init; }

            [JsonPropertyName("echo")]
            public Guid Echo { get; } = Guid.NewGuid();

            [JsonPropertyName("params")]
            public dynamic Params { get; init; }
        }
        public class CQResponse
        {

            [JsonPropertyName("status")]
            public string Status { get; init; }
            [JsonPropertyName("retcode")]
            public int RetCode { get; init; }
            [JsonPropertyName("echo")]
            public Guid Echo { get; init; }
            [JsonPropertyName("data")]
            public JsonObject Data { get; init; }

        }

        public class CQGroupAddRequest
        {

            [JsonPropertyName("sub_type")]
            public Enums.GroupRequestType RequestType { get; init; }
            [JsonPropertyName("approve")]
            public bool Approve { get; set; }

            [JsonPropertyName("reason")]
            public string Reason { set; get; }
        }

        public class Sender
        {
            [JsonPropertyName("role")]
            public Enums.GroupRole Role { get; set; }
            [JsonPropertyName("user_id")]
            public long UserId { get; set; }
            [JsonPropertyName("area")]
            public string Area { get; set; }
            [JsonPropertyName("card")]
            public string Aard { get; set; }
            [JsonPropertyName("level")]
            public string Level { get; set; }
            [JsonPropertyName("nickname")]
            public string NickName { get; set; }
            [JsonPropertyName("sex")]
            public string Sex { get; set; }
            [JsonPropertyName("age")]
            public int Age { get; set; }
        }

        public class CQEventBase
        {
            /// <summary>
            /// 事件发生的时间戳
            /// </summary>
            [JsonPropertyName("time")]
            public long Time { get; set; }

            /// <summary>
            /// 收到事件的机器人 QQ 号
            /// </summary>
            [JsonPropertyName("self_id")]
            public long? SelfId { get; set; }

            /// <summary>
            /// 事件类型
            /// </summary>
            [JsonPropertyName("post_type")]
            public string? PostType { get; set; }
        }

        public class CQMessageEventBase : CQEventBase
        {
            /// <summary>
            /// 消息类型
            /// </summary>
            [JsonPropertyName("message_type")]
            public string MessageType { get; set; }

            /// <summary>
            /// 消息子类型
            /// </summary>
            [JsonPropertyName("sub_type")]
            public string SubType { get; set; }

            /// <summary>
            /// 消息 ID
            /// </summary>
            [JsonPropertyName("message_id")]
            public int MessageId { get; set; }

            /// <summary>
            /// 发送者 QQ 号
            /// </summary>
            [JsonPropertyName("user_id")]
            public long UserId { get; set; }

            /// <summary>
            /// 消息内容
            /// </summary>
            [JsonPropertyName("message")]
            public List<Segment> MessageList { get; set; }

            /// <summary>
            /// 原始消息内容
            /// </summary>
            [JsonPropertyName("raw_message")]
            public string RawMessage { get; set; }

            /// <summary>
            /// 字体
            /// </summary>
            [JsonPropertyName("font")]
            public int Font { get; set; }
        }

        public class GroupMessage : CQMessageEventBase
        {
            /// <summary>
            /// 群号
            /// </summary>
            [JsonPropertyName("group_id")]
            public long GroupId { get; set; }

            /// <summary>
            /// 匿名信息
            /// </summary>
            [JsonPropertyName("anonymous")]
            public Anonymous? Anonymous { get; set; }

            /// <summary>
            /// 发送人信息
            /// </summary>
            [JsonPropertyName("sender")]
            public Sender SenderInfo { get; set; }

            /// <summary>
            /// 消息序号
            /// </summary>
            [JsonPropertyName("message_seq")]
            public long MessageSequence { get; set; }
        }

        public class PrivateMessage : CQMessageEventBase
        {
            /// <summary>
            /// 发送人信息
            /// </summary>
            [JsonPropertyName("sender")]
            public Sender SenderInfo { get; set; }
        }
    }
}
