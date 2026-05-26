using System.Text.Json.Nodes;
using KanonBot.API;
using KanonBot.Message;

namespace KanonBot.Drivers;

public partial class OneBot
{
    public class Message
    {
        public static List<Models.Segment> Build(Chain msgChain)
        {
            var ListSegment = new List<Models.Segment>();
            foreach (var msg in msgChain.Iter())
            {
                ListSegment.Add(
                    msg switch {
                        TextSegment text => new Models.Segment {
                            msgType = Enums.SegmentType.Text,
                            rawData = new JsonObject { { "text", text.value } }
                        },
                        ImageSegment image => new Models.Segment {
                            msgType = Enums.SegmentType.Image,
                            rawData = image.t switch {
                                ImageSegment.Type.Base64 => new JsonObject { { "file", $"base64://{image.value}" } },
                                ImageSegment.Type.Url => new JsonObject { { "file", image.value } },
                                ImageSegment.Type.File => new JsonObject { { "file", Ali.PutFile(Utils.LoadFile2Byte(image.value).Result, "jpg") } }, // 这里还有缺陷，如果图片上传失败的话，还是会尝试发送
                                _ => throw new ArgumentException("不支持的图片类型")
                            }
                        },
                        AtSegment at => at.platform switch {
                            Platform.OneBot => new Models.Segment {
                                msgType = Enums.SegmentType.At,
                                rawData = new JsonObject { { "qq", at.value } }
                            },
                            _ => throw new ArgumentException("不支持的平台类型")
                        },
                        EmojiSegment face => new Models.Segment {
                            msgType = Enums.SegmentType.Face,
                            rawData = new JsonObject { { "id", face.value } }
                        },
                        // 收到未知消息就转换为纯文本
                        _ => new Models.Segment {
                            msgType = Enums.SegmentType.Text,
                            rawData = new JsonObject { { "text", msg.Build() } }
                        }
                    }
                );
            }
            return ListSegment;
        }

        public static Chain Parse(List<Models.Segment> MessageList)
        {
            var chain = new Chain();
            foreach (var obj in MessageList)
            {
                ArgumentNullException.ThrowIfNull(obj.rawData);
                chain.Add(
                    obj.msgType switch {
                        Enums.SegmentType.Text => new TextSegment(obj.rawData["text"]!.ToString()),
                        Enums.SegmentType.Image => obj.rawData.ContainsKey("url") ? new ImageSegment(obj.rawData["url"]!.ToString(), ImageSegment.Type.Url) : new ImageSegment(obj.rawData["file"]!.ToString(), ImageSegment.Type.File),
                        Enums.SegmentType.At => new AtSegment(obj.rawData["qq"]!.ToString(), Platform.OneBot),
                        Enums.SegmentType.Face => new EmojiSegment(obj.rawData["id"]!.ToString()),
                        _ => new RawSegment(obj.msgType.ToString(), obj.rawData)
                    }
                );
            }
            return chain;
        }
    }
}
