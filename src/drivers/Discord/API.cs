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
            var allowedMentions = new AllowedMentions(AllowedMentionTypes.None);
            foreach (var seg in msgChain.Iter()) {
                switch (seg)
                {
                    case ImageSegment s:
                        switch (s.t)
                        {
                            case ImageSegment.Type.Base64: {
                                    var uuid = Guid.NewGuid();
                                    using var _s = Utils.Byte2Stream(Convert.FromBase64String(s.value));
                                    await channel.SendFileAsync(_s, $"{uuid}.png", messageReference: messageRef, allowedMentions: allowedMentions);
                                    messageRef = null;
                                } break;
                            case ImageSegment.Type.File: {
                                    var uuid = Guid.NewGuid();
                                    using var _s = Utils.LoadFile2ReadStream(s.value);
                                    await channel.SendFileAsync(_s, $"{uuid}.png", messageReference: messageRef, allowedMentions: allowedMentions);
                                    messageRef = null;
                                } break;
                            case ImageSegment.Type.Url: {
                                    var uuid = Guid.NewGuid();
                                    using var _s = await s.value.GetStreamAsync();
                                    await channel.SendFileAsync(_s, $"{uuid}.png", messageReference: messageRef, allowedMentions: allowedMentions);
                                    messageRef = null;
                                } break;
                            default:
                                break;
                        }
                        break;
                    case TextSegment s:
                        await channel.SendMessageAsync(s.value, messageReference: messageRef, allowedMentions: allowedMentions);
                        messageRef = null;
                        break;
                    case AtSegment s:
                        if (s.value == "all") {
                            await channel.SendMessageAsync($"@everyone", messageReference: messageRef);
                        } else {
                            await channel.SendMessageAsync($"<@{s.value}>", messageReference: messageRef);
                        }
                        messageRef = null;
                        break;
                    default:
                        await channel.SendMessageAsync(seg.Build(), messageReference: messageRef, allowedMentions: allowedMentions);
                        messageRef = null;
                        break;
                }
            }
        }

      

       

    }
}