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
        using var panel = data.mode switch
        {
            RosuPP.Mode.Catch
                => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2-fruits.png"),
            RosuPP.Mode.Mania
                => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2-mania.png"),
            _ => await Img.LoadAsync("work/legacy/v2_scorepanel/default-score-v2.png")
        };

        using var avatar = await Utils.LoadOrDownloadAvatar(data.scoreInfo.User!);

        // bg
        var bg = await Utils.LoadOrDownloadBackground(data.scoreInfo.Beatmap!.BeatmapsetId, data.scoreInfo.Beatmap.BeatmapId);
        bg ??= await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
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
            x.DrawText(textOptions,  title, Color.Black)
        );
        textOptions.Origin = new PointF(499, 105);
        score.Mutate(x =>
            x.DrawText(textOptions,  title, Color.White)
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
            x.DrawText(textOptions,  artist, Color.Black)
        );
        textOptions.Origin = new PointF(519, 175);
        score.Mutate(x =>
            x.DrawText(textOptions,  artist, Color.White)
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
            x.DrawText(textOptions,  creator, Color.Black)
        );
        textOptions.Origin = new PointF(795, 175);
        score.Mutate(x =>
            x.DrawText(textOptions,  creator, Color.White)
        );
        // beatmap_id
        var beatmap_id = data.scoreInfo.Beatmap.BeatmapId.ToString();
        textOptions.Origin = new PointF(1008, 178);
        score.Mutate(x =>
            x.DrawText(textOptions,  beatmap_id, Color.Black)
        );
        textOptions.Origin = new PointF(1008, 175);
        score.Mutate(x =>
            x.DrawText(textOptions,  beatmap_id, Color.White)
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
            x.DrawText(textOptions,  song_time, Color.Black)
        );
        textOptions.Origin = new PointF(1741, 124);
        score.Mutate(x =>
            x.DrawText(textOptions,  song_time, color)
        );
        // bpm
        var bpm = data.ppInfo.bpm.ToString("0.##");
        textOptions.Origin = new PointF(1457, 127);
        score.Mutate(x =>
            x.DrawText(textOptions,  bpm, Color.Black)
        );
        textOptions.Origin = new PointF(1457, 124);
        score.Mutate(x => x.DrawText(textOptions,  bpm, color));
        // ar
        var ar = ppInfo.AR.ToString("0.0#");
        textOptions.Origin = new PointF(1457, 218);
        score.Mutate(x =>
            x.DrawText(textOptions,  ar, Color.Black)
        );
        textOptions.Origin = new PointF(1457, 215);
        score.Mutate(x => x.DrawText(textOptions,  ar, color));
        // od
        var od = ppInfo.OD.ToString("0.0#");
        textOptions.Origin = new PointF(1741, 218);
        score.Mutate(x =>
            x.DrawText(textOptions,  od, Color.Black)
        );
        textOptions.Origin = new PointF(1741, 215);
        score.Mutate(x => x.DrawText(textOptions,  od, color));
        // cs
        var cs = ppInfo.CS.ToString("0.0#");
        textOptions.Origin = new PointF(1457, 312);
        score.Mutate(x =>
            x.DrawText(textOptions,  cs, Color.Black)
        );
        textOptions.Origin = new PointF(1457, 309);
        score.Mutate(x => x.DrawText(textOptions,  cs, color));
        // hp
        var hp = ppInfo.HP.ToString("0.0#");
        textOptions.Origin = new PointF(1741, 312);
        score.Mutate(x =>
            x.DrawText(textOptions,  hp, Color.Black)
        );
        textOptions.Origin = new PointF(1741, 309);
        score.Mutate(x => x.DrawText(textOptions,  hp, color));
        // stars, version
        var starText = $"Stars: {ppInfo.star:0.##}";
        textOptions.Origin = new PointF(584, 292);
        score.Mutate(x =>
            x.DrawText(textOptions,  starText, Color.Black)
        );
        textOptions.Origin = new PointF(584, 289);
        score.Mutate(x =>
            x.DrawText(textOptions,  starText, color)
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
            x.DrawText(textOptions,  version, Color.Black)
        );
        textOptions.Origin = new PointF(584, 317);
        score.Mutate(x =>
            x.DrawText(textOptions,  version, Color.White)
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
            x.DrawText(textOptions,  username, Color.Black)
        );
        textOptions.Origin = new PointF(145, 467);
        score.Mutate(x =>
            x.DrawText(textOptions,  username, Color.White)
        );
        // time
        textOptions.Font = TorusRegular.Get(27.61f);
        data.scoreInfo.EndedAt = data.scoreInfo.EndedAt.ToLocalTime(); //to UTC+8
        var time = data.scoreInfo.EndedAt.ToString("yyyy/MM/dd HH:mm:ss");
        textOptions.Origin = new PointF(145, 505);
        score.Mutate(x =>
            x.DrawText(textOptions,  time, Color.Black)
        );
        textOptions.Origin = new PointF(145, 502);
        score.Mutate(x =>
            x.DrawText(textOptions,  time, Color.White)
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
            x.DrawText(textOptions,  pptext, ppColor)
        );
        textOptions.Origin = new PointF(1532 + metric.Width, 638);
        score.Mutate(x =>
            x.DrawText(textOptions,  "pp", ppTColor)
        );
        if (ppInfo.ppStat.speed == null)
            pptext = "-";
        else
            pptext = ppInfo.ppStat.speed.Value.ToString("0");
        metric = TextMeasurer.MeasureSize(pptext, textOptions);
        textOptions.Origin = new PointF(1672, 638);
        score.Mutate(x =>
            x.DrawText(textOptions,  pptext, ppColor)
        );
        textOptions.Origin = new PointF(1672 + metric.Width, 638);
        score.Mutate(x =>
            x.DrawText(textOptions,  "pp", ppTColor)
        );
        if (ppInfo.ppStat.acc == null)
            pptext = "-";
        else
            pptext = ppInfo.ppStat.acc.Value.ToString("0");
        metric = TextMeasurer.MeasureSize(pptext, textOptions);
        textOptions.Origin = new PointF(1812, 638);
        score.Mutate(x =>
            x.DrawText(textOptions,  pptext, ppColor)
        );
        textOptions.Origin = new PointF(1812 + metric.Width, 638);
        score.Mutate(x =>
            x.DrawText(textOptions,  "pp", ppTColor)
        );

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
                x.DrawText(textOptions,  pptext, ppColor)
            );
            textOptions.Origin = new PointF(50 + 139 * i + metric.Width, 638);
            score.Mutate(x =>
                x.DrawText(textOptions,  "pp", ppTColor)
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
            x.DrawText(textOptions,  pptext, ppColor)
        );
        textOptions.Origin = new PointF(99 + metric.Width, 562);
        score.Mutate(x =>
            x.DrawText(textOptions,  "pp", ppTColor)
        );

        // total pp
        textOptions.Font = TorusRegular.Get(61f);

        pptext = Math.Round(ppInfo.ppStat.total).ToString("0");
        var ppm = TextMeasurer.MeasureSize(pptext, textOptions);

        textOptions.HorizontalAlignment = HorizontalAlignment.Right;
        textOptions.Origin = new PointF(1825, 500);
        score.Mutate(x =>
            x.DrawText(textOptions,  pptext, ppColor)
        );
        textOptions.Origin = new PointF(1899, 500);
        score.Mutate(x =>
            x.DrawText(textOptions,  "pp", ppTColor)
        );

        if (data.oldPP is not null)
        {
            pptext = Math.Round(data.oldPP.Value).ToString("0");
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Origin = new PointF(1825 - ppm.Width - 175, 500);
            score.Mutate(x =>
                x.DrawText(textOptions,  pptext, ppColor)
            );
            textOptions.Origin = new PointF(1899 - ppm.Width - 100, 500);
            score.Mutate(x =>
                x.DrawText(textOptions,  "pp →", ppTColor)
            );
            textOptions.Font = TorusRegular.Get(20);
            textOptions.Origin = new PointF(1825 - ppm.Width - 150, 450);
            score.Mutate(x =>
                x.DrawText(textOptions,  "(oldpp)", ppTColor)
            );
        }

        // score
        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
        textOptions.Font = TorusRegular.Get(40);
        textOptions.Origin = new PointF(980, 745);
        score.Mutate(x =>
            x.DrawText(textOptions, data.scoreInfo.ScoreAuto.ToString("N0"), Color.White)
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
                x.DrawText(textOptions,  great, Color.Black)
            );
            textOptions.Origin = new PointF(790, 849);
            score.Mutate(x =>
                x.DrawText(textOptions,  great, Color.White)
            );
            // ok
            textOptions.Origin = new PointF(790, 975);
            score.Mutate(x =>
                x.DrawText(textOptions,  ok, Color.Black)
            );
            textOptions.Origin = new PointF(790, 972);
            score.Mutate(x =>
                x.DrawText(textOptions,  ok, Color.White)
            );
            // meh
            textOptions.Origin = new PointF(1152, 852);
            score.Mutate(x =>
                x.DrawText(textOptions,  meh, Color.Black)
            );
            textOptions.Origin = new PointF(1152, 849);
            score.Mutate(x =>
                x.DrawText(textOptions,  meh, Color.White)
            );
            // miss
            textOptions.Origin = new PointF(1152, 975);
            score.Mutate(x =>
                x.DrawText(textOptions,  miss, Color.Black)
            );
            textOptions.Origin = new PointF(1152, 972);
            score.Mutate(x =>
                x.DrawText(textOptions,  miss, Color.White)
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
                x.DrawText(textOptions,  great, Color.Black)
            );
            textOptions.Origin = new PointF(790, 832);
            score.Mutate(x =>
                x.DrawText(textOptions,  great, Color.White)
            );
            // geki
            textOptions.Origin = new PointF(1156, 836);
            score.Mutate(x =>
                x.DrawText(textOptions,  geki, Color.Black)
            );
            textOptions.Origin = new PointF(1156, 834);
            score.Mutate(x =>
                x.DrawText(textOptions,  geki, Color.White)
            );
            // katu
            textOptions.Origin = new PointF(790, 909);
            score.Mutate(x =>
                x.DrawText(textOptions,  katu, Color.Black)
            );
            textOptions.Origin = new PointF(790, 907);
            score.Mutate(x =>
                x.DrawText(textOptions,  katu, Color.White)
            );
            // ok
            textOptions.Origin = new PointF(1156, 909);
            score.Mutate(x =>
                x.DrawText(textOptions,  ok, Color.Black)
            );
            textOptions.Origin = new PointF(1156, 907);
            score.Mutate(x =>
                x.DrawText(textOptions,  ok, Color.White)
            );
            // meh
            textOptions.Origin = new PointF(790, 984);
            score.Mutate(x =>
                x.DrawText(textOptions,  meh, Color.Black)
            );
            textOptions.Origin = new PointF(790, 982);
            score.Mutate(x =>
                x.DrawText(textOptions,  meh, Color.White)
            );
            // miss
            textOptions.Origin = new PointF(1156, 984);
            score.Mutate(x =>
                x.DrawText(textOptions,  miss, Color.Black)
            );
            textOptions.Origin = new PointF(1156, 982);
            score.Mutate(x =>
                x.DrawText(textOptions,  miss, Color.White)
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
                x.DrawText(textOptions,  great, Color.Black)
            );
            textOptions.Origin = new PointF(792, 854);
            score.Mutate(x =>
                x.DrawText(textOptions,  great, Color.White)
            );
            // ok
            textOptions.Origin = new PointF(792, 985);
            score.Mutate(x =>
                x.DrawText(textOptions,  ok, Color.Black)
            );
            textOptions.Origin = new PointF(792, 982);
            score.Mutate(x =>
                x.DrawText(textOptions,  ok, Color.White)
            );
            // meh
            textOptions.Origin = new PointF(1154, 857);
            score.Mutate(x =>
                x.DrawText(textOptions,  meh, Color.Black)
            );
            textOptions.Origin = new PointF(1154, 854);
            score.Mutate(x =>
                x.DrawText(textOptions,  meh, Color.White)
            );
            // miss
            textOptions.Origin = new PointF(1154, 985);
            score.Mutate(x =>
                x.DrawText(textOptions,  miss, Color.Black)
            );
            textOptions.Origin = new PointF(1154, 982);
            score.Mutate(x =>
                x.DrawText(textOptions,  miss, Color.White)
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
            x.DrawText(textOptions,  $"{acc:0.0#}%", Color.Black)
        );
        using var acchue = new Image<Rgba32>(1950 - 2, 1088);
        var hue = acc < 60 ? 260f : (acc - 60) * 2 + 280f;
        textOptions.Origin = new PointF(360, 963);
        acchue.Mutate(x =>
            x.DrawText(textOptions,  $"{acc:0.0#}%", color)
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
                    x.DrawText(textOptions,  " / ", Color.Black)
                );
                textOptions.Origin = new PointF(1598, 963);
                score.Mutate(x =>
                    x.DrawText(textOptions,  " / ", Color.White)
                );
                textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                textOptions.Origin = new PointF(1607, 966);
                score.Mutate(x =>
                    x.DrawText(textOptions, $"{maxCombo}x", Color.Black)
                );
                textOptions.Origin = new PointF(1607, 963);
                score.Mutate(x =>
                    x.DrawText(textOptions, $"{maxCombo}x", color)
                );
                textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                textOptions.Origin = new PointF(1588, 966);
                score.Mutate(x =>
                    x.DrawText(textOptions, $"{combo}x", Color.Black)
                );
                using var combohue = new Image<Rgba32>(1950 - 2, 1088);
                hue = (((float)combo / (float)maxCombo) * 100) + 260;
                textOptions.Origin = new PointF(1588, 963);
                combohue.Mutate(x =>
                    x.DrawText(textOptions,  $"{combo}x", color)
                );
                combohue.Mutate(x => x.Hue(((float)hue)));
                score.Mutate(x => x.DrawImage(combohue, 1));
            }
            else
            {
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                textOptions.Origin = new PointF(1598, 966);
                score.Mutate(x =>
                    x.DrawText(textOptions, $"{combo}x", Color.Black)
                );
                textOptions.Origin = new PointF(1598, 963);
                score.Mutate(x =>
                    x.DrawText(textOptions,  $"{combo}x", color)
                );
            }
        }
        else
        {
            textOptions.HorizontalAlignment = HorizontalAlignment.Center;
            textOptions.Origin = new PointF(1598, 966);
            score.Mutate(x =>
                x.DrawText(textOptions,  $"{combo}x", Color.Black)
            );
            textOptions.Origin = new PointF(1598, 963);
            score.Mutate(x =>
                x.DrawText(textOptions,  $"{combo}x", color)
            );
        }

        // 不知道为啥更新了imagesharp后对比度(亮度)变了
        score.Mutate(x => x.Brightness(0.998f));

        return score;
    }
}
