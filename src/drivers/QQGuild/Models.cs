#pragma warning disable CS8618 // 非null 字段未初始化

using System.Text.Json.Serialization;
using KanonBot.Message;
using KanonBot.Serializer;
using System.Text.Json.Nodes;

namespace KanonBot.Drivers;
public partial class QQGuild
{
    public class Models
    {
        public class SendMessageData
        {
            /// <summary>
            /// 消息内容，文本内容，支持内嵌格式
            /// </summary>
            [JsonPropertyName("content")]
            public string? Content { get; set; }
            /// <summary>
            /// 图片url地址，平台会转存该图片，用于下发图片消息
            /// </summary>
            [JsonPropertyName("image")]
            public string? ImageUrl { get; set; }
            /// <summary>
            /// 要回复的消息id(Message.id), 在 AT_CREATE_MESSAGE 事件中获取。
            /// </summary>
            [JsonPropertyName("msg_id")]
            public string? MessageId { get; set; }
            /// <summary>
            /// 引用消息对象
            /// </summary>
            [JsonPropertyName("message_reference")]
            public MessageReference? MessageReference { get; set; }

            // TODO: 下列消息的解析

            /// <summary>
            /// embed
            /// </summary>
            [JsonPropertyName("embed")]
            public JsonNode? Embed { get; set; }
            /// <summary>
            /// ark消息
            /// </summary>
            [JsonPropertyName("ark")]
            public JsonNode? Ark { get; set; }

            public SendMessageData Build(Chain msgChain) => Message.Build(this, msgChain);
        }
        public class PayloadBase<T>
        {
            /// <summary>
            /// 操作码
            /// </summary>
            [JsonPropertyName("op")]
            public Enums.OperationCode Operation { get; set; }

            /// <summary>
            /// 事件类型
            /// </summary>
            [JsonPropertyName("t")]
            public Enums.EventType Type { get; set; }

            /// <summary>
            /// 事件内容
            /// </summary>
            [JsonPropertyName("d")]
            public T? Data { get; set; }

            /// <summary>
            /// s 下行消息都会有一个序列号，标识消息的唯一性，客户端需要再发送心跳的时候，携带客户端收到的最新的s。
            /// </summary>
            [JsonPropertyName("s")]
            public int? Seq { get; set; }

        }


        public class Member
        {
            /// <summary>
            /// 用户的昵称
            /// </summary>
            [JsonPropertyName("nico")]
            public string Nick { get; set; }
            /// <summary>
            /// 用户加入频道的时间
            /// </summary>
            [JsonPropertyName("joined_at")]
            public DateTimeOffset JoinedAt { get; set; }
            /// <summary>
            /// 用户在频道内的身份组ID
            /// </summary>
            [JsonPropertyName("roles")]
            public Enums.DefaultRole[] Roles { get; set; }
            /// <summary>
            /// 用户的频道基础信息，只有成员相关接口中会填充此信息
            /// </summary>
            [JsonPropertyName("user")]
            public User? User { get; set; }
        }
        public class User
        {
            /// <summary>
            /// 用户 id
            /// </summary>
            [JsonPropertyName("id")]
            public string ID { get; set; }
            /// <summary>
            /// 用户名
            /// </summary>
            [JsonPropertyName("username")]
            public string UserName { get; set; }
            /// <summary>
            /// 是否是机器人
            /// </summary>
            [JsonPropertyName("bot")]
            public bool isBot { get; set; }
            /// <summary>
            /// 用户头像地址
            /// </summary>
            [JsonPropertyName("avatar")]
            public string? Avatar { get; set; }
            /// <summary>
            /// 特殊关联应用的 openid，需要特殊申请并配置后才会返回。如需申请，请联系平台运营人员。
            /// </summary>
            [JsonPropertyName("union_openid")]
            public string? UnionOpenID { get; set; }
            /// <summary>
            /// 机器人关联的互联应用的用户信息，与union_openid关联的应用是同一个。如需申请，请联系平台运营人员。
            /// </summary>
            [JsonPropertyName("union_user_account")]
            public string? UnionUserAccount { get; set; }
        }

