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
    public class Search
    {
        async public static Task Execute(Target target, string cmd)
        {
            var command = BotCmdHelper.CmdParser(cmd, BotCmdHelper.FuncType.Score);
            var mode = command.osu_mode;
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

            var beatmaps = await API.OSU.Client.SearchBeatmap(command.order_number.ToString(), mode);

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

            var beatmapset = beatmaps!.Beatmapsets[0];
            var beatmap = beatmapset.Beatmaps!.First();
            beatmap.Beatmapset = beatmaps.Beatmapsets[0];

            var data = await OsuCalculator.CalculatePanelSSData(beatmap, mods_lazer);
            
            data.scoreInfo.UserId = 3;  // bancho bot
            data.scoreInfo.User = await API.OSU.Client.GetUser(data.scoreInfo.UserId);
            data.scoreInfo.Beatmapset = beatmapset;
            data.scoreInfo.Beatmap = beatmap;
            data.scoreInfo.ModeInt = beatmap.Mode.ToNum();
            data.scoreInfo.Mods = mods_lazer;
            data.scoreInfo.EndedAt = DateTime.UtcNow;
            data.scoreInfo.StartedAt = DateTime.UtcNow;
            data.scoreInfo.Score = 1000000;
            
            using var stream = new MemoryStream();
            using var img = await LegacyImage.Draw.DrawScore(data);
            await img.SaveAsync(stream, new JpegEncoder());
            await target.reply(
                new Chain().image(
                    Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                    ImageSegment.Type.Base64
                ).msg("该功能为测试功能。")
            );
        }
    }
}
