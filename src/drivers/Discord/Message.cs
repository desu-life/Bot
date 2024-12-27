using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using KanonBot.Message;

namespace KanonBot.Drivers;

public partial class Discord
{
    [GeneratedRegex(@"<@(\d*?)>", RegexOptions.Multiline)]
    private static partial Regex AtPattern();

    [GeneratedRegex(@"<@&(\d*?)>", RegexOptions.Multiline)]
    private static partial Regex AtRolePattern();

    [GeneratedRegex(@"@everyone", RegexOptions.Multiline)]
    private static partial Regex AtEveryone();

    [GeneratedRegex(@"@here", RegexOptions.Multiline)]
    private static partial Regex AtHere();

    public class Message
    {
        /// <summary>
        /// 解析部分附件只支持图片
        /// </summary>
        /// <param name="MessageData"></param>
        /// <returns></returns>
        public static Chain Parse(IMessage MessageData)
        {
            var chain = new Chain();
            // 处理 content
            var segList = new List<(Match m, IMsgSegment seg)>();

            foreach (Match m in AtPattern().Matches(MessageData.Content).Cast<Match>())
            {
                segList.Add((m, new AtSegment(m.Groups[1].Value, Platform.Discord)));
            }

            foreach (Match m in AtRolePattern().Matches(MessageData.Content).Cast<Match>())
            {
                segList.Add((m, new AtSegment($"&{m.Groups[1].Value}", Platform.Discord)));
            }

            foreach (Match m in AtEveryone().Matches(MessageData.Content).Cast<Match>())
            {
                segList.Add((m, new AtSegment("all", Platform.Discord)));
            }

            foreach (Match m in AtHere().Matches(MessageData.Content).Cast<Match>())
            {
                segList.Add((m, new AtSegment("all", Platform.Discord)));
            }

            void AddText(ref Chain chain, string text)
            {
                // 匹配一下attacment
                foreach (var embed in MessageData.Embeds)
                {
                    if (embed.Type == EmbedType.Image)
                    {
                        // 添加图片
                        if (embed.Image is not null) {
                            chain.Add(new ImageSegment(embed.Image.Value.Url, ImageSegment.Type.Url));
                            // text = text.Replace(embed.Image.Value.Url, "");
                        }
                    }
                }
                if (text.Length != 0)
                    chain.Add(new TextSegment(Utils.KOOKUnEscape(text)));
            }

            var pos = 0;
            foreach (var x in segList.OrderBy(x => x.m.Index)) {
                if (pos < x.m.Index)
                {
                    AddText(ref chain, MessageData.Content[pos..x.m.Index]);
                }
                chain.Add(x.seg);
                pos = x.m.Index + x.m.Length;
            }

            if (pos < MessageData.Content.Length) {
                AddText(ref chain, MessageData.Content[pos..]);
            }

            return chain;
        }
    }
}
