using KanonBot.Drivers;
using KanonBot.Message;
using KanonBot.API;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;
using LanguageExt.UnsafeValueAccess;
using KanonBot.Functions.OSU;
using KanonBot.OsuPerformance;


namespace KanonBot.Functions.OSUBot
{
    public class GetBackground
    {
        async public static Task Execute(Target target, string cmd)
        {
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Score);
            // 判断是否给定了bid
            if (command.order_number == -1)
            {
                await target.reply("请提供谱面参数。");
                return;
            }

            List<string> mods = new();
            try
            {
                mods = Enumerable
                    .Range(0, command.osu_mods.Length / 2)
                    .Select(p => new string(command.osu_mods.AsSpan().Slice(p * 2, 2)).ToUpper())
                    .ToList();
            }
            catch { }

            Log.Debug($"Mods: {string.Join(",", mods)}");

            var mods_lazer = mods.Map(API.OSU.Models.Mod.FromString).ToArray();

            var beatmaps = await API.OSU.Client.SearchBeatmap(command.order_number.ToString());

            if (beatmaps == null)
            {
                await target.reply("未找到谱面。");
                return;
            }

            if (beatmaps.Beatmapsets.Count == 0)
            {
                await target.reply("未找到谱面。");
                return;
            }

            var beatmapset = beatmaps.Beatmapsets[0];
            await target.reply($"https://assets.ppy.sh/beatmaps/{beatmapset.Id}/covers/raw.jpg");
        }
    }
}
