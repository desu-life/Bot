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
            // 判断是否给定了bid
            if (string.IsNullOrWhiteSpace(cmd))
            {
                await target.reply("请提供谱面参数。");
                return;
            }

            var beatmaps = await API.OSU.Client.SearchBeatmap(cmd);

            var beatmapset = beatmaps!.Beatmapsets[0];
            var beatmap = beatmapset.Beatmaps!.First();
            beatmap.Beatmapset = beatmaps.Beatmapsets[0];

            var data = await RosuCalculator.CalculatePanelSSData(beatmap);
            
            data.scoreInfo.UserId = 3;  // bancho bot
            data.scoreInfo.User = await API.OSU.Client.GetUser(data.scoreInfo.UserId);
            data.scoreInfo.Beatmapset = beatmapset;
            data.scoreInfo.Beatmap = beatmap;
            
            using var stream = new MemoryStream();
            using var img = await LegacyImage.Draw.DrawScore(data);
            await img.SaveAsync(stream, new JpegEncoder());
            await target.reply(
                new Chain().image(
                    Convert.ToBase64String(stream.ToArray(), 0, (int)stream.Length),
                    ImageSegment.Type.Base64
                )
            );
        }
    }
}
