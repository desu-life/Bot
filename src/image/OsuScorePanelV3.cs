using System.IO;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Img = SixLabors.ImageSharp.Image;
using OSU = KanonBot.API.OSU;
using static KanonBot.Image.Fonts;

namespace KanonBot.Image
{
    public static class OsuScorePanelV3
    {
        public static async Task<Img> Draw(ScorePanelData data)
        {
            var scoreimg = new Image<Rgba32>(2848, 1602);

            var ppInfo = data.ppInfo!;

            using var avatar = await Utils.LoadOrDownloadAvatar(data.scoreInfo.User!);

            using var panel = await Img.LoadAsync(data.scoreInfo.Passed
                ? "./work/panelv2/score_panel/Score_v3_Passed_Panel.png"
                : "./work/panelv2/score_panel/Score_v3_Failed_Panel.png");
            scoreimg.Mutate(x => x.DrawImage(panel, 1));

            await DrawBackground(scoreimg, data);
            await DrawModeIcon(scoreimg, data);
            DrawAvatar(scoreimg, avatar);

            var textOptions = new RichTextOptions(TorusSemiBold.Get(120))
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            await DrawBeatmapInfo(scoreimg, data, textOptions);
            DrawUserInfo(scoreimg, data, textOptions);
            await DrawMods(scoreimg, data, textOptions);
            DrawMainPP(scoreimg, data, textOptions);
            DrawLengthGraph(scoreimg, data, textOptions);
            DrawPPDetails(scoreimg, data, textOptions);

            return scoreimg;
        }

        private static async Task DrawBackground(Image<Rgba32> scoreimg, ScorePanelData data)
        {
            var bg = await Utils.LoadOrDownloadBackground(data.scoreInfo.Beatmap!.BeatmapsetId, data.scoreInfo.Beatmap.BeatmapId);
            bg ??= await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");

            using var bgarea = new Image<Rgba32>(631, 444);
            bgarea.Mutate(x => x.Fill(Color.ParseHex("#f2f2f2")).RoundCorner(new Size(631, 444), 20));

            using var bgstatus = new Image<Rgba32>(619, 80);
            var statusColor = data.scoreInfo.Beatmap.Status switch
            {
                OSU.Models.Status.Approved => Color.ParseHex("#14b400"),
                OSU.Models.Status.Ranked => Color.ParseHex("#66bdff"),
                OSU.Models.Status.Loved => Color.ParseHex("#ff66aa"),
                _ => Color.ParseHex("#e08918")
            };
            bgstatus.Mutate(x => x.Fill(statusColor).RoundCorner(new Size(619, 80), 20));
            bgarea.Mutate(x => x.DrawImage(bgstatus, new Point(6, 358), 1));

            using var bg2 = bg.Clone(x => x.RoundCorner(new Size(619, 401), 20));
            bgarea.Mutate(x => x.DrawImage(bg2, new Point(6, 6), 1));
            scoreimg.Mutate(x => x.DrawImage(bgarea, new Point(70, 51), 1));

            bg.Dispose();
        }

            //TODO beatmap status icon

            //beatmap difficulty icon
            using var osuscoremode_icon = await Utils.ReadImageRgba(
                        $"./work/panelv2/icons/mode_icon/score/{data.scoreInfo.Mode.ToStr()}.png"
            );
            osuscoremode_icon.Mutate(x => x.Resize(110, 110));
            var modeC = Utils.ForStarDifficulty(data.ppInfo!.star);
            osuscoremode_icon.Mutate(
                x =>
                    x.ProcessPixelRowsAsVector4(row =>
                    {
                        for (int p = 0; p < row.Length; p++)
                        {
                            row[p].X = ((Vector4)modeC).X;
                            row[p].Y = ((Vector4)modeC).Y;
                            row[p].Z = ((Vector4)modeC).Z;
                        }
                    })
            );
            scoreimg.Mutate(
                        x =>
                            x.DrawImage(osuscoremode_icon, new Point(794, 381), 1)
                    );

