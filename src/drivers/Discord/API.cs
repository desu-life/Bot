using Newtonsoft.Json.Linq;
using KanonBot.Message;
using System.IO;
using Discord.WebSocket;
using libDiscord = Discord;
using Discord;
using System.Net.Http;

namespace KanonBot.Drivers;
public partial class Discord
{
    // API 部分 * 包装 Driver
    public class API
    {
        private string AuthToken;
        public API(string authToken)
        {
            this.AuthToken = $"Bot {authToken}";
        }

        // IFlurlRequest http()
        // {
        //     return EndPoint.WithHeader("Authorization", this.AuthToken);
        // }

        async public Task SendMessage(IMessageChannel channel, Chain msgChain, libDiscord.IMessage? originalTarget = null)
        {
            var messageRef = originalTarget is null ? null : new MessageReference(originalTarget.Id);
            foreach (var seg in msgChain.Iter()) {
                switch (seg)
                {
                    case ImageSegment s:
                        switch (s.t)
                        {
                            case ImageSegment.Type.Base64: {
                                    var uuid = Guid.NewGuid();
                                    using var _s = Utils.Byte2Stream(Convert.FromBase64String(s.value));
                                    await channel.SendFileAsync(_s, $"{uuid}.jpg", messageReference: messageRef);
                                } break;
                            case ImageSegment.Type.File: {
                                    var uuid = Guid.NewGuid();
                                    using var _s = Utils.LoadFile2ReadStream(s.value);
                                    await channel.SendFileAsync(_s, $"{uuid}.jpg", messageReference: messageRef);
                                } break;
                            case ImageSegment.Type.Url: {
                                    var uuid = Guid.NewGuid();
                                    using var _s = await s.value.GetStreamAsync();
                                    await channel.SendFileAsync(_s, $"{uuid}.jpg", messageReference: messageRef);
                                } break;
                            default:
                                break;
                        }
                        break;
                    case TextSegment s:
                        await channel.SendMessageAsync(s.value, messageReference: messageRef);
                        break;
                    case AtSegment s:
                        // 我不管，我就先不发送
                        break;
                    default:
                        throw new NotSupportedException("不支持的平台类型");
                }
            }
        }

      

       

    }
}