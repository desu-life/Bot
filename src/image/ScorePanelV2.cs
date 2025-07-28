#pragma warning disable CS8618 // 非null 字段未初始化

namespace KanonBot.Image;

using System.IO;
using KanonBot.Image.Components;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static KanonBot.Image.Fonts;
using Img = SixLabors.ImageSharp.Image;
using OSU = KanonBot.API.OSU;

public static class ScoreV2
{
    public class ScorePanelData
    {
        public OsuPerformance.PPInfo ppInfo;
        public OSU.Models.ScoreLazer scoreInfo;
        public RosuPP.Mode mode;
        public string server;
        public double? oldPP;
        public double? playtime;
    }

    public static async Task<Img> DrawScore(ScorePanelData data)
    {
        var ppInfo = data.ppInfo;
        var score = new Image<Rgba32>(1950, 1088);
        // 先下载必要文件
        var bgPath = $"./work/background/{data.scoreInfo.Beatmap!.BeatmapId}.png";
        if (!File.Exists(bgPath))
        {
            bgPath = null;
            try
            {
                bgPath = await OSU.Client.SayoDownloadBeatmapBackgroundImg(
                    data.scoreInfo.Beatmap.BeatmapsetId,
                    data.scoreInfo.Beatmap.BeatmapId,
                    "./work/background/"
                );
            }
            catch (Exception ex)
            {
                var msg = $"从Sayo API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                Log.Warning(msg);
            }

            if (bgPath is null)
            {
                try
                {
                    bgPath = await OSU.Client.DownloadBeatmapBackgroundImg(
                        data.scoreInfo.Beatmap.BeatmapsetId,
                        "./work/background/",
                        $"{data.scoreInfo.Beatmap!.BeatmapId}.png"
                    );
                }
                catch (Exception ex)
                {
                    var msg = $"从OSU API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                    Log.Warning(msg);
                }
            }
        }

        using var avatar = await Utils.LoadOrDownloadAvatar(data.scoreInfo.User!);

        using var panel = data.mode switch
        {
            RosuPP.Mode.Catch
                => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2-fruits.png"),
            RosuPP.Mode.Mania
                => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2-mania.png"),
            _ => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2.png")
        };

        // bg
        Image<Rgba32> bg;
        if (bgPath is null)
        {
            bg = await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
        }
        else
        {
            try
            {
                bg = await Img.LoadAsync<Rgba32>(bgPath);
            }
            catch
            {
                bg = await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
                try
                {
                    File.Delete(bgPath);
                }
                catch { }
            }
        }
        using var smallBg = bg.Clone(x => x.RoundCorner(new Size(433, 296), 20));
        using var backBlack = new Image<Rgba32>(1950 - 2, 1088);
        backBlack.Mutate(x =>
            x.BackgroundColor(Color.Black).RoundCorner(new Size(1950 - 2, 1088), 20)
        );
        bg.Mutate(x => x.GaussianBlur(5).RoundCorner(new Size(1950 - 2, 1088), 20));
        score.Mutate(x => x.DrawImage(bg, 1));
        score.Mutate(x => x.DrawImage(backBlack, 0.33f));

        if (data.scoreInfo.IsLazer)
        {
            var blurpanel = panel.Clone(x => x.GaussianBlur(3));
            score.Mutate(x => x.DrawImage(blurpanel, 0.3f));
        }

        score.Mutate(x => x.DrawImage(panel, 1));
        score.Mutate(x => x.DrawImage(smallBg, new Point(27, 34), 1));

        if (!string.IsNullOrWhiteSpace(data.server))
        {
            score.Mutate(x =>
                x.DrawText(
                    new DrawingOptions
                    {
                        GraphicsOptions = new GraphicsOptions
                        {
                            Antialias = true,
                            ColorBlendingMode = PixelColorBlendingMode.Lighten
                        }
                    },
                    new RichTextOptions(FredokaBold.Get(60, FontStyle.Bold))
                    {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Origin = new PointF(1913, 1061)
                    },
                    data.server,
                    new SolidBrush(Color.Transparent),
                    new SolidPen(Color.FromRgba(0x5f, 0x5f, 0x5f, 0xaa), 3)
                )
            );
        }

        bg.Dispose();

        using var diffCircle = await DifficultyRing.Draw(data.mode, ppInfo.star);
        diffCircle.Mutate(x => x.Resize(65, 65));
        score.Mutate(x => x.DrawImage(diffCircle, new Point(512, 257), 1));
        // beatmap_status
        if (data.scoreInfo.Beatmap.Status is OSU.Models.Status.Ranked)
        {
            using var c = await Img.LoadAsync("./work/icons/ranked.png");
            score.Mutate(x => x.DrawImage(c, new Point(415, 16), 1));
        }
        if (data.scoreInfo.Beatmap.Status is OSU.Models.Status.Approved)
        {
            using var c = await Img.LoadAsync("./work/icons/approved.png");
            score.Mutate(x => x.DrawImage(c, new Point(415, 16), 1));
        }
        if (data.scoreInfo.Beatmap.Status is OSU.Models.Status.Loved)
        {
            using var c = await Img.LoadAsync("./work/icons/loved.png");
            score.Mutate(x => x.DrawImage(c, new Point(415, 16), 1));
        }
        // mods
        var me = data.scoreInfo.Mods.AsEnumerable();
        // mods.Sort((a, b) => a.IsSpeedChangeMod ? 1 : -1);

        // 筛选classic成绩
        if (data.scoreInfo.IsClassic)
        {
            me = me.Filter(x => !x.IsClassic);
        }

        var mods = me.ToList();
        var modp = 0;
        if (mods.Count > 7)
        {
            foreach (var mod in mods)
            {
                using var modPic = await OsuModIcon.DrawV2(mod, data.scoreInfo.IsLazer);
                if (modPic is null)
                    continue;
                modPic.Mutate(x => x.Resize(200, 0));
                score.Mutate(x => x.DrawImage(modPic, new Point(modp + 440, 440), 1));
                modp += 120;
            }
        }
        else
        {
            foreach (var mod in mods)
            {
                using var modPic = await OsuModIcon.DrawV2(mod, data.scoreInfo.IsLazer);
                if (modPic is null)
                    continue;
                modPic.Mutate(x => x.Resize(200, 0));
                score.Mutate(x => x.DrawImage(modPic, new Point(modp + 440, 440), 1));
                modp += 160;
            }
        }
        // rankings
        var ranking = data.scoreInfo.Passed ? data.scoreInfo.RankAuto : "F";
        using var rankPic = await Img.LoadAsync($"./work/ranking/ranking-{ranking}.png");

        if (data.scoreInfo.IsLazer)
        {
            var blurrank = rankPic.Clone(x =>
                x.Resize(new ResizeOptions { Size = new Size(300, 300), Mode = ResizeMode.BoxPad })
                    .GaussianBlur(40)
            );
            score.Mutate(x =>
                x.DrawImage(blurrank, new Point(913 - 150 + 62, 874 - 150 + 31), 0.8f)
            );
        }

        score.Mutate(x => x.DrawImage(rankPic, new Point(913, 874), 1));
        // text part (文字部分)
        var font = TorusRegular.Get(60);
        var drawOptions = new DrawingOptions
        {
            GraphicsOptions = new GraphicsOptions { Antialias = true }
        };
        var textOptions = new RichTextOptions(font)
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            FallbackFontFamilies = [HarmonySans, HarmonySansArabic]
        };
        // beatmap_info
        var title = "";
        foreach (char c in data.scoreInfo.Beatmapset!.Title)
        {
            title += c;
            var m = TextMeasurer.MeasureSize(title, textOptions);
            if (m.Width > 725)
            {
                title += "...";
                break;
            }
        }
        textOptions.Origin = new PointF(499, 110);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, title, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(499, 105);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, title, new SolidBrush(Color.White), null)
        );
        // artist
        textOptions.Font = TorusRegular.Get(40);
        var artist = "";
        foreach (char c in data.scoreInfo.Beatmapset.Artist)
        {
            artist += c;
            var m = TextMeasurer.MeasureSize(artist, textOptions);
            if (m.Width > 205)
            {
                artist += "...";
                break;
            }
        }
        textOptions.Origin = new PointF(519, 178);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, artist, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(519, 175);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, artist, new SolidBrush(Color.White), null)
        );
        // creator
        var creator = "";
        foreach (char c in data.scoreInfo.Beatmapset.Creator)
        {
            creator += c;
            var m = TextMeasurer.MeasureSize(creator, textOptions);
            if (m.Width > 145)
            {
                creator += "...";
                break;
            }
        }
        textOptions.Origin = new PointF(795, 178);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, creator, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(795, 175);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, creator, new SolidBrush(Color.White), null)
        );
        // beatmap_id
        var beatmap_id = data.scoreInfo.Beatmap.BeatmapId.ToString();
        textOptions.Origin = new PointF(1008, 178);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, beatmap_id, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(1008, 175);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, beatmap_id, new SolidBrush(Color.White), null)
        );
        // ar,od info
        var color = Color.ParseHex("#f1ce59");
        textOptions.Font = TorusRegular.Get(24.25f);
        // time
        var song_time = Utils.Duration2TimeString(
            (long)Math.Round(data.scoreInfo.Beatmap.TotalLength / data.ppInfo.clockrate)
        );
        if (data.playtime is not null)
        {
            var pt = Utils.Duration2TimeString(
                (long)Math.Round(data.playtime.Value / data.ppInfo.clockrate)
            );

            song_time = $"{pt} / {song_time}";
        }
        textOptions.Origin = new PointF(1741, 127);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, song_time, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(1741, 124);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, song_time, new SolidBrush(color), null)
        );
        // bpm
        var bpm = data.ppInfo.bpm.ToString("0.##");
        textOptions.Origin = new PointF(1457, 127);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, bpm, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(1457, 124);
        score.Mutate(x => x.DrawText(drawOptions, textOptions, bpm, new SolidBrush(color), null));
        // ar
        var ar = ppInfo.AR.ToString("0.0#");
        textOptions.Origin = new PointF(1457, 218);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, ar, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(1457, 215);
        score.Mutate(x => x.DrawText(drawOptions, textOptions, ar, new SolidBrush(color), null));
        // od
        var od = ppInfo.OD.ToString("0.0#");
        textOptions.Origin = new PointF(1741, 218);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, od, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(1741, 215);
        score.Mutate(x => x.DrawText(drawOptions, textOptions, od, new SolidBrush(color), null));
        // cs
        var cs = ppInfo.CS.ToString("0.0#");
        textOptions.Origin = new PointF(1457, 312);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, cs, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(1457, 309);
        score.Mutate(x => x.DrawText(drawOptions, textOptions, cs, new SolidBrush(color), null));
        // hp
        var hp = ppInfo.HP.ToString("0.0#");
        textOptions.Origin = new PointF(1741, 312);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, hp, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(1741, 309);
        score.Mutate(x => x.DrawText(drawOptions, textOptions, hp, new SolidBrush(color), null));
        // stars, version
        var starText = $"Stars: {ppInfo.star:0.##}";
        textOptions.Origin = new PointF(584, 292);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, starText, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(584, 289);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, starText, new SolidBrush(color), null)
        );
        var version = "";
        foreach (char c in data.scoreInfo.Beatmap.Version)
        {
            version += c;
            var m = TextMeasurer.MeasureSize(version, textOptions);
            if (m.Width > 140)
            {
                version += "...";
                break;
            }
        }
        textOptions.Origin = new PointF(584, 320);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, version, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(584, 317);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, version, new SolidBrush(Color.White), null)
        );
        // avatar
        avatar.Mutate(x => x.Resize(80, 80).RoundCorner(new Size(80, 80), 40));
        score.Mutate(x => x.Fill(Color.White, new EllipsePolygon(80, 465, 85, 85)));
        score.Mutate(x => x.DrawImage(avatar, new Point(40, 425), 1));
        // username
        textOptions.Font = TorusSemiBold.Get(36);
        var username = data.scoreInfo.User!.Username;
        textOptions.Origin = new PointF(145, 470);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, username, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(145, 467);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, username, new SolidBrush(Color.White), null)
        );
        // time
        textOptions.Font = TorusRegular.Get(27.61f);
        data.scoreInfo.EndedAt = data.scoreInfo.EndedAt.ToLocalTime(); //to UTC+8
        var time = data.scoreInfo.EndedAt.ToString("yyyy/MM/dd HH:mm:ss");
        textOptions.Origin = new PointF(145, 505);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, time, new SolidBrush(Color.Black), null)
        );
        textOptions.Origin = new PointF(145, 502);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, time, new SolidBrush(Color.White), null)
        );

        // pp
        var ppTColor = Color.ParseHex("#cf93ae");
        var ppColor = Color.ParseHex("#fc65a9");
        textOptions.Font = TorusRegular.Get(33.5f);
        // aim, speed
        string pptext;
        if (ppInfo.ppStat.aim == null)
            pptext = "-";
        else
            pptext = ppInfo.ppStat.aim.Value.ToString("0");
        var metric = TextMeasurer.MeasureSize(pptext, textOptions);
        textOptions.Origin = new PointF(1532, 638);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
        );
        textOptions.Origin = new PointF(1532 + metric.Width, 638);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
        );
        if (ppInfo.ppStat.speed == null)
            pptext = "-";
        else
            pptext = ppInfo.ppStat.speed.Value.ToString("0");
        metric = TextMeasurer.MeasureSize(pptext, textOptions);
        textOptions.Origin = new PointF(1672, 638);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
        );
        textOptions.Origin = new PointF(1672 + metric.Width, 638);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
        );
        if (ppInfo.ppStat.acc == null)
            pptext = "-";
        else
            pptext = ppInfo.ppStat.acc.Value.ToString("0");
        metric = TextMeasurer.MeasureSize(pptext, textOptions);
        textOptions.Origin = new PointF(1812, 638);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
        );
        textOptions.Origin = new PointF(1812 + metric.Width, 638);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
        );

        // if (data.scoreInfo.Mode is OSU.Mode.Mania)
        // {
        //     pptext = "-";
        //     metric = TextMeasurer.MeasureSize(pptext, textOptions);
        //     for (var i = 0; i < 5; i++)
        //     {
        //         textOptions.Origin = new PointF(50 + 139 * i, 638);
        //         score.Mutate(x => x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null));
        //         textOptions.Origin = new PointF(50 + 139 * i + metric.Width, 638);
        //         score.Mutate(x => x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null));
        //     }
        // }
        // else
        // {
        // }
        // 这边不再需要匹配mania模式
        for (var i = 0; i < 5; i++)
        {
            try
            {
                pptext = ppInfo.ppStats![5 - (i + 1)].total.ToString("0");
            }
            catch
            {
                pptext = "-";
            }
            metric = TextMeasurer.MeasureSize(pptext, textOptions);
            textOptions.Origin = new PointF(50 + 139 * i, 638);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
            );
            textOptions.Origin = new PointF(50 + 139 * i + metric.Width, 638);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
            );
        }

        // if fc
        textOptions.Font = TorusRegular.Get(24.5f);
        try
        {
            pptext = ppInfo.ppStats![5].total.ToString("0");
        }
        catch
        {
            pptext = "-";
        }
        metric = TextMeasurer.MeasureSize(pptext, textOptions);
        textOptions.Origin = new PointF(99, 562);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
        );
        textOptions.Origin = new PointF(99 + metric.Width, 562);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
        );

        // total pp
        textOptions.Font = TorusRegular.Get(61f);

        pptext = Math.Round(ppInfo.ppStat.total).ToString("0");
        var ppm = TextMeasurer.MeasureSize(pptext, textOptions);

        textOptions.HorizontalAlignment = HorizontalAlignment.Right;
        textOptions.Origin = new PointF(1825, 500);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
        );
        textOptions.Origin = new PointF(1899, 500);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, "pp", new SolidBrush(ppTColor), null)
        );

        if (data.oldPP is not null)
        {
            pptext = Math.Round(data.oldPP.Value).ToString("0");
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Origin = new PointF(1825 - ppm.Width - 175, 500);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, pptext, new SolidBrush(ppColor), null)
            );
            textOptions.Origin = new PointF(1899 - ppm.Width - 100, 500);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, "pp →", new SolidBrush(ppTColor), null)
            );
            textOptions.Font = TorusRegular.Get(20);
            textOptions.Origin = new PointF(1825 - ppm.Width - 150, 450);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, "(oldpp)", new SolidBrush(ppTColor), null)
            );
        }

        // score
        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
        textOptions.Font = TorusRegular.Get(40);
        textOptions.Origin = new PointF(980, 745);
        score.Mutate(x =>
            x.DrawText(
                drawOptions,
                textOptions,
                data.scoreInfo.ScoreAuto.ToString("N0"),
                new SolidBrush(Color.White),
                null
            )
        );

        if (data.mode is RosuPP.Mode.Catch)
        {
            textOptions.Font = TorusRegular.Get(40.00f);
            var statistics = data.scoreInfo.ConvertStatistics;
            var great = statistics.CountGreat.ToString();
            var ok = statistics.CountOk.ToString();
            var meh = statistics.CountMeh.ToString();
            var miss = statistics.CountMiss.ToString();

            // great
            textOptions.Origin = new PointF(790, 852);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(790, 849);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.White), null)
            );
            // ok
            textOptions.Origin = new PointF(790, 975);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(790, 972);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.White), null)
            );
            // meh
            textOptions.Origin = new PointF(1152, 852);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1152, 849);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.White), null)
            );
            // miss
            textOptions.Origin = new PointF(1152, 975);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1152, 972);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.White), null)
            );
        }
        else if (data.mode is RosuPP.Mode.Mania)
        {
            textOptions.Font = TorusRegular.Get(35.00f);
            var great = data.scoreInfo.Statistics.CountGreat.ToString();
            var ok = data.scoreInfo.Statistics.CountOk.ToString();
            var meh = data.scoreInfo.Statistics.CountMeh.ToString();
            var miss = data.scoreInfo.Statistics.CountMiss.ToString();
            var geki = data.scoreInfo.Statistics.CountGeki.ToString();
            var katu = data.scoreInfo.Statistics.CountKatu.ToString();

            // great
            textOptions.Origin = new PointF(790, 834);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(790, 832);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.White), null)
            );
            // geki
            textOptions.Origin = new PointF(1156, 836);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, geki, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1156, 834);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, geki, new SolidBrush(Color.White), null)
            );
            // katu
            textOptions.Origin = new PointF(790, 909);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, katu, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(790, 907);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, katu, new SolidBrush(Color.White), null)
            );
            // ok
            textOptions.Origin = new PointF(1156, 909);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1156, 907);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.White), null)
            );
            // meh
            textOptions.Origin = new PointF(790, 984);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(790, 982);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.White), null)
            );
            // miss
            textOptions.Origin = new PointF(1156, 984);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1156, 982);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.White), null)
            );
        }
        else
        {
            textOptions.Font = TorusRegular.Get(53.09f);
            var great = data.scoreInfo.Statistics.CountGreat.ToString();
            var ok = data.scoreInfo.Statistics.CountOk.ToString();
            var meh = data.scoreInfo.Statistics.CountMeh.ToString();
            var miss = data.scoreInfo.Statistics.CountMiss.ToString();

            // great
            textOptions.Origin = new PointF(792, 857);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(792, 854);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, great, new SolidBrush(Color.White), null)
            );
            // ok
            textOptions.Origin = new PointF(792, 985);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(792, 982);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, ok, new SolidBrush(Color.White), null)
            );
            // meh
            textOptions.Origin = new PointF(1154, 857);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1154, 854);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, meh, new SolidBrush(Color.White), null)
            );
            // miss
            textOptions.Origin = new PointF(1154, 985);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1154, 982);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, miss, new SolidBrush(Color.White), null)
            );
        }

        // acc
        textOptions.Font = TorusRegular.Get(53.56f);
        var acc = data.scoreInfo.AccAuto * 100f;
        var hsl = new Hsl(150, 1, 1);
        // ("#ffbd1f") idk?
        color = Color.ParseHex("#87ff6a");
        textOptions.Origin = new PointF(360, 966);
        score.Mutate(x =>
            x.DrawText(drawOptions, textOptions, $"{acc:0.0#}%", new SolidBrush(Color.Black), null)
        );
        using var acchue = new Image<Rgba32>(1950 - 2, 1088);
        var hue = acc < 60 ? 260f : (acc - 60) * 2 + 280f;
        textOptions.Origin = new PointF(360, 963);
        acchue.Mutate(x =>
            x.DrawText(drawOptions, textOptions, $"{acc:0.0#}%", new SolidBrush(color), null)
        );
        acchue.Mutate(x => x.Hue(((float)hue)));
        score.Mutate(x => x.DrawImage(acchue, 1));
        // combo
        var combo = data.scoreInfo.MaxCombo;
        if (ppInfo.maxCombo != null)
        {
            var maxCombo = ppInfo.maxCombo.Value;
            if (maxCombo > 0)
            {
                textOptions.Origin = new PointF(1598, 966);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, " / ", new SolidBrush(Color.Black), null)
                );
                textOptions.Origin = new PointF(1598, 963);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, " / ", new SolidBrush(Color.White), null)
                );
                textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                textOptions.Origin = new PointF(1607, 966);
                score.Mutate(x =>
                    x.DrawText(
                        drawOptions,
                        textOptions,
                        $"{maxCombo}x",
                        new SolidBrush(Color.Black),
                        null
                    )
                );
                textOptions.Origin = new PointF(1607, 963);
                score.Mutate(x =>
                    x.DrawText(
                        drawOptions,
                        textOptions,
                        $"{maxCombo}x",
                        new SolidBrush(color),
                        null
                    )
                );
                textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                textOptions.Origin = new PointF(1588, 966);
                score.Mutate(x =>
                    x.DrawText(
                        drawOptions,
                        textOptions,
                        $"{combo}x",
                        new SolidBrush(Color.Black),
                        null
                    )
                );
                using var combohue = new Image<Rgba32>(1950 - 2, 1088);
                hue = (((float)combo / (float)maxCombo) * 100) + 260;
                textOptions.Origin = new PointF(1588, 963);
                combohue.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, $"{combo}x", new SolidBrush(color), null)
                );
                combohue.Mutate(x => x.Hue(((float)hue)));
                score.Mutate(x => x.DrawImage(combohue, 1));
            }
            else
            {
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                textOptions.Origin = new PointF(1598, 966);
                score.Mutate(x =>
                    x.DrawText(
                        drawOptions,
                        textOptions,
                        $"{combo}x",
                        new SolidBrush(Color.Black),
                        null
                    )
                );
                textOptions.Origin = new PointF(1598, 963);
                score.Mutate(x =>
                    x.DrawText(drawOptions, textOptions, $"{combo}x", new SolidBrush(color), null)
                );
            }
        }
        else
        {
            textOptions.HorizontalAlignment = HorizontalAlignment.Center;
            textOptions.Origin = new PointF(1598, 966);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, $"{combo}x", new SolidBrush(Color.Black), null)
            );
            textOptions.Origin = new PointF(1598, 963);
            score.Mutate(x =>
                x.DrawText(drawOptions, textOptions, $"{combo}x", new SolidBrush(color), null)
            );
        }

        // 不知道为啥更新了imagesharp后对比度(亮度)变了
        score.Mutate(x => x.Brightness(0.998f));

        return score;
    }
}