        private static void DrawAvatar(Image<Rgba32> scoreimg, Image<Rgba32> avatar)
        {
            avatar.Mutate(x => x.Resize(100, 100).RoundCorner(new Size(100, 100), 50));
            scoreimg.Mutate(x => x
                .Fill(Color.White, new EllipsePolygon(140, 618, 105, 105))
                .DrawImage(avatar, new Point(90, 568), 1));
        }

        private static async Task DrawBeatmapInfo(Image<Rgba32> scoreimg, ScorePanelData data, RichTextOptions textOptions)
        {
            // Title
            var title = Utils.TruncateTextByWidth(data.scoreInfo.Beatmapset!.Title, textOptions, 1130);
            textOptions.Font = TorusSemiBold.Get(100);
            DrawTextWithShadow(scoreimg, textOptions, title, Color.ParseHex("#4d4d4d"), Color.ParseHex("#404040"), 769, 160, 158);

            // Creator
            var creator = Utils.TruncateTextByWidth(data.scoreInfo.Beatmapset.Creator, textOptions, 810);
            textOptions.Font = TorusRegular.Get(60);
            textOptions.Origin = new PointF(1070, 234);
            scoreimg.Mutate(x => x.DrawText(textOptions, creator, Color.ParseHex("#e36a79")));

            // Artist
            var artist = Utils.TruncateTextByWidth(data.scoreInfo.Beatmapset.Artist, textOptions, 450);
            textOptions.Origin = new PointF(1005, 322);
            scoreimg.Mutate(x => x.DrawText(textOptions, artist, Color.ParseHex("#6cac9c")));

            // Beatmap ID
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Font = TorusRegular.Get(50);
            textOptions.Origin = new PointF(1770, 322);
            scoreimg.Mutate(x => x.DrawText(textOptions, data.scoreInfo.Beatmap!.BeatmapId.ToString(), Color.ParseHex("#5872df")));

            // Stars
            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
            textOptions.Font = TorusSemiBold.Get(50);
            var starsText = $"Stars: {data.ppInfo.star:0.##}";
            var starsMeasure = TextMeasurer.MeasureSize(starsText, textOptions);
            DrawTextWithShadow(scoreimg, textOptions, starsText, Color.ParseHex("#f1c959"), Color.ParseHex("#3a3b3c"), 924, 442, 441);

            // Star icons
            await DrawStarIcons(scoreimg, data.ppInfo.star, 924 + (int)starsMeasure.Width + 10);

            // Version
            textOptions.Font = TorusRegular.Get(40);
            var version = Utils.TruncateTextByWidth(data.scoreInfo.Beatmap!.Version, textOptions, 740);
            var versionText = $"Version: {version}";
            DrawTextWithShadow(scoreimg, textOptions, versionText, Color.ParseHex("#333333"), Color.ParseHex("#3a3b3c"), 924, 480, 478);
        }

        private static async Task DrawStarIcons(Image<Rgba32> scoreimg, double star, int startPos)
        {
            var wholeStars = (int)Math.Floor(star);
            var fractional = star - Math.Truncate(star);

            using var icon = await Utils.ReadImageRgba("./work/panelv2/score_panel/Star.png");
            icon.Mutate(x => x.Resize(30, 30));

            if (wholeStars < 19)
            {
                for (int i = 0; i < wholeStars; i++)
                {
                    scoreimg.Mutate(x => x.DrawImage(icon, new Point(startPos, 401), 1));
                    startPos += 34;
                }
            }
            var fractSize = 10 + (int)(20.0 * fractional);
            var offset = (30 - fractSize) / 2;
            icon.Mutate(x => x.Resize(fractSize, fractSize));
            scoreimg.Mutate(x => x.DrawImage(icon, new Point(startPos + offset, 401 + offset), 1));
        }