        public class MessageData
        {
            /// <summary>
            /// 消息发送者
            /// </summary>
            [JsonPropertyName("author")]
            public User Author { get; set; }
            /// <summary>
            /// 消息内容
            /// </summary>
            [JsonPropertyName("content")]
            public string Content { get; set; }
            /// <summary>
            /// 消息 ID
            /// </summary>
            [JsonPropertyName("id")]
            public string ID { get; set; }
            /// <summary>
            /// 子频道 ID
            /// </summary>
            [JsonPropertyName("channel_id")]
            public string ChannelID { get; set; }
            /// <summary>
            /// 频道 ID
            /// </summary>
            [JsonPropertyName("guild_id")]
            public string GuildID { get; set; }
            /// <summary>
            /// 消息中@的人
            /// </summary>
            [JsonPropertyName("mentions")]
            public User[] Mentions { get; set; }
            /// <summary>
            /// 消息创建者的member信息
            /// </summary>
            [JsonPropertyName("member")]
            public Member Member { get; set; }
            /// <summary>
            /// 用于消息间的排序，seq 在同一子频道中按从先到后的顺序递增，不同的子频道之间消息无法排序。(目前只在消息事件中有值，2022年8月1日 后续废弃)
            /// </summary>
            [JsonPropertyName("seq")]
            public int Seq { get; set; }
            /// <summary>
            /// 子频道消息 seq，用于消息间的排序，seq 在同一子频道中按从先到后的顺序递增，不同的子频道之间消息无法排序
            /// </summary>
            [JsonPropertyName("seq_in_channel")]
            public int SeqInChannel { get; set; }
            /// <summary>
            /// 消息创建时间
            /// </summary>
            [JsonPropertyName("timestamp")]
            public DateTimeOffset Time { get; set; }
            /// <summary>
            /// 消息编辑时间
            /// </summary>
            [JsonPropertyName("edited_timestamp")]
            public DateTimeOffset? EditedTime { get; set; }
            /// <summary>
            /// 是否是@全员消息
            /// </summary>
            [JsonPropertyName("mention_everyone")]
            public bool? MentionEveryone { get; set; }
            /// <summary>
            /// 引用消息对象
            /// </summary>
            [JsonPropertyName("message_reference")]
            public MessageReference? MessageReference { get; set; }

            // TODO: 下列消息的解析

            /// <summary>
            /// 附件
            /// </summary>
            [JsonPropertyName("attachments")]
            public List<JsonNode>? Attachments { get; set; }
            /// <summary>
            /// embed
            /// </summary>
            [JsonPropertyName("embeds")]
            public List<JsonNode>? Embeds { get; set; }
            /// <summary>
            /// ark消息
            /// </summary>
            [JsonPropertyName("ark")]
            public JsonNode? Ark { get; set; }
        }
        public class MessageReference
        {
            /// <summary>
            /// 消息 ID
            /// </summary>
            [JsonPropertyName("message_id")]
            public string MessageId { get; set; }
            /// <summary>
            /// 是否忽略获取引用消息详情错误，默认否
            /// </summary>
            [JsonPropertyName("ignore_get_message_error")]
            public bool IgnoreGetMessageError { get; set; } = false;
        }

        public class ReadyData
        {
            /// <summary>
            /// 版本
            /// </summary>
            [JsonPropertyName("version")]
            public int Version { get; set; }
            /// <summary>
            /// 会话ID
            /// </summary>
            [JsonPropertyName("session_id")]
            public Guid SessionId { get; set; }
            /// <summary>
            /// 用户信息
            /// </summary>
            [JsonPropertyName("user")]
            public User User { get; set; }
            /// <summary>
            /// shard 该参数是用来进行水平分片的。该参数是个拥有两个元素的数组。例如：[0,4]，代表分为四个片，当前链接是第 0 个片，业务稍后应该继续建立 shard 为[1,4],[2,4],[3,4]的链接，才能完整接收事件。
            /// </summary>
            [JsonPropertyName("shard")]
            public int[] Shard { get; set; }

        }

        public class ResumeData
        {
            /// <summary>
            /// token 是创建机器人的时候分配的，格式为Bot {appid}.{app_token}
            /// </summary>
            [JsonPropertyName("token")]
            public string Token { get; set; }
            /// <summary>
            /// 会话ID
            /// </summary>
            [JsonPropertyName("session_id")]
            public Guid SessionId { get; set; }
            /// <summary>
            /// 在接收事件时候的 s 字段
            /// </summary>
            [JsonPropertyName("seq")]
            public int Seq { get; set; }
        }

        public class IdentityData
        {
            /// <summary>
            /// token 是创建机器人的时候分配的，格式为Bot {appid}.{app_token}
            /// </summary>
            [JsonPropertyName("token")]
            public string Token { get; set; }

            /// <summary>
            /// intents 是此次连接所需要接收的事件
            /// </summary>
            [JsonPropertyName("intents")]
            public Enums.Intent Intents { get; set; }

            /// <summary>
            /// shard 该参数是用来进行水平分片的。该参数是个拥有两个元素的数组。例如：[0,4]，代表分为四个片，当前链接是第 0 个片，业务稍后应该继续建立 shard 为[1,4],[2,4],[3,4]的链接，才能完整接收事件。
            /// </summary>
            [JsonPropertyName("shard")]
            public int[] Shard { get; set; }

            /// <summary>
            /// properties 目前无实际作用，可以按照自己的实际情况填写，也可以留空
            /// </summary>
            [JsonPropertyName("properties")]
            public Properties Prop { get; set; } = new Properties();

            public class Properties {
                // 自动获取当前运行系统类型
                [JsonPropertyName("$os")]
                public string Os { get; set; } = Environment.OSVersion.Platform.ToString();
                [JsonPropertyName("$browser")]
                public string Browser { get; set; } = "KanonBot";
                [JsonPropertyName("$device")]
                public string Device { get; set; } = "KanonBot";
            }
        }
    }
}