        private static void DrawUserInfo(Image<Rgba32> scoreimg, ScorePanelData data, RichTextOptions textOptions)
        {
            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
            textOptions.Font = TorusSemiBold.Get(50);
            textOptions.Origin = new PointF(235, 630);
            scoreimg.Mutate(x => x.DrawText(textOptions, data.scoreInfo.User!.Username, Color.ParseHex("#333333")));

            textOptions.Font = TorusRegular.Get(36);
            textOptions.Origin = new PointF(235, 664);
            scoreimg.Mutate(x => x.DrawText(textOptions, data.scoreInfo.EndedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"), Color.ParseHex("#333333")));
        }

        private static async Task DrawMods(Image<Rgba32> scoreimg, ScorePanelData data, RichTextOptions textOptions)
        {
            if (data.scoreInfo.Mods.Length == 0) return;

            textOptions.Font = TorusSemiBold.Get(50);
            var usernameMeasure = TextMeasurer.MeasureSize(data.scoreInfo.User!.Username, textOptions);
            textOptions.Font = TorusRegular.Get(36);
            var timeMeasure = TextMeasurer.MeasureSize(data.scoreInfo.EndedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"), textOptions);
            var modStartX = 90 + 198 + (int)Math.Max(usernameMeasure.Width, timeMeasure.Width);

            foreach (var mod in data.scoreInfo.Mods)
            {
                var path = $"./work/mods_v2/2x/{mod.Acronym}.png";
                if (!File.Exists(path)) continue;
                using var modicon = await Img.LoadAsync(path);
                modicon.Mutate(x => x.Resize(90, 90));
                scoreimg.Mutate(x => x.DrawImage(modicon, new Point(modStartX, 573), 1));
                modStartX += 110;
            }
        }

        private static void DrawMainPP(Image<Rgba32> scoreimg, ScorePanelData data, RichTextOptions textOptions)
        {
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Font = TorusSemiBold.Get(80);
            textOptions.Origin = new PointF(2745, 655);
            scoreimg.Mutate(x => x.DrawText(textOptions, "pp", Color.ParseHex("#cf93ae")));
            var ppMeasure = TextMeasurer.MeasureSize("pp", textOptions);
            textOptions.Origin = new PointF(2745 - ppMeasure.Width, 655);
            scoreimg.Mutate(x => x.DrawText(textOptions, ((int)data.ppInfo.ppStat.total).ToString(), Color.ParseHex("#fc65a9")));
        }

        private static void DrawLengthGraph(Image<Rgba32> scoreimg, ScorePanelData data, RichTextOptions textOptions)
        {
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Font = TorusRegular.Get(30);
            textOptions.Origin = new PointF(2750, 747);

            var lengthText = Utils.Duration2TimeStringForScoreV3(data.scoreInfo.Beatmap!.TotalLength);
            var lengthMeasure = TextMeasurer.MeasureSize(lengthText, textOptions);
            var graphLength = 2708;

            if (!data.scoreInfo.Passed && data.ppInfo.maxCombo != null)
            {
                double totalObjs = data.scoreInfo.Beatmap.CountCircles + data.scoreInfo.Beatmap.CountSliders + data.scoreInfo.Beatmap.CountSpinners;
                double hitObjs = data.scoreInfo.Mode == OSU.Mode.Mania
                    ? data.scoreInfo.Statistics.CountGeki + data.scoreInfo.Statistics.CountKatu +
                      data.scoreInfo.Statistics.CountOk + data.scoreInfo.Statistics.CountMiss +
                      data.scoreInfo.Statistics.CountMeh + data.scoreInfo.Statistics.CountGreat
                    : data.scoreInfo.Statistics.CountOk + data.scoreInfo.Statistics.CountMiss +
                      data.scoreInfo.Statistics.CountMeh + data.scoreInfo.Statistics.CountGreat;
                graphLength = (int)((2708.0 - lengthMeasure.Width - 60.0) * (hitObjs / totalObjs));
            }
            else if (!data.scoreInfo.Passed)
            {
                graphLength = 2708 - (int)lengthMeasure.Width - 60;
            }
            graphLength = Math.Max(85, graphLength);

            var graphColor = data.scoreInfo.Passed ? "#c5e8f7" : "#cc4e53";
            using var graphArea = new Image<Rgba32>(graphLength, 50);
            graphArea.Mutate(x => x.Fill(Color.ParseHex(graphColor)).RoundCorner(new Size(graphLength, 50), 26));
            scoreimg.Mutate(x => x.DrawImage(graphArea, new Point(70, 706), 1));

            // Length text (shadow + main)
            scoreimg.Mutate(x => x.DrawText(textOptions, lengthText, data.scoreInfo.Passed ? Color.ParseHex("#311314") : Color.ParseHex("#3d3d3d")));
            textOptions.Origin = new PointF(2750, 746);
            scoreimg.Mutate(x => x.DrawText(textOptions, lengthText, data.scoreInfo.Passed ? Color.ParseHex("#585858") : Color.ParseHex("#333333")));

            if (!data.scoreInfo.Passed)
            {
                textOptions.Font = TorusSemiBold.Get(80);
                textOptions.Origin = new PointF(graphLength + 120, 770);
                scoreimg.Mutate(x => x.DrawText(textOptions, "×", Color.ParseHex("#cc4e53")));
            }

            // Finish/Fail label
            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
            textOptions.Font = TorusRegular.Get(30);
            var label = data.scoreInfo.Passed ? "Finish" : "Fail";
            textOptions.Origin = new PointF(90, 747);
            scoreimg.Mutate(x => x.DrawText(textOptions, label, Color.ParseHex("#311314")));
            textOptions.Origin = new PointF(90, 746);
            scoreimg.Mutate(x => x.DrawText(textOptions, label, data.scoreInfo.Passed ? Color.ParseHex("#585858") : Color.ParseHex("#e6e6e6")));
        }

        private static void DrawPPDetails(Image<Rgba32> scoreimg, ScorePanelData data, RichTextOptions textOptions)
        {
            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
            textOptions.Font = TorusSemiBold.Get(50);
            const int ppY = 938;

            // aim / spd / acc
            DrawPPValue(scoreimg, textOptions, (int)data.ppInfo.ppStat.aim!, 2196, ppY,
                Color.ParseHex("#fc65a9"), Color.ParseHex("#cf93ae"));
            DrawPPValue(scoreimg, textOptions, (int)data.ppInfo.ppStat.speed!, 2401, ppY,
                Color.ParseHex("#fc65a9"), Color.ParseHex("#cf93ae"));
            DrawPPValue(scoreimg, textOptions, (int)data.ppInfo.ppStat.acc!, 2596, ppY,
                Color.ParseHex("#fc65a9"), Color.ParseHex("#cf93ae"));

            // Prediction pps (5 values)
            int predX = 108;
            for (int i = 0; i < 5; i++)
            {
                DrawPPValue(scoreimg, textOptions, (int)data.ppInfo.ppStats![4 - i].total, predX, ppY,
                    Color.ParseHex("#fc65a9"), Color.ParseHex("#cf93ae"));
                predX += 204;
            }

            // If-FC PP (shadow + main)
            textOptions.Font = TorusRegular.Get(36);
            var ifFcPp = (int)data.ppInfo.ppStats![5].total;
            DrawPPValue(scoreimg, textOptions, ifFcPp, 178, 831,
                Color.ParseHex("#3b3b3b"), Color.ParseHex("#3b3b3b"));
            DrawPPValue(scoreimg, textOptions, ifFcPp, 178, 830,
                Color.ParseHex("#fc65a9"), Color.ParseHex("#cf93ae"));
        }

        private static void DrawPPValue(Image<Rgba32> img, RichTextOptions textOptions, int ppValue, float x, float y, Color numColor, Color suffixColor)
        {
            var text = ppValue.ToString();
            textOptions.Origin = new PointF(x, y);
            img.Mutate(ctx => ctx.DrawText(textOptions, text, numColor));

            var measure = TextMeasurer.MeasureSize(text, textOptions);
            textOptions.Origin = new PointF(x + measure.Width, y);
            img.Mutate(ctx => ctx.DrawText(textOptions, "pp", suffixColor));
        }

        private static void DrawTextWithShadow(Image<Rgba32> img, RichTextOptions textOptions, string text, Color mainColor, Color shadowColor, float x, float shadowY, float mainY)
        {
            textOptions.Origin = new PointF(x, shadowY);
            img.Mutate(ctx => ctx.DrawText(textOptions, text, shadowColor));
            textOptions.Origin = new PointF(x, mainY);
            img.Mutate(ctx => ctx.DrawText(textOptions, text, mainColor));
        }
    }
}
