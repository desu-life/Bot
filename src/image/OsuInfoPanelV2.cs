using System.IO;
using System.Numerics;
using KanonBot.Functions.OSU;
using KanonBot.Image;
using KanonBot.Image.Components;
using KanonBot.OsuPerformance;
using RosuPP;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static KanonBot.API.OSU.OSUExtensions;
using static KanonBot.Image.Fonts;
using static KanonBot.Image.InfoV1;
using Img = SixLabors.ImageSharp.Image;
using OSU = KanonBot.API.OSU;
using ResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;

namespace KanonBot.Image;

//配色方案 0=用户自定义 1=模板light 2=模板dark 3...4...5...
//本来应该做个class的 算了 懒了 就这样吧 复制粘贴没什么不好的
public static class OsuInfoPanelV2
{
    public record struct InfoCustom(
        Color UsernameColor,
        Color ModeIconColor,
        Color RankColor,
        Color CountryRankColor,
        Color CountryRankDiffColor,
        Color CountryRankDiffIconColor,
        Color RankLineChartColor,
        Color RankLineChartTextColor,
        Color RankLineChartDotColor,
        Color RankLineChartDotStrokeColor,
        Color RankLineChartDashColor,
        Color RankLineChartDateTextColor,
        Color ppMainColor,
        Color ppDiffColor,
        Color ppDiffIconColor,
        Color ppProgressBarColorTextColor,
        Color ppProgressBarColor,
        Color ppProgressBarBackgroundColor,
        Color accMainColor,
        Color accDiffColor,
        Color accDiffIconColor,
        Color accProgressBarColorTextColor,
        Color accProgressBarColor,
        Color accProgressBarBackgroundColor,
        Color GradeStatisticsColor_XH,
        Color GradeStatisticsColor_X,
        Color GradeStatisticsColor_SH,
        Color GradeStatisticsColor_S,
        Color GradeStatisticsColor_A,
        Color Details_PlayTimeColor,
        Color Details_TotalHitsColor,
        Color Details_PlayCountColor,
        Color Details_RankedScoreColor,
        Color DetailsDiff_PlayTimeColor,
        Color DetailsDiff_TotalHitsColor,
        Color DetailsDiff_PlayCountColor,
        Color DetailsDiff_RankedScoreColor,
        Color DetailsDiff_PlayTimeIconColor,
        Color DetailsDiff_TotalHitsIconColor,
        Color DetailsDiff_PlayCountIconColor,
        Color DetailsDiff_RankedScoreIconColor,
        Color LevelTitleColor,
        Color LevelProgressBarColor,
        Color LevelProgressBarBackgroundColor,
        Color MainBPTitleColor,
        Color MainBPArtistColor,
        Color MainBPMapperColor,
        Color MainBPBIDColor,
        Color MainBPStarsColor,
        Color MainBPAccColor,
        Color MainBPRankColor,
        Color MainBPppMainColor,
        Color MainBPppTitleColor,
        Color SubBp2ndModeColor,
        Color SubBp2ndBPTitleColor,
        Color SubBp2ndBPVersionColor,
        Color SubBp2ndBPBIDColor,
        Color SubBp2ndBPStarsColor,
        Color SubBp2ndBPAccColor,
        Color SubBp2ndBPRankColor,
        Color SubBp2ndBPppMainColor,
        Color SubBp3rdModeColor,
        Color SubBp3rdBPTitleColor,
        Color SubBp3rdBPVersionColor,
        Color SubBp3rdBPBIDColor,
        Color SubBp3rdBPStarsColor,
        Color SubBp3rdBPAccColor,
        Color SubBp3rdBPRankColor,
        Color SubBp3rdBPppMainColor,
        Color SubBp4thModeColor,
        Color SubBp4thBPTitleColor,
        Color SubBp4thBPVersionColor,
        Color SubBp4thBPBIDColor,
        Color SubBp4thBPStarsColor,
        Color SubBp4thBPAccColor,
        Color SubBp4thBPRankColor,
        Color SubBp4thBPppMainColor,
        Color SubBp5thModeColor,
        Color SubBp5thBPTitleColor,
        Color SubBp5thBPVersionColor,
        Color SubBp5thBPBIDColor,
        Color SubBp5thBPStarsColor,
        Color SubBp5thBPAccColor,
        Color SubBp5thBPRankColor,
        Color SubBp5thBPppMainColor,
        Color footerColor,
        Color SubBpInfoSplitColor,
        float SideImgBrightness,
        float AvatarBrightness,
        float BadgeBrightness,
        float MainBPImgBrightness,
        float CountryFlagBrightness,
        float ModeCaptionBrightness,
        float ModIconBrightness,
        float ScoreModeIconBrightness,
        float OsuSupporterIconBrightness,
        float CountryFlagAlpha,
        float OsuSupporterIconAlpha,
        float BadgeAlpha,
        float AvatarAlpha,
        float ModIconAlpha,
        bool FixedScoreModeIconColor,
        bool DisplaySupporterStatus,
        bool ModeIconAlpha,
        bool Score1ModeIconAlpha,
        bool Score2ModeIconAlpha,
        bool Score3ModeIconAlpha,
        bool Score4ModeIconAlpha,
        bool DetailsPlaytimeIconAlpha,
        bool DetailsTotalHitIconAlpha,
        bool DetailsPlayCountIconAlpha,
        bool DetailsRankedScoreIconAlpha,
        bool ppDiffIconColorAlpha,
        bool accDiffIconColorAlpha,
        bool CountryRankDiffIconColorAlpha
    )
    {
        public static readonly InfoCustom DarkDefault =
            new(
                UsernameColor: Color.ParseHex("#e6e6e6"),
                RankColor: Color.ParseHex("#5872DF"),
                CountryRankColor: Color.ParseHex("#5872DF"),
                RankLineChartColor: Color.ParseHex("#2784ac"),
                RankLineChartTextColor: Color.ParseHex("#2784ac"),
                RankLineChartDotColor: Color.ParseHex("#2784ac"),
                RankLineChartDotStrokeColor: Color.ParseHex("#b2b5b7"),
                RankLineChartDashColor: Color.ParseHex("#e6e6e6"),
                RankLineChartDateTextColor: Color.ParseHex("#e6e6e6"),
                ppMainColor: Color.ParseHex("#e36a79"),
                ppProgressBarColorTextColor: Color.ParseHex("#FF7BAC"),
                ppProgressBarColor: Color.ParseHex("#5D3B3A"),
                ppProgressBarBackgroundColor: Color.ParseHex("#44312F"),
                accMainColor: Color.ParseHex("#6cac9c"),
                accProgressBarColorTextColor: Color.ParseHex("#00DE75"),
                accProgressBarColor: Color.ParseHex("#294B32"),
                accProgressBarBackgroundColor: Color.ParseHex("#243829"),
                GradeStatisticsColor_XH: Color.ParseHex("#6C91E0"),
                GradeStatisticsColor_X: Color.ParseHex("#6C91E0"),
                GradeStatisticsColor_SH: Color.ParseHex("#6C91E0"),
                GradeStatisticsColor_S: Color.ParseHex("#6C91E0"),
                GradeStatisticsColor_A: Color.ParseHex("#6C91E0"),
                Details_PlayTimeColor: Color.ParseHex("#e6e6e6"),
                Details_TotalHitsColor: Color.ParseHex("#e6e6e6"),
                Details_PlayCountColor: Color.ParseHex("#e6e6e6"),
                Details_RankedScoreColor: Color.ParseHex("#e6e6e6"),
                LevelTitleColor: Color.ParseHex("#e6e6e6"),
                LevelProgressBarColor: Color.ParseHex("#85485F"),
                LevelProgressBarBackgroundColor: Color.ParseHex("#000000"),
                MainBPTitleColor: Color.ParseHex("#e6e6e6"),
                MainBPArtistColor: Color.ParseHex("#e6e6e6"),
                MainBPMapperColor: Color.ParseHex("#e6e6e6"),
                MainBPBIDColor: Color.ParseHex("#e6e6e6"),
                MainBPStarsColor: Color.ParseHex("#e6e6e6"),
                MainBPAccColor: Color.ParseHex("#e6e6e6"),
                MainBPRankColor: Color.ParseHex("#e6e6e6"),
                MainBPppMainColor: Color.ParseHex("#5979bd"),
                MainBPppTitleColor: Color.ParseHex("#e6e6e6"),
                SubBp2ndBPTitleColor: Color.ParseHex("#e6e6e6"),
                SubBp2ndBPVersionColor: Color.ParseHex("#e6e6e6"),
                SubBp2ndBPBIDColor: Color.ParseHex("#e6e6e6"),
                SubBp2ndBPStarsColor: Color.ParseHex("#e6e6e6"),
                SubBp2ndBPAccColor: Color.ParseHex("#ffcd22"),
                SubBp2ndBPRankColor: Color.ParseHex("#e6e6e6"),
                SubBp2ndBPppMainColor: Color.ParseHex("#e36a79"),
                SubBp3rdBPTitleColor: Color.ParseHex("#e6e6e6"),
                SubBp3rdBPVersionColor: Color.ParseHex("#e6e6e6"),
                SubBp3rdBPBIDColor: Color.ParseHex("#e6e6e6"),
                SubBp3rdBPStarsColor: Color.ParseHex("#e6e6e6"),
                SubBp3rdBPAccColor: Color.ParseHex("#ffcd22"),
                SubBp3rdBPRankColor: Color.ParseHex("#e6e6e6"),
                SubBp3rdBPppMainColor: Color.ParseHex("#e36a79"),
                SubBp4thBPTitleColor: Color.ParseHex("#e6e6e6"),
                SubBp4thBPVersionColor: Color.ParseHex("#e6e6e6"),
                SubBp4thBPBIDColor: Color.ParseHex("#e6e6e6"),
                SubBp4thBPStarsColor: Color.ParseHex("#e6e6e6"),
                SubBp4thBPAccColor: Color.ParseHex("#ffcd22"),
                SubBp4thBPRankColor: Color.ParseHex("#e6e6e6"),
                SubBp4thBPppMainColor: Color.ParseHex("#e36a79"),
                SubBp5thBPTitleColor: Color.ParseHex("#e6e6e6"),
                SubBp5thBPVersionColor: Color.ParseHex("#e6e6e6"),
                SubBp5thBPBIDColor: Color.ParseHex("#e6e6e6"),
                SubBp5thBPStarsColor: Color.ParseHex("#e6e6e6"),
                SubBp5thBPAccColor: Color.ParseHex("#ffcd22"),
                SubBp5thBPRankColor: Color.ParseHex("#e6e6e6"),
                SubBp5thBPppMainColor: Color.ParseHex("#e36a79"),
                footerColor: Color.ParseHex("#e6e6e6"),
                SubBpInfoSplitColor: Color.ParseHex("#e6e6e6"),
                ModeIconColor: Color.ParseHex("#e6e6e6"),
                SubBp2ndModeColor: Color.White,
                SubBp3rdModeColor: Color.White,
                SubBp4thModeColor: Color.White,
                SubBp5thModeColor: Color.White,
                DetailsDiff_PlayTimeColor: Color.ParseHex("#e36a79"),
                DetailsDiff_PlayTimeIconColor: Color.ParseHex("#e36a79"),
                DetailsDiff_TotalHitsColor: Color.ParseHex("#6cac9c"),
                DetailsDiff_TotalHitsIconColor: Color.ParseHex("#6cac9c"),
                DetailsDiff_PlayCountColor: Color.ParseHex("#5872df"),
                DetailsDiff_PlayCountIconColor: Color.ParseHex("#5872df"),
                DetailsDiff_RankedScoreColor: Color.ParseHex("#3a4d78"),
                DetailsDiff_RankedScoreIconColor: Color.ParseHex("#3a4d78"),
                ppDiffColor: Color.ParseHex("#e36a79"),
                ppDiffIconColor: Color.ParseHex("#e36a79"),
                accDiffColor: Color.ParseHex("#6cac9c"),
                accDiffIconColor: Color.ParseHex("#6cac9c"),
                CountryRankDiffColor: Color.ParseHex("#e6e6e6"),
                CountryRankDiffIconColor: Color.ParseHex("#3a4d78"),
                SideImgBrightness: 0.6f,
                AvatarBrightness: 0.6f,
                BadgeBrightness: 0.6f,
                MainBPImgBrightness: 0.6f,
                CountryFlagBrightness: 0.6f,
                ModeCaptionBrightness: 0.6f,
                ModIconBrightness: 0.6f,
                ScoreModeIconBrightness: 0.6f,
                OsuSupporterIconBrightness: 0.6f,
                CountryFlagAlpha: 1.0f,
                OsuSupporterIconAlpha: 1.0f,
                BadgeAlpha: 1.0f,
                AvatarAlpha: 1.0f,
                ModIconAlpha: 1.0f,
                FixedScoreModeIconColor: false,
                DisplaySupporterStatus: true,
                ModeIconAlpha: false,
                Score1ModeIconAlpha: false,
                Score2ModeIconAlpha: false,
                Score3ModeIconAlpha: false,
                Score4ModeIconAlpha: false,
                DetailsPlaytimeIconAlpha: false,
                DetailsTotalHitIconAlpha: false,
                DetailsPlayCountIconAlpha: false,
                DetailsRankedScoreIconAlpha: false,
                ppDiffIconColorAlpha: false,
                accDiffIconColorAlpha: false,
                CountryRankDiffIconColorAlpha: false
            );
        public static readonly InfoCustom LightDefault =
            new(
                UsernameColor: Color.ParseHex("#4d4d4d"),
                RankColor: Color.ParseHex("#5872df"),
                CountryRankColor: Color.ParseHex("#5872df"),
                RankLineChartColor: Color.ParseHex("#80caea"),
                RankLineChartTextColor: Color.ParseHex("#80caea"),
                RankLineChartDotColor: Color.ParseHex("#80caea"),
                RankLineChartDotStrokeColor: Color.ParseHex("#eff1f3"),
                RankLineChartDashColor: Color.ParseHex("#dbe1e4"),
                RankLineChartDateTextColor: Color.ParseHex("#666666"),
                ppMainColor: Color.ParseHex("#e36a79"),
                ppProgressBarColorTextColor: Color.ParseHex("#d84356"),
                ppProgressBarColor: Color.ParseHex("#f7bebe"),
                ppProgressBarBackgroundColor: Color.ParseHex("#fddcd7"),
                accMainColor: Color.ParseHex("#6cac9c"),
                accProgressBarColorTextColor: Color.ParseHex("#006837"),
                accProgressBarColor: Color.ParseHex("#a4d8b1"),
                accProgressBarBackgroundColor: Color.ParseHex("#c3e7cb"),
                GradeStatisticsColor_XH: Color.ParseHex("#3a4d78"),
                GradeStatisticsColor_X: Color.ParseHex("#3a4d78"),
                GradeStatisticsColor_SH: Color.ParseHex("#3a4d78"),
                GradeStatisticsColor_S: Color.ParseHex("#3a4d78"),
                GradeStatisticsColor_A: Color.ParseHex("#3a4d78"),
                Details_PlayTimeColor: Color.ParseHex("#7f7f7f"),
                Details_TotalHitsColor: Color.ParseHex("#7f7f7f"),
                Details_PlayCountColor: Color.ParseHex("#7f7f7f"),
                Details_RankedScoreColor: Color.ParseHex("#7f7f7f"),
                LevelTitleColor: Color.ParseHex("#656b6d"),
                LevelProgressBarColor: Color.ParseHex("#f3b6cd"),
                LevelProgressBarBackgroundColor: Color.ParseHex("#e6e6e6"),
                MainBPTitleColor: Color.ParseHex("#656b6d"),
                MainBPArtistColor: Color.ParseHex("#656b6d"),
                MainBPMapperColor: Color.ParseHex("#656b6d"),
                MainBPBIDColor: Color.ParseHex("#656b6d"),
                MainBPStarsColor: Color.ParseHex("#656b6d"),
                MainBPAccColor: Color.ParseHex("#656b6d"),
                MainBPRankColor: Color.ParseHex("#656b6d"),
                MainBPppMainColor: Color.ParseHex("#364a75"),
                MainBPppTitleColor: Color.ParseHex("#656b6d"),
                SubBp2ndBPTitleColor: Color.ParseHex("#656b6d"),
                SubBp2ndBPVersionColor: Color.ParseHex("#656b6d"),
                SubBp2ndBPBIDColor: Color.ParseHex("#656b6d"),
                SubBp2ndBPStarsColor: Color.ParseHex("#656b6d"),
                SubBp2ndBPAccColor: Color.ParseHex("#ffcd22"),
                SubBp2ndBPRankColor: Color.ParseHex("#656b6d"),
                SubBp2ndBPppMainColor: Color.ParseHex("#ff7bac"),
                SubBp3rdBPTitleColor: Color.ParseHex("#656b6d"),
                SubBp3rdBPVersionColor: Color.ParseHex("#656b6d"),
                SubBp3rdBPBIDColor: Color.ParseHex("#656b6d"),
                SubBp3rdBPStarsColor: Color.ParseHex("#656b6d"),
                SubBp3rdBPAccColor: Color.ParseHex("#ffcd22"),
                SubBp3rdBPRankColor: Color.ParseHex("#656b6d"),
                SubBp3rdBPppMainColor: Color.ParseHex("#ff7bac"),
                SubBp4thBPTitleColor: Color.ParseHex("#656b6d"),
                SubBp4thBPVersionColor: Color.ParseHex("#656b6d"),
                SubBp4thBPBIDColor: Color.ParseHex("#656b6d"),
                SubBp4thBPStarsColor: Color.ParseHex("#656b6d"),
                SubBp4thBPAccColor: Color.ParseHex("#ffcd22"),
                SubBp4thBPRankColor: Color.ParseHex("#656b6d"),
                SubBp4thBPppMainColor: Color.ParseHex("#ff7bac"),
                SubBp5thBPTitleColor: Color.ParseHex("#656b6d"),
                SubBp5thBPVersionColor: Color.ParseHex("#656b6d"),
                SubBp5thBPBIDColor: Color.ParseHex("#656b6d"),
                SubBp5thBPStarsColor: Color.ParseHex("#656b6d"),
                SubBp5thBPAccColor: Color.ParseHex("#ffcd22"),
                SubBp5thBPRankColor: Color.ParseHex("#656b6d"),
                SubBp5thBPppMainColor: Color.ParseHex("#ff7bac"),
                footerColor: Color.ParseHex("#7f7f7f"),
                SubBpInfoSplitColor: Color.ParseHex("#656b6d"),
                ModeIconColor: Color.ParseHex("#7f7f7f"),
                SubBp2ndModeColor: Color.White,
                SubBp3rdModeColor: Color.White,
                SubBp4thModeColor: Color.White,
                SubBp5thModeColor: Color.White,
                DetailsDiff_PlayTimeColor: Color.ParseHex("#e36a79"),
                DetailsDiff_PlayTimeIconColor: Color.ParseHex("#e36a79"),
                DetailsDiff_TotalHitsColor: Color.ParseHex("#6cac9c"),
                DetailsDiff_TotalHitsIconColor: Color.ParseHex("#6cac9c"),
                DetailsDiff_PlayCountColor: Color.ParseHex("#5872df"),
                DetailsDiff_PlayCountIconColor: Color.ParseHex("#5872df"),
                DetailsDiff_RankedScoreColor: Color.ParseHex("#3a4d78"),
                DetailsDiff_RankedScoreIconColor: Color.ParseHex("#3a4d78"),
                ppDiffColor: Color.ParseHex("#e36a79"),
                ppDiffIconColor: Color.ParseHex("#e36a79"),
                accDiffColor: Color.ParseHex("#6cac9c"),
                accDiffIconColor: Color.ParseHex("#6cac9c"),
                CountryRankDiffColor: Color.ParseHex("#808080"),
                CountryRankDiffIconColor: Color.ParseHex("#3a4d78"),
                SideImgBrightness: 1.0f,
                AvatarBrightness: 1.0f,
                BadgeBrightness: 1.0f,
                MainBPImgBrightness: 1.0f,
                CountryFlagBrightness: 1.0f,
                ModeCaptionBrightness: 1.0f,
                ModIconBrightness: 1.0f,
                ScoreModeIconBrightness: 1.0f,
                OsuSupporterIconBrightness: 1.0f,
                CountryFlagAlpha: 1.0f,
                OsuSupporterIconAlpha: 1.0f,
                BadgeAlpha: 1.0f,
                AvatarAlpha: 1.0f,
                ModIconAlpha: 1.0f,
                FixedScoreModeIconColor: false,
                DisplaySupporterStatus: true,
                ModeIconAlpha: false,
                Score1ModeIconAlpha: false,
                Score2ModeIconAlpha: false,
                Score3ModeIconAlpha: false,
                Score4ModeIconAlpha: false,
                DetailsPlaytimeIconAlpha: false,
                DetailsTotalHitIconAlpha: false,
                DetailsPlayCountIconAlpha: false,
                DetailsRankedScoreIconAlpha: false,
                ppDiffIconColorAlpha: false,
                accDiffIconColorAlpha: false,
                CountryRankDiffIconColorAlpha: false
            );

        public static InfoCustom ParseColors(string raw, Option<InfoCustom> opOption)
        {
            var options = opOption.IfNone(InfoCustom.LightDefault);
            raw.Split('\n')
                .Map(arg => arg.Split(":").Map(s => s.Trim()).ToArray())
                .Iter(arg =>
                {
                    if (arg.Length != 2)
                        throw new ArgumentException("未知的格式");
                    switch (arg[0])
                    {
                        case "DetailsDiff_PlayTimeColor":
                            options.DetailsDiff_PlayTimeColor = Color.ParseHex(arg[1]);
                            break;
                        case "DetailsDiff_PlayTimeIconColor":
                            options.DetailsDiff_PlayTimeIconColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                                options.DetailsPlaytimeIconAlpha = true;
                            break;
                        case "DetailsDiff_TotalHitsColor":
                            options.DetailsDiff_TotalHitsColor = Color.ParseHex(arg[1]);
                            break;
                        case "DetailsDiff_TotalHitsIconColor":
                            options.DetailsDiff_TotalHitsIconColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                                options.DetailsTotalHitIconAlpha = true;
                            break;
                        case "DetailsDiff_PlayCountColor":
                            options.DetailsDiff_PlayCountColor = Color.ParseHex(arg[1]);
                            break;
                        case "DetailsDiff_PlayCountIconColor":
                            options.DetailsDiff_PlayCountIconColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                                options.DetailsPlayCountIconAlpha = true;
                            break;
                        case "DetailsDiff_RankedScoreColor":
                            options.DetailsDiff_RankedScoreColor = Color.ParseHex(arg[1]);
                            break;
                        case "DetailsDiff_RankedScoreIconColor":
                            options.DetailsDiff_RankedScoreIconColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                                options.DetailsRankedScoreIconAlpha = true;
                            break;
                        case "ppDiffColor":
                            options.ppDiffColor = Color.ParseHex(arg[1]);
                            break;
                        case "ppDiffIconColor":
                            options.ppDiffIconColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                                options.ppDiffIconColorAlpha = true;
                            break;
                        case "accDiffColor":
                            options.accDiffColor = Color.ParseHex(arg[1]);
                            break;
                        case "accDiffIconColor":
                            options.accDiffIconColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                                options.accDiffIconColorAlpha = true;
                            break;
                        case "CountryRankDiffColor":
                            options.CountryRankDiffColor = Color.ParseHex(arg[1]);
                            break;
                        case "CountryRankDiffIconColor":
                            options.CountryRankDiffIconColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                                options.CountryRankDiffIconColorAlpha = true;
                            break;
                        case "OsuSupporterIconBrightness":
                            options.OsuSupporterIconBrightness = float.Parse($"{arg[1]}");
                            break;
                        case "CountryFlagAlpha":
                            options.CountryFlagAlpha = float.Parse($"{arg[1]}");
                            break;
                        case "OsuSupporterIconAlpha":
                            options.OsuSupporterIconAlpha = float.Parse($"{arg[1]}");
                            break;
                        case "BadgeAlpha":
                            options.BadgeAlpha = float.Parse($"{arg[1]}");
                            break;
                        case "AvatarAlpha":
                            options.AvatarAlpha = float.Parse($"{arg[1]}");
                            break;
                        case "ModIconAlpha":
                            options.ModIconAlpha = float.Parse($"{arg[1]}");
                            break;
                        case "UsernameColor":
                            options.UsernameColor = Color.ParseHex(arg[1]);
                            break;
                        case "ModeIconColor":
                            options.ModeIconColor = Color.ParseHex(arg[1]);
                            if (arg[1].ToLower().Length > 7)
                            {
                                options.ModeIconAlpha = true;
                            }
                            break;
                        case "RankColor":
                            options.RankColor = Color.ParseHex(arg[1]);
                            break;
                        case "CountryRankColor":
                            options.CountryRankColor = Color.ParseHex(arg[1]);
                            break;
                        case "RankLineChartColor":
                            options.RankLineChartColor = Color.ParseHex(arg[1]);
                            break;
                        case "RankLineChartTextColor":
                            options.RankLineChartTextColor = Color.ParseHex(arg[1]);
                            break;
                        case "RankLineChartDotColor":
                            options.RankLineChartDotColor = Color.ParseHex(arg[1]);
                            break;
                        case "RankLineChartDotStrokeColor":
                            options.RankLineChartDotStrokeColor = Color.ParseHex(arg[1]);
                            break;
                        case "RankLineChartDashColor":
                            options.RankLineChartDashColor = Color.ParseHex(arg[1]);
                            break;
                        case "RankLineChartDateTextColor":
                            options.RankLineChartDateTextColor = Color.ParseHex(arg[1]);
                            break;
                        case "ppMainColor":
                            options.ppMainColor = Color.ParseHex(arg[1]);
                            break;
                        case "ppProgressBarColorTextColor":
                            options.ppProgressBarColorTextColor = Color.ParseHex(arg[1]);
                            break;
                        case "ppProgressBarColor":
                            options.ppProgressBarColor = Color.ParseHex(arg[1]);
                            break;
                        case "ppProgressBarBackgroundColor":
                            options.ppProgressBarBackgroundColor = Color.ParseHex(arg[1]);
                            break;
                        case "accMainColor":
                            options.accMainColor = Color.ParseHex(arg[1]);
                            break;
                        case "accProgressBarColorTextColor":
                            options.accProgressBarColorTextColor = Color.ParseHex(arg[1]);
                            break;
                        case "accProgressBarColor":
                            options.accProgressBarColor = Color.ParseHex(arg[1]);
                            break;
                        case "accProgressBarBackgroundColor":
                            options.accProgressBarBackgroundColor = Color.ParseHex(arg[1]);
                            break;
                        case "GradeStatisticsColor_XH":
                            options.GradeStatisticsColor_XH = Color.ParseHex(arg[1]);
                            break;
                        case "GradeStatisticsColor_X":
                            options.GradeStatisticsColor_X = Color.ParseHex(arg[1]);
                            break;
                        case "GradeStatisticsColor_SH":
                            options.GradeStatisticsColor_SH = Color.ParseHex(arg[1]);
                            break;
                        case "GradeStatisticsColor_S":
                            options.GradeStatisticsColor_S = Color.ParseHex(arg[1]);
                            break;
                        case "GradeStatisticsColor_A":
                            options.GradeStatisticsColor_A = Color.ParseHex(arg[1]);
                            break;
                        case "Details_PlayTimeColor":
                            options.Details_PlayTimeColor = Color.ParseHex(arg[1]);
                            break;
                        case "Details_TotalHitsColor":
                            options.Details_TotalHitsColor = Color.ParseHex(arg[1]);
                            break;
                        case "Details_PlayCountColor":
                            options.Details_PlayCountColor = Color.ParseHex(arg[1]);
                            break;
                        case "Details_RankedScoreColor":
                            options.Details_RankedScoreColor = Color.ParseHex(arg[1]);
                            break;
                        case "LevelTitleColor":
                            options.LevelTitleColor = Color.ParseHex(arg[1]);
                            break;
                        case "LevelProgressBarColor":
                            options.LevelProgressBarColor = Color.ParseHex(arg[1]);
                            break;
                        case "LevelProgressBarBackgroundColor":
                            options.LevelProgressBarBackgroundColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPTitleColor":
                            options.MainBPTitleColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPArtistColor":
                            options.MainBPArtistColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPMapperColor":
                            options.MainBPMapperColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPBIDColor":
                            options.MainBPBIDColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPStarsColor":
                            options.MainBPStarsColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPAccColor":
                            options.MainBPAccColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPRankColor":
                            options.MainBPRankColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPppMainColor":
                            options.MainBPppMainColor = Color.ParseHex(arg[1]);
                            break;
                        case "MainBPppTitleColor":
                            options.MainBPppTitleColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp2ndModeColor":
                            options.SubBp2ndModeColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                            {
                                options.Score1ModeIconAlpha = true;
                            }
                            break;
                        case "SubBp2ndBPTitleColor":
                            options.SubBp2ndBPTitleColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp2ndBPVersionColor":
                            options.SubBp2ndBPVersionColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp2ndBPBIDColor":
                            options.SubBp2ndBPBIDColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp2ndBPStarsColor":
                            options.SubBp2ndBPStarsColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp2ndBPAccColor":
                            options.SubBp2ndBPAccColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp2ndBPRankColor":
                            options.SubBp2ndBPRankColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp2ndBPppMainColor":
                            options.SubBp2ndBPppMainColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp3rdModeColor":
                            options.SubBp3rdModeColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                            {
                                options.Score2ModeIconAlpha = true;
                            }
                            break;
                        case "SubBp3rdBPTitleColor":
                            options.SubBp3rdBPTitleColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp3rdBPVersionColor":
                            options.SubBp3rdBPVersionColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp3rdBPBIDColor":
                            options.SubBp3rdBPBIDColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp3rdBPStarsColor":
                            options.SubBp3rdBPStarsColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp3rdBPAccColor":
                            options.SubBp3rdBPAccColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp3rdBPRankColor":
                            options.SubBp3rdBPRankColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp3rdBPppMainColor":
                            options.SubBp3rdBPppMainColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp4thModeColor":
                            options.SubBp4thModeColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                            {
                                options.Score3ModeIconAlpha = true;
                            }
                            break;
                        case "SubBp4thBPTitleColor":
                            options.SubBp4thBPTitleColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp4thBPVersionColor":
                            options.SubBp4thBPVersionColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp4thBPBIDColor":
                            options.SubBp4thBPBIDColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp4thBPStarsColor":
                            options.SubBp4thBPStarsColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp4thBPAccColor":
                            options.SubBp4thBPAccColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp4thBPRankColor":
                            options.SubBp4thBPRankColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp4thBPppMainColor":
                            options.SubBp4thBPppMainColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp5thModeColor":
                            options.SubBp5thModeColor = Color.ParseHex(arg[1]);
                            if (arg[1].Length > 7)
                            {
                                options.Score4ModeIconAlpha = true;
                            }
                            break;
                        case "SubBp5thBPTitleColor":
                            options.SubBp5thBPTitleColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp5thBPVersionColor":
                            options.SubBp5thBPVersionColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp5thBPBIDColor":
                            options.SubBp5thBPBIDColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp5thBPStarsColor":
                            options.SubBp5thBPStarsColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp5thBPAccColor":
                            options.SubBp5thBPAccColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp5thBPRankColor":
                            options.SubBp5thBPRankColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBp5thBPppMainColor":
                            options.SubBp5thBPppMainColor = Color.ParseHex(arg[1]);
                            break;
                        case "SubBpInfoSplitColor":
                            options.SubBpInfoSplitColor = Color.ParseHex(arg[1]);
                            break;
                        case "footerColor":
                            options.footerColor = Color.ParseHex(arg[1]);
                            break;
                        case "FixedScoreModeIconColor":
                            options.FixedScoreModeIconColor = bool.Parse(arg[1]);
                            break;
                        case "DisplaySupporterStatus":
                            options.DisplaySupporterStatus = bool.Parse(arg[1]);
                            break;
                        case "SideImgBrightness":
                            options.SideImgBrightness = float.Parse(arg[1]);
                            break;
                        case "AvatarBrightness":
                            options.AvatarBrightness = float.Parse(arg[1]);
                            break;
                        case "BadgeBrightness":
                            options.BadgeBrightness = float.Parse(arg[1]);
                            break;
                        case "MainBPImgBrightness":
                            options.MainBPImgBrightness = float.Parse(arg[1]);
                            break;
                        case "CountryFlagBrightness":
                            options.CountryFlagBrightness = float.Parse(arg[1]);
                            break;
                        case "ModeCaptionBrightness":
                            options.ModeCaptionBrightness = float.Parse(arg[1]);
                            break;
                        case "ModIconBrightness":
                            options.ModIconBrightness = float.Parse(arg[1]);
                            break;
                        case "ScoreModeIconBrightness":
                            options.ScoreModeIconBrightness = float.Parse(arg[1]);
                            break;
                        default:
                            throw new Exception("错误参数: " + arg[0]);
                    }
                });
            return options;
        }
    }

    public static async Task<Img> Draw(
        UserPanelData data,
        OSU.Models.ScoreLazer[] allBP,
        InfoCustom v2Options,
        bool isBonded = false,
        bool eventmode = false,
        bool isDataOfDayAvaiavle = true,
        bool output4k = false,
        CalculatorKind kind = CalculatorKind.Unset
    )
    {
        var ColorMode = data.customMode;
        (
            Color UsernameColor,
            Color ModeIconColor,
            Color RankColor,
            Color CountryRankColor,
            Color CountryRankDiffColor,
            Color CountryRankDiffIconColor,
            Color RankLineChartColor,
            Color RankLineChartTextColor,
            Color RankLineChartDotColor,
            Color RankLineChartDotStrokeColor,
            Color RankLineChartDashColor,
            Color RankLineChartDateTextColor,
            Color ppMainColor,
            Color ppDiffColor,
            Color ppDiffIconColor,
            Color ppProgressBarColorTextColor,
            Color ppProgressBarColor,
            Color ppProgressBarBackgroundColor,
            Color accMainColor,
            Color accDiffColor,
            Color accDiffIconColor,
            Color accProgressBarColorTextColor,
            Color accProgressBarColor,
            Color accProgressBarBackgroundColor,
            Color GradeStatisticsColor_XH,
            Color GradeStatisticsColor_X,
            Color GradeStatisticsColor_SH,
            Color GradeStatisticsColor_S,
            Color GradeStatisticsColor_A,
            Color Details_PlayTimeColor,
            Color Details_TotalHitsColor,
            Color Details_PlayCountColor,
            Color Details_RankedScoreColor,
            Color DetailsDiff_PlayTimeColor,
            Color DetailsDiff_TotalHitsColor,
            Color DetailsDiff_PlayCountColor,
            Color DetailsDiff_RankedScoreColor,
            Color DetailsDiff_PlayTimeIconColor,
            Color DetailsDiff_TotalHitsIconColor,
            Color DetailsDiff_PlayCountIconColor,
            Color DetailsDiff_RankedScoreIconColor,
            Color LevelTitleColor,
            Color LevelProgressBarColor,
            Color LevelProgressBarBackgroundColor,
            Color MainBPTitleColor,
            Color MainBPArtistColor,
            Color MainBPMapperColor,
            Color MainBPBIDColor,
            Color MainBPStarsColor,
            Color MainBPAccColor,
            Color MainBPRankColor,
            Color MainBPppMainColor,
            Color MainBPppTitleColor,
            Color SubBp2ndModeColor,
            Color SubBp2ndBPTitleColor,
            Color SubBp2ndBPVersionColor,
            Color SubBp2ndBPBIDColor,
            Color SubBp2ndBPStarsColor,
            Color SubBp2ndBPAccColor,
            Color SubBp2ndBPRankColor,
            Color SubBp2ndBPppMainColor,
            Color SubBp3rdModeColor,
            Color SubBp3rdBPTitleColor,
            Color SubBp3rdBPVersionColor,
            Color SubBp3rdBPBIDColor,
            Color SubBp3rdBPStarsColor,
            Color SubBp3rdBPAccColor,
            Color SubBp3rdBPRankColor,
            Color SubBp3rdBPppMainColor,
            Color SubBp4thModeColor,
            Color SubBp4thBPTitleColor,
            Color SubBp4thBPVersionColor,
            Color SubBp4thBPBIDColor,
            Color SubBp4thBPStarsColor,
            Color SubBp4thBPAccColor,
            Color SubBp4thBPRankColor,
            Color SubBp4thBPppMainColor,
            Color SubBp5thModeColor,
            Color SubBp5thBPTitleColor,
            Color SubBp5thBPVersionColor,
            Color SubBp5thBPBIDColor,
            Color SubBp5thBPStarsColor,
            Color SubBp5thBPAccColor,
            Color SubBp5thBPRankColor,
            Color SubBp5thBPppMainColor,
            Color footerColor,
            Color SubBpInfoSplitColor,
            float SideImgBrightness,
            float AvatarBrightness,
            float BadgeBrightness,
            float MainBPImgBrightness,
            float CountryFlagBrightness,
            float ModeCaptionBrightness,
            float ModIconBrightness,
            float ScoreModeIconBrightness,
            float OsuSupporterIconBrightness,
            float CountryFlagAlpha,
            float OsuSupporterIconAlpha,
            float BadgeAlpha,
            float AvatarAlpha,
            float ModIconAlpha,
            bool FixedScoreModeIconColor,
            bool DisplaySupporterStatus,
            bool ModeIconAlpha,
            bool Score1ModeIconAlpha,
            bool Score2ModeIconAlpha,
            bool Score3ModeIconAlpha,
            bool Score4ModeIconAlpha,
            bool DetailsPlaytimeIconAlpha,
            bool DetailsTotalHitIconAlpha,
            bool DetailsPlayCountIconAlpha,
            bool DetailsRankedScoreIconAlpha,
            bool ppDiffIconColorAlpha,
            bool accDiffIconColorAlpha,
            bool CountryRankDiffIconColorAlpha
        ) = v2Options;

        var info = new Image<Rgba32>(4000, 2640);
        //获取全部bp

        var prevStatistics = data.prevUserInfo?.Statistics ?? data.userInfo.Statistics; // 没有就为当前数据

        //设定textOption/drawOption
        var textOptions = new RichTextOptions(Fonts.TorusSemiBold.Get(120))
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            FallbackFontFamilies = [HarmonySans, HarmonySansArabic]
        };

        //自定义侧图
        string sidePicPath;
        bool hasSidePic = false;
        if (File.Exists($"./work/panelv2/user_customimg/{data.osuId}.png"))
        {
            sidePicPath = $"./work/panelv2/user_customimg/{data.osuId}.png";
            hasSidePic = true;
        }
        else
        {
            sidePicPath = ColorMode switch
            {
                UserPanelData.CustomMode.Custom => "./work/panelv2/infov2-dark-customimg.png",
                UserPanelData.CustomMode.Light => "./work/panelv2/infov2-light-customimg.png",
                UserPanelData.CustomMode.Dark => "./work/panelv2/infov2-dark-customimg.png",
                _ => throw new ArgumentOutOfRangeException("未知的自定义模式")
            };
        }
            
        using var sidePic = await Utils.ReadImageRgba(sidePicPath); // 读取
        sidePic.Mutate(x => x.Brightness(SideImgBrightness));
        info.Mutate(x => x.DrawImage(sidePic, new Point(90, 72), 1));

        //进度条 - 先绘制进度条，再覆盖面板
        //pp
        using var pp_background = new Image<Rgba32>(1443, 68);
        pp_background.Mutate(x => x.Fill(ppProgressBarBackgroundColor));
        pp_background.Mutate(x => x.RoundCornerParts(new Size(1443, 68), 10, 10, 20, 20));
        info.Mutate(x => x.DrawImage(pp_background, new Point(2358, 410), 1));
        //获取bnspp
        double bounsPP = 0.00;
        double scorePP = 0.00;
        #region bnspp
        if (allBP == null || allBP.Length < 100)
        {
            scorePP = data.userInfo.Statistics.PP;
        }
        else if (allBP!.Length == 0)
        {
            scorePP = data.userInfo.Statistics.PP;
        }
        else
        {
            double pp = 0.0,
                sumOxy = 0.0,
                sumOx2 = 0.0,
                avgX = 0.0,
                avgY = 0.0,
                sumX = 0.0;
            List<double> ys = new();
            for (int i = 0; i < allBP.Length; ++i)
            {
                var tmp = (allBP[i].pp ?? 0.0) * Math.Pow(0.95, i);
                scorePP += tmp;
                ys.Add(Math.Log10(tmp) / Math.Log10(100.0));
            }
            // calculateLinearRegression
            for (int i = 1; i <= ys.Count; ++i)
            {
                double weight = Utils.log1p(i + 1.0);
                sumX += weight;
                avgX += i * weight;
                avgY += ys[i - 1] * weight;
            }
            avgX /= sumX;
            avgY /= sumX;
            for (int i = 1; i <= ys.Count; ++i)
            {
                sumOxy += (i - avgX) * (ys[i - 1] - avgY) * Utils.log1p(i + 1.0);
                sumOx2 += Math.Pow(i - avgX, 2.0) * Utils.log1p(i + 1.0);
            }
            double Oxy = sumOxy / sumX;
            double Ox2 = sumOx2 / sumX;
            // end
            var b = new double[] { avgY - (Oxy / Ox2) * avgX, Oxy / Ox2 };
            for (double i = 100; i <= data.userInfo.Statistics.PlayCount; ++i)
            {
                double val = Math.Pow(100.0, b[0] + b[1] * i);
                if (val <= 0.0)
                {
                    break;
                }
                pp += val;
            }
            scorePP += pp;
            bounsPP = data.userInfo.Statistics.PP - scorePP;
            int totalscores =
                data.userInfo.Statistics.GradeCounts.A
                + data.userInfo.Statistics.GradeCounts.S
                + data.userInfo.Statistics.GradeCounts.SH
                + data.userInfo.Statistics.GradeCounts.SS
                + data.userInfo.Statistics.GradeCounts.SSH;
            bool max;
            if (totalscores >= 25397 || bounsPP >= 416.6667)
                max = true;
            else
                max = false;
            int rankedScores = max
                ? Math.Max(totalscores, 25397)
                : (int)Math.Round(Math.Log10(-(bounsPP / 416.6667) + 1.0) / Math.Log10(0.9994));
            if (double.IsNaN(scorePP) || double.IsNaN(bounsPP))
            {
                scorePP = 0.0;
                bounsPP = 0.0;
                rankedScores = 0;
            }
        }
        #endregion
        //绘制mainpp
        int pp_front_length = 1443 - (int)(1443.0 * (bounsPP / scorePP));
        if (pp_front_length < 1)
            pp_front_length = 1443;
        using var pp_front = new Image<Rgba32>(pp_front_length, 68);
        pp_front.Mutate(x => x.Fill(ppProgressBarColor));
        pp_front.Mutate(x => x.RoundCornerParts(new Size(pp_front_length, 68), 10, 10, 20, 20));
        info.Mutate(x => x.DrawImage(pp_front, new Point(2358, 410), 1));

        //50&100
        using var acc_background = new Image<Rgba32>(1443, 68);
        acc_background.Mutate(x => x.Fill(accProgressBarBackgroundColor));
        acc_background.Mutate(x => x.RoundCornerParts(new Size(1443, 68), 10, 10, 20, 20));
        info.Mutate(x => x.DrawImage(acc_background, new Point(2358, 611), 1));

        //300
        int acc_front_length = (int)(1443.00 * (data.userInfo.Statistics.HitAccuracy / 100.0));
        if (acc_front_length < 1)
            acc_front_length = 1443;
        using var acc_300 = new Image<Rgba32>(acc_front_length, 68);
        acc_300.Mutate(x => x.Fill(accProgressBarColor));
        acc_300.Mutate(x =>
            x.RoundCornerParts(
                new Size((int)(1443.00 * (data.userInfo.Statistics.HitAccuracy / 100.0)), 68),
                10,
                10,
                20,
                20
            )
        );
        info.Mutate(x => x.DrawImage(acc_300, new Point(2358, 611), 1));

        #region junkcodes
        //acc
        //var v1infos = await OSU.GetUserWithV1API(data.userInfo.Id, data.userInfo.PlayMode);
        //var v1info = v1infos!.First();
        //var v1n50 = v1info.Count50;
        //var v1n100 = v1info.Count100;
        //var v1n300 = v1info.Count300;
        //var v1totalhits = v1n50 + v1n100 + v1n300;
        //50&100
        //Img acc_background = new Image<Rgba32>(1443, 68);
        //acc_background.Mutate(x => x.Fill(Color.ParseHex("#c3e7cb")));
        //acc_background.Mutate(x => x.RoundCorner_Parts(new Size(1443, 68), 10, 10, 20, 20));
        //info.Mutate(x => x.DrawImage(acc_background, new Point(2358, 611), 1));
        //only 300
        //Img acc_100 = new Image<Rgba32>((int)(1443.00 * ((double)v1n300 / (double)v1totalhits)), 68);
        //acc_100.Mutate(x => x.Fill(Color.ParseHex("#a4d8b1")));
        //acc_100.Mutate(x => x.RoundCorner_Parts(new Size((int)(1443.0 * ((double)v1n300 / (double)v1totalhits)), 68), 10, 10, 20, 20));
        //info.Mutate(x => x.DrawImage(acc_100, new Point(2358, 611), 1));
        /*
        //100
        Img acc_100 = new Image<Rgba32>((int)(1443.0 - 1443.00 * ((double)v1n50 / (double)v1totalhits)), 68);
        acc_100.Mutate(x => x.Fill(Color.ParseHex("#a4d8b1")));
        acc_100.Mutate(x => x.RoundCorner_Parts(new Size((int)(1443.0 - 1443.0 * ((double)v1n50 / (double)v1totalhits)), 68), 10, 10, 20, 20));
        info.Mutate(x => x.DrawImage(acc_100, new Point(2358, 611), 1));
        //300
        Img acc_300 = new Image<Rgba32>((int)(1443.0 - 1443.0 * (((double)v1n50 + (double)v1n100) / (double)v1totalhits)), 68);
        acc_300.Mutate(x => x.Fill(Color.ParseHex("#92cca7")));
        acc_300.Mutate(x => x.RoundCorner_Parts(new Size((int)(1443.0 - 1443.0 * (((double)v1n50 + (double)v1n100) / (double)v1totalhits)), 68), 10, 10, 20, 20));
        info.Mutate(x => x.DrawImage(acc_300, new Point(2358, 611), 1));
        */
        #endregion

        //top score image 先绘制top bp图片再覆盖面板
        //download background image
        if (allBP!.Length > 0)
        {
            var bp1bgPath = $"./work/background/{allBP![0].Beatmap!.BeatmapId}.png";
            if (!File.Exists(bp1bgPath))
            {
                bp1bgPath = null;
                try
                {
                    bp1bgPath = await OSU.Client.SayoDownloadBeatmapBackgroundImg(
                        allBP![0].Beatmapset!.Id,
                        allBP![0].Beatmap!.BeatmapId,
                        "./work/background/"
                    );
                }
                catch (Exception ex)
                {
                    var msg =
                        $"从Sayo API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                    Log.Warning(msg);
                }

                if (bp1bgPath is null)
                {
                    try
                    {
                        bp1bgPath = await OSU.Client.DownloadBeatmapBackgroundImg(
                            allBP![0].Beatmap!.BeatmapsetId,
                            "./work/background/",
                            $"{allBP![0].Beatmap!.BeatmapId}.png"
                        );
                    }
                    catch (Exception ex)
                    {
                        var msg =
                            $"从OSU API下载背景图片时发生了一处异常\n异常类型: {ex.GetType()}\n异常信息: '{ex.Message}'";
                        Log.Warning(msg);
                    }
                }
            }

            Image<Rgba32> bp1bg;
            if (bp1bgPath is null)
            {
                bp1bg = await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
            }
            else
            {
                try
                {
                    bp1bg = await Img.LoadAsync<Rgba32>(bp1bgPath);
                }
                catch
                {
                    bp1bg = await Img.LoadAsync<Rgba32>("./work/legacy/load-failed-img.png");
                    try
                    {
                        File.Delete(bp1bgPath);
                    }
                    catch { }
                }
            }
            //bp1bg.Mutate(x => x.Resize(355, 200));
            bp1bg.Mutate(x =>
                x.Resize(new ResizeOptions() { Size = new Size(355, 0), Mode = ResizeMode.Max })
            ); //355x200
            bp1bg.Mutate(x => x.Brightness(MainBPImgBrightness));
            info.Mutate(x => x.DrawImage(bp1bg, new Point(1566, 1550), 1));
            bp1bg.Dispose(); // 手动丢弃
        }
        else
        {
            using var bp1bg = new Image<Rgba32>(355, 200);
            switch (ColorMode)
            {
                case UserPanelData.CustomMode.Custom:
                    bp1bg.Mutate(x => x.Fill(Color.White));
                    break;
                case UserPanelData.CustomMode.Light:
                    //light
                    //do nothing
                    break;
                case UserPanelData.CustomMode.Dark:
                    //dark
                    bp1bg.Mutate(x => x.Fill(Color.White));
                    break;
            }
            info.Mutate(x => x.DrawImage(bp1bg, new Point(1566, 1550), 1));
        }

        //level progress
        using var levelprogress_background = new Image<Rgba32>(2312, 12);
        levelprogress_background.Mutate(x => x.Fill(LevelProgressBarBackgroundColor));
        //levelprogress_background.Mutate(x => x.RoundCorner(new Size(2312, 6), 4.5f));
        int levelprogressFrontPos = (int)(
            (double)2312 * (double)data.userInfo.Statistics.Level.Progress / 100.0
        );
        if (levelprogressFrontPos > 0)
        {
            using var levelprogress_front = new Image<Rgba32>(levelprogressFrontPos, 6);
            levelprogress_front.Mutate(x => x.Fill(LevelProgressBarColor));
            levelprogress_front.Mutate(x => x.RoundCorner(new Size(levelprogressFrontPos, 12), 8));
            levelprogress_background.Mutate(x =>
                x.DrawImage(levelprogress_front, new Point(0, 0), 1)
            );
        }
        levelprogress_background.Mutate(x => x.Rotate(-90));
        info.Mutate(x => x.DrawImage(levelprogress_background, new Point(3900, 72), 1));

        //用户面板/自定义面板
        string panelPath;
        if (File.Exists($"./work/panelv2/user_infopanel/{data.osuId}.png"))
            panelPath = $"./work/panelv2/user_infopanel/{data.osuId}.png";
        else
            panelPath = ColorMode switch
            {
                UserPanelData.CustomMode.Custom => "./work/panelv2/infov2-dark.png",
                UserPanelData.CustomMode.Light => "./work/panelv2/infov2-light.png",
                UserPanelData.CustomMode.Dark => "./work/panelv2/infov2-dark.png",
                _ => throw new ArgumentOutOfRangeException("未知的颜色模式"),
            };
        using var panel = await Utils.ReadImageRgba(panelPath); // 读取
        info.Mutate(x => x.DrawImage(panel, new Point(0, 0), 1));

        //rank
        textOptions.Font = TorusRegular.Get(60);
        textOptions.Origin = new PointF(1972, 481);
        textOptions.VerticalAlignment = VerticalAlignment.Center;
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.GlobalRank), RankColor)
        );

        //country_flag
        using var flags = await Img.LoadAsync($"./work/flags/{data.userInfo.Country!.Code}.png");
        flags.Mutate(x => x.Resize(100, 67).Brightness(CountryFlagBrightness));
        flags.Mutate(x =>
            x.ProcessPixelRowsAsVector4(row =>
            {
                for (int p = 0; p < row.Length; p++)
                    if (row[p].W > 0.2f)
                        row[p].W = CountryFlagAlpha;
            })
        );
        info.Mutate(x => x.DrawImage(flags, new Point(1577, 600), 1));

        //country_rank
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        textOptions.Origin = new PointF(1687, 629);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("#{0:N0}", data.userInfo.Statistics.CountryRank), CountryRankColor)
        );
        if (isBonded)
        {
            if (Math.Abs(data.userInfo.Statistics.CountryRank - prevStatistics.CountryRank) >= 1)
            {
                textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                textOptions.Origin = new PointF(2250, 629);
                textOptions.Font = TorusRegular.Get(44);
                info.Mutate(x =>
                    x.DrawText(
                        textOptions,
                        string.Format(
                            "{0:N0}",
                            Math.Abs(
                                data.userInfo.Statistics.CountryRank - prevStatistics.CountryRank
                            )
                        ),
                        CountryRankDiffColor
                    )
                );
                using var cr_indicator_icon_increase = await Utils.ReadImageRgba(
                    $"./work/panelv2/icons/indicator.png"
                );
                cr_indicator_icon_increase.Mutate(x => x.Resize(36, 36));
                if ((data.userInfo.Statistics.CountryRank - prevStatistics.CountryRank) > 0)
                    cr_indicator_icon_increase.Mutate(x => x.Rotate(180));
                cr_indicator_icon_increase.Mutate(x =>
                    x.ProcessPixelRowsAsVector4(row =>
                    {
                        for (int p = 0; p < row.Length; p++)
                        {
                            row[p].X = ((Vector4)CountryRankDiffIconColor).X;
                            row[p].Y = ((Vector4)CountryRankDiffIconColor).Y;
                            row[p].Z = ((Vector4)CountryRankDiffIconColor).Z;
                            if (CountryRankDiffIconColorAlpha)
                                if (row[p].W > 0.2f)
                                    row[p].W = row[p].W * ((Vector4)CountryRankDiffIconColor).W;
                        }
                    })
                );
                info.Mutate(x => x.DrawImage(cr_indicator_icon_increase, new Point(2255, 616), 1));
            }
        }

        //pp
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        textOptions.Origin = new PointF(3120, 350);
        textOptions.Font = TorusRegular.Get(60);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.PP), ppMainColor)
        );
        if (isBonded)
        {
            if (Math.Abs(data.userInfo.Statistics.PP - prevStatistics.PP) >= 1.0)
            {
                textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                textOptions.Origin = new PointF(3735, 350);
                textOptions.Font = TorusRegular.Get(40);
                info.Mutate(x =>
                    x.DrawText(
                        textOptions,
                        string.Format(
                            "{0:N0}",
                            Math.Abs(data.userInfo.Statistics.PP - prevStatistics.PP)
                        ),
                        ppDiffColor
                    )
                );
                using var pp_indicator_icon_increase = await Utils.ReadImageRgba(
                    $"./work/panelv2/icons/indicator.png"
                );
                pp_indicator_icon_increase.Mutate(x => x.Resize(36, 36));
                if ((data.userInfo.Statistics.PP - prevStatistics.PP) < 0)
                    pp_indicator_icon_increase.Mutate(x => x.Rotate(180));
                pp_indicator_icon_increase.Mutate(x =>
                    x.ProcessPixelRowsAsVector4(row =>
                    {
                        for (int p = 0; p < row.Length; p++)
                        {
                            row[p].X = ((Vector4)ppDiffIconColor).X;
                            row[p].Y = ((Vector4)ppDiffIconColor).Y;
                            row[p].Z = ((Vector4)ppDiffIconColor).Z;
                            if (ppDiffIconColorAlpha)
                                if (row[p].W > 0.2f)
                                    row[p].W = row[p].W * ((Vector4)ppDiffIconColor).W;
                        }
                    })
                );
                info.Mutate(x => x.DrawImage(pp_indicator_icon_increase, new Point(3740, 335), 1));
            }
        }

        //acc
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        textOptions.Origin = new PointF(3120, 551);
        textOptions.Font = TorusRegular.Get(60);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:0.##}%", data.userInfo.Statistics.HitAccuracy), accMainColor)
        );
        if (isBonded)
        {
            if (Math.Abs(data.userInfo.Statistics.HitAccuracy - prevStatistics.HitAccuracy) >= 0.01)
            {
                textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                textOptions.Origin = new PointF(3735, 551);
                textOptions.Font = TorusRegular.Get(40);
                info.Mutate(x =>
                    x.DrawText(
                        textOptions,
                        string.Format(
                            "{0:0.##}%",
                            data.userInfo.Statistics.HitAccuracy - prevStatistics.HitAccuracy
                        ),
                        accDiffColor
                    )
                );
                using var acc_indicator_icon_increase = await Utils.ReadImageRgba(
                    $"./work/panelv2/icons/indicator.png"
                );
                acc_indicator_icon_increase.Mutate(x => x.Resize(36, 36));
                if ((data.userInfo.Statistics.HitAccuracy - prevStatistics.HitAccuracy) < 0.0)
                    acc_indicator_icon_increase.Mutate(x => x.Rotate(180));
                acc_indicator_icon_increase.Mutate(x =>
                    x.ProcessPixelRowsAsVector4(row =>
                    {
                        for (int p = 0; p < row.Length; p++)
                        {
                            row[p].X = ((Vector4)accDiffIconColor).X;
                            row[p].Y = ((Vector4)accDiffIconColor).Y;
                            row[p].Z = ((Vector4)accDiffIconColor).Z;
                            if (accDiffIconColorAlpha)
                                if (row[p].W > 0.2f)
                                    row[p].W = row[p].W * ((Vector4)accDiffIconColor).W;
                        }
                    })
                );
                info.Mutate(x => x.DrawImage(acc_indicator_icon_increase, new Point(3740, 536), 1));
            }
        }
        //ppsub
        if (scorePP != 0)
        {
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Font = TorusRegular.Get(40);
            var ppsub_point = 2374 + pp_front_length - 40;
            textOptions.Origin = new PointF(ppsub_point, 440);
            info.Mutate(x =>
                x.DrawText(textOptions, string.Format("{0:N0}", scorePP), ppProgressBarColorTextColor)
            );
        }

        //acc sub
        if (data.userInfo.Statistics.HitAccuracy != 0)
        {
            textOptions.HorizontalAlignment = HorizontalAlignment.Right;
            textOptions.Font = TorusRegular.Get(40);
            var percent = 100d / 100d;
            var accsub_point = 2374 + (percent * 1443.00) - 40;
            textOptions.Origin = new PointF((float)accsub_point, 641f);
            info.Mutate(x =>
                x.DrawText(textOptions, "300", accProgressBarColorTextColor)
            );
        }

        //grades
        textOptions.Font = TorusRegular.Get(38);
        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
        textOptions.Origin = new PointF(2646, 988);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.GradeCounts.SSH), GradeStatisticsColor_XH)
        );
        textOptions.Origin = new PointF(2646 + 218, 988);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.GradeCounts.SS), GradeStatisticsColor_X)
        );
        textOptions.Origin = new PointF(2646 + 218 * 2, 988);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.GradeCounts.SH), GradeStatisticsColor_SH)
        );
        textOptions.Origin = new PointF(2646 + 218 * 3, 988);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.GradeCounts.S), GradeStatisticsColor_S)
        );
        textOptions.Origin = new PointF(2646 + 218 * 4, 988);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.GradeCounts.A), GradeStatisticsColor_A)
        );

        //level main
        textOptions.Origin = new PointF(3906, 2470);
        textOptions.Font = TorusRegular.Get(48);
        info.Mutate(x =>
            x.DrawText(textOptions, data.userInfo.Statistics.Level.Current.ToString(), LevelTitleColor)
        );

        //update time
        textOptions.Origin = new PointF(3955, 2582);
        textOptions.HorizontalAlignment = HorizontalAlignment.Right;
        textOptions.Font = TorusRegular.Get(40);
        info.Mutate(x =>
            x.DrawText(textOptions, $"Update at {DateTime.Now:yyyy/MM/dd HH:mm:ss}", footerColor)
        );

        //desu.life
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        textOptions.Origin = new PointF(90, 2582);
        info.Mutate(x =>
            x.DrawText(textOptions, $"Kanonbot - desu.life", footerColor)
        );

        //details
        textOptions.Font = TorusRegular.Get(50);

        //play time
        textOptions.Origin = new PointF(1705, 1217);
        info.Mutate(x =>
            x.DrawText(textOptions, Utils.Duration2StringWithoutSec(data.userInfo.Statistics.PlayTime), Details_PlayTimeColor)
        );
        //total hits
        textOptions.Origin = new PointF(2285, 1217);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.TotalHits), Details_TotalHitsColor)
        );
        //play count
        textOptions.Origin = new PointF(2853, 1217);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.PlayCount), Details_PlayCountColor)
        );
        //ranked scores
        textOptions.Origin = new PointF(3420, 1217);
        info.Mutate(x =>
            x.DrawText(textOptions, string.Format("{0:N0}", data.userInfo.Statistics.RankedScore), Details_RankedScoreColor)
        );

        //details diff
        if (isBonded)
        {
            textOptions.Font = TorusRegular.Get(36);
            using var indicator_icon_increase = await Utils.ReadImageRgba(
                $"./work/panelv2/icons/indicator.png"
            );
            indicator_icon_increase.Mutate(x => x.Resize(42, 42));
            //using var indicator_icon_decrease = await Utils.ReadImageRgba($"./work/panelv2/icons/indicator.png");
            //indicator_icon_decrease.Mutate(x => x.Resize(42, 42).Rotate(180));
            var text = "";
            //play time
            textOptions.Origin = new PointF(1705, 1265);
            text = Utils.Duration2StringWithoutSec(
                data.userInfo.Statistics.PlayTime - prevStatistics.PlayTime
            );
            info.Mutate(x =>
                x.DrawText(textOptions, text, DetailsDiff_PlayTimeColor)
            );
            var m = TextMeasurer.MeasureSize(text, textOptions);
            indicator_icon_increase.Mutate(x =>
                x.ProcessPixelRowsAsVector4(row =>
                {
                    for (int p = 0; p < row.Length; p++)
                    {
                        row[p].X = ((Vector4)DetailsDiff_PlayTimeIconColor).X;
                        row[p].Y = ((Vector4)DetailsDiff_PlayTimeIconColor).Y;
                        row[p].Z = ((Vector4)DetailsDiff_PlayTimeIconColor).Z;
                        if (DetailsPlaytimeIconAlpha)
                            if (row[p].W > 0.2f)
                                row[p].W = row[p].W * ((Vector4)DetailsDiff_PlayTimeIconColor).W;
                    }
                })
            );
            info.Mutate(x =>
                x.DrawImage(indicator_icon_increase, new Point(1705 + (int)m.Width + 10, 1247), 1)
            );

            //total hits
            textOptions.Origin = new PointF(2285, 1265);
            text = string.Format(
                "{0:N0}",
                data.userInfo.Statistics.TotalHits - prevStatistics.TotalHits
            );
            info.Mutate(x =>
                x.DrawText(textOptions, text, DetailsDiff_TotalHitsColor)
            );
            m = TextMeasurer.MeasureSize(text, textOptions);
            indicator_icon_increase.Mutate(x =>
                x.ProcessPixelRowsAsVector4(row =>
                {
                    for (int p = 0; p < row.Length; p++)
                    {
                        row[p].X = ((Vector4)DetailsDiff_TotalHitsIconColor).X;
                        row[p].Y = ((Vector4)DetailsDiff_TotalHitsIconColor).Y;
                        row[p].Z = ((Vector4)DetailsDiff_TotalHitsIconColor).Z;
                        if (DetailsTotalHitIconAlpha)
                            if (row[p].W > 0.2f)
                                row[p].W = row[p].W * ((Vector4)DetailsDiff_TotalHitsIconColor).W;
                    }
                })
            );
            info.Mutate(x =>
                x.DrawImage(indicator_icon_increase, new Point(2285 + (int)m.Width + 10, 1247), 1)
            );

            //play count
            textOptions.Origin = new PointF(2853, 1265);
            text = string.Format(
                "{0:N0}",
                data.userInfo.Statistics.PlayCount - prevStatistics.PlayCount
            );
            info.Mutate(x =>
                x.DrawText(textOptions, text, DetailsDiff_PlayCountColor)
            );
            m = TextMeasurer.MeasureSize(text, textOptions);
            indicator_icon_increase.Mutate(x =>
                x.ProcessPixelRowsAsVector4(row =>
                {
                    for (int p = 0; p < row.Length; p++)
                    {
                        row[p].X = ((Vector4)DetailsDiff_PlayCountIconColor).X;
                        row[p].Y = ((Vector4)DetailsDiff_PlayCountIconColor).Y;
                        row[p].Z = ((Vector4)DetailsDiff_PlayCountIconColor).Z;
                        if (DetailsPlayCountIconAlpha)
                            if (row[p].W > 0.2f)
                                row[p].W = row[p].W * ((Vector4)DetailsDiff_PlayCountIconColor).W;
                    }
                })
            );
            info.Mutate(x =>
                x.DrawImage(indicator_icon_increase, new Point(2853 + (int)m.Width + 10, 1247), 1)
            );

            //ranked scores
            textOptions.Origin = new PointF(3420, 1265);
            text = string.Format(
                "{0:N0}",
                data.userInfo.Statistics.RankedScore - prevStatistics.RankedScore
            );
            info.Mutate(x =>
                x.DrawText(textOptions, text, DetailsDiff_RankedScoreColor)
            );
            m = TextMeasurer.MeasureSize(text, textOptions);
            indicator_icon_increase.Mutate(x =>
                x.ProcessPixelRowsAsVector4(row =>
                {
                    for (int p = 0; p < row.Length; p++)
                    {
                        row[p].X = ((Vector4)DetailsDiff_RankedScoreIconColor).X;
                        row[p].Y = ((Vector4)DetailsDiff_RankedScoreIconColor).Y;
                        row[p].Z = ((Vector4)DetailsDiff_RankedScoreIconColor).Z;
                        if (DetailsRankedScoreIconAlpha)
                            if (row[p].W > 0.2f)
                                row[p].W = row[p].W * ((Vector4)DetailsDiff_RankedScoreIconColor).W;
                    }
                })
            );
            info.Mutate(x =>
                x.DrawImage(indicator_icon_increase, new Point(3420 + (int)m.Width + 10, 1247), 1)
            );
        }

        List<PPInfo> ppinfos = [];
        for (int i = 0; i < Math.Min(5, allBP.Length); i++)
        {
            PPInfo ppinfo;
            ppinfo = await UniversalCalculator.CalculateData(allBP[i], kind);
            ppinfos.Add(ppinfo);
        }

        var score_mode_iconpos_y = 1853;
        //top performance
        //title  +mods
        textOptions.Font = TorusRegular.Get(90);
        textOptions.Origin = new PointF(1945, 1590);
        if (allBP!.Length > 0)
        {
            var title = "";
            foreach (char c in allBP![0].Beatmapset!.Title)
            {
                title += c;
                var m = TextMeasurer.MeasureSize(title, textOptions);
                if (m.Width > 725)
                {
                    title += "...";
                    break;
                }
            }

            info.Mutate(x =>
                x.DrawText(textOptions,  title, MainBPTitleColor)
            );

            //mods
            OSU.Models.Mod[] firstbpmods;
            if (allBP![0].IsLazer)
            {
                firstbpmods = allBP![0].Mods;
            }
            else
            {
                firstbpmods = allBP![0].Mods.Filter(x => !x.IsClassic).ToArray();
            }

            if (firstbpmods.Length > 0)
            {
                textOptions.Origin = new PointF(
                    1945 + TextMeasurer.MeasureSize(title, textOptions).Width + 25,
                    1611
                );
                textOptions.Font = TorusRegular.Get(40);
                var mainscoremods = "+";
                foreach (var x in firstbpmods)
                {
                    mainscoremods += $"{x.Acronym}, ";
                }
                info.Mutate(x =>
                    x.DrawText(textOptions, mainscoremods[..mainscoremods.LastIndexOf(",")], MainBPTitleColor)
                );
            }

            //artist
            textOptions.Font = TorusRegular.Get(42);
            textOptions.Origin = new PointF(1956, 1668);
            var artist = "";
            foreach (char c in allBP![0].Beatmapset!.Artist)
            {
                artist += c;
                var m = TextMeasurer.MeasureSize(artist, textOptions);
                if (m.Width > 205)
                {
                    artist += "...";
                    break;
                }
            }
            info.Mutate(x =>
                x.DrawText(textOptions, artist, MainBPArtistColor)
            );

            //creator
            textOptions.Origin = new PointF(2231, 1668);
            var creator = "";
            foreach (char c in allBP![0].Beatmapset!.Creator)
            {
                creator += c;
                var m = TextMeasurer.MeasureSize(creator, textOptions);
                if (m.Width > 145)
                {
                    creator += "...";
                    break;
                }
            }
            info.Mutate(x =>
                x.DrawText(textOptions, creator, MainBPMapperColor)
            );

            //bid
            textOptions.Origin = new PointF(2447, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions, allBP![0].Beatmap!.BeatmapId.ToString(), MainBPBIDColor)
            );

            textOptions.Origin = new PointF(2657, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions, ppinfos[0].star.ToString("0.##*"), MainBPStarsColor)
            );

            //acc
            textOptions.Origin = new PointF(2813, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions, allBP![0].AccAuto.ToString("0.##%"), MainBPAccColor)
            );

            //rank
            textOptions.Origin = new PointF(2988, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions, allBP![0].RankAuto, MainBPRankColor)
            );
        }
        else
        {
            info.Mutate(x =>
                x.DrawText(textOptions,  "-", MainBPTitleColor)
            );

            //artist
            textOptions.Font = TorusRegular.Get(42);
            textOptions.Origin = new PointF(1956, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions,  "-", MainBPArtistColor)
            );

            //creator
            textOptions.Origin = new PointF(2231, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions,  "-", MainBPMapperColor)
            );

            //bid
            textOptions.Origin = new PointF(2447, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions,  "-", MainBPBIDColor)
            );

            //star
            textOptions.Origin = new PointF(2657, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions,  "-", MainBPStarsColor)
            );

            //acc
            textOptions.Origin = new PointF(2813, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions,  "-", MainBPAccColor)
            );

            //rank
            textOptions.Origin = new PointF(2988, 1668);
            info.Mutate(x =>
                x.DrawText(textOptions,  "-", MainBPRankColor)
            );
        }

        var bound = allBP.GetUpperBound(0);

        //2nd~5th bp
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        var MainTitleAndDifficultyTitlePos_X = 1673;

        //2nd~5th main title
        textOptions.Font = TorusRegular.Get(50);
        for (int i = 1; i < 5; ++i)
        {
            if (i <= bound)
            {
                var title = "";
                foreach (char c in allBP![i].Beatmapset!.Title)
                {
                    title += c;
                    var m = TextMeasurer.MeasureSize(title, textOptions);
                    if (m.Width > 710)
                    {
                        title += "...";
                        break;
                    }
                }
                textOptions.Origin = new PointF(
                    MainTitleAndDifficultyTitlePos_X,
                    1868 + 186 * (i - 1)
                );
                switch (i)
                {
                    case 1:
                        info.Mutate(x =>
                            x.DrawText(textOptions, title, SubBp2ndBPTitleColor)
                        );
                        break;
                    case 2:
                        info.Mutate(x =>
                            x.DrawText(textOptions, title, SubBp3rdBPTitleColor)
                        );
                        break;
                    case 3:
                        info.Mutate(x =>
                            x.DrawText(textOptions, title, SubBp4thBPTitleColor)
                        );
                        break;
                    case 4:
                        info.Mutate(x =>
                            x.DrawText(textOptions, title, SubBp5thBPTitleColor)
                        );
                        break;
                    default:
                        break;
                }
            }
            else
            {
                textOptions.Origin = new PointF(
                    MainTitleAndDifficultyTitlePos_X,
                    1868 + 186 * (i - 1)
                );
                switch (i)
                {
                    case 1:
                        info.Mutate(x =>
                            x.DrawText(textOptions, "-", SubBp2ndBPTitleColor)
                        );
                        break;
                    case 2:
                        info.Mutate(x =>
                            x.DrawText(textOptions, "-", SubBp3rdBPTitleColor)
                        );
                        break;
                    case 3:
                        info.Mutate(x =>
                            x.DrawText(textOptions, "-", SubBp4thBPTitleColor)
                        );
                        break;
                    case 4:
                        info.Mutate(x =>
                            x.DrawText(textOptions, "-", SubBp5thBPTitleColor)
                        );
                        break;
                    default:
                        break;
                }
            }
        }

        //2nd~5th version and acc and bid and shdklahdksadkjkcna5hoacsporjasldjlksakdlsa
        textOptions.Font = TorusRegular.Get(40);
        var otherbp_mods_pos_y = 1853;
        for (int i = 1; i < 5; ++i)
        {
            if (i <= bound)
            {
                Color splitC = new(),
                    versionC = new(),
                    bidC = new(),
                    starC = new(),
                    accC = new(),
                    rankC = new(),
                    modeC = new();
                splitC = SubBpInfoSplitColor;
                switch (i)
                {
                    case 1:
                        versionC = SubBp2ndBPVersionColor;
                        bidC = SubBp2ndBPBIDColor;
                        starC = SubBp2ndBPStarsColor;
                        accC = SubBp2ndBPAccColor;
                        rankC = SubBp2ndBPRankColor;
                        modeC = SubBp2ndModeColor;
                        break;
                    case 2:
                        versionC = SubBp3rdBPVersionColor;
                        bidC = SubBp3rdBPBIDColor;
                        starC = SubBp3rdBPStarsColor;
                        accC = SubBp3rdBPAccColor;
                        rankC = SubBp3rdBPRankColor;
                        modeC = SubBp3rdModeColor;
                        break;
                    case 3:
                        versionC = SubBp4thBPVersionColor;
                        bidC = SubBp4thBPBIDColor;
                        starC = SubBp4thBPStarsColor;
                        accC = SubBp4thBPAccColor;
                        rankC = SubBp4thBPRankColor;
                        modeC = SubBp4thModeColor;
                        break;
                    case 4:
                        versionC = SubBp5thBPVersionColor;
                        bidC = SubBp5thBPBIDColor;
                        starC = SubBp5thBPStarsColor;
                        accC = SubBp5thBPAccColor;
                        rankC = SubBp5thBPRankColor;
                        modeC = SubBp5thModeColor;
                        break;
                    default:
                        break;
                }

                var title = "";
                foreach (char c in allBP![i].Beatmap!.Version)
                {
                    title += c;
                    var m = TextMeasurer.MeasureSize(title, textOptions);
                    if (m.Width > 130)
                    {
                        title += "...";
                        break;
                    }
                }
                textOptions.Origin = new PointF(
                    MainTitleAndDifficultyTitlePos_X,
                    1925 + 186 * (i - 1)
                );
                info.Mutate(x =>
                    x.DrawText(textOptions,  title, versionC)
                );
                var textMeasurePos =
                    MainTitleAndDifficultyTitlePos_X
                    + TextMeasurer.MeasureSize(title, textOptions).Width
                    + 5;
                //split
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  " | ", splitC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //bid
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions, allBP![i].Beatmap!.BeatmapId.ToString(), bidC)
                );
                textMeasurePos =
                    textMeasurePos
                    + TextMeasurer
                        .MeasureSize(allBP![i].Beatmap!.BeatmapId.ToString(), textOptions)
                        .Width
                    + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  " | ", splitC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //star
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions, ppinfos[i].star.ToString("0.##*"), starC)
                );
                textMeasurePos =
                    textMeasurePos
                    + TextMeasurer.MeasureSize(ppinfos[i].star.ToString("0.##*"), textOptions).Width
                    + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  " | ", splitC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //acc
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions, allBP![i].AccAuto.ToString("0.##%"), accC)
                );
                textMeasurePos =
                    textMeasurePos
                    + TextMeasurer
                        .MeasureSize(allBP![i].AccAuto.ToString("0.##%"), textOptions)
                        .Width
                    + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  " | ", splitC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //ranking
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions, allBP![i].RankAuto, rankC)
                );
                //shdklahdksadkjkcna5hoacsporjasldjlksakdlsa

                OSU.Models.Mod[] bpmods;
                if (allBP![i].IsLazer)
                {
                    bpmods = allBP![i].Mods;
                }
                else
                {
                    bpmods = allBP![i].Mods.Filter(x => !x.IsClassic).ToArray();
                }

                if (bpmods.Length > 0)
                {
                    var otherbp_mods_pos_x = 2580;
                    foreach (var x in bpmods)
                    {
                        if (!File.Exists($"./work/mods_v2/2x/{x.Acronym}.png"))
                            continue;
                        using var modicon = await Img.LoadAsync(
                            $"./work/mods_v2/2x/{x.Acronym}.png"
                        );
                        modicon.Mutate(x => x.Resize(90, 90).Brightness(ModIconBrightness));
                        modicon.Mutate(x =>
                            x.ProcessPixelRowsAsVector4(row =>
                            {
                                for (int p = 0; p < row.Length; p++)
                                    if (row[p].W > 0.2f)
                                        row[p].W = ModIconAlpha;
                            })
                        );
                        info.Mutate(x =>
                            x.DrawImage(
                                modicon,
                                new Point(otherbp_mods_pos_x, otherbp_mods_pos_y),
                                1
                            )
                        );
                        otherbp_mods_pos_x += 105;
                    }
                }
                otherbp_mods_pos_y += 186;

                //mode_icon
                using var osuscoremode_icon = await Utils.ReadImageRgba(
                    $"./work/panelv2/icons/mode_icon/score/{data.userInfo.Mode.ToStr()}.png"
                );
                osuscoremode_icon.Mutate(x => x.Resize(92, 92));
                if (FixedScoreModeIconColor)
                {
                    //固定
                    osuscoremode_icon.Mutate(x =>
                        x.ProcessPixelRowsAsVector4(row =>
                        {
                            for (int p = 0; p < row.Length; p++)
                            {
                                row[p].X = ((Vector4)modeC).X;
                                row[p].Y = ((Vector4)modeC).Y;
                                row[p].Z = ((Vector4)modeC).Z;
                                switch (i)
                                {
                                    case 1:
                                        if (Score1ModeIconAlpha)
                                            if (row[p].W > 0.0f)
                                                row[p].W =
                                                    row[p].W * ((Vector4)SubBp2ndModeColor).W;
                                        break;
                                    case 2:
                                        if (Score2ModeIconAlpha)
                                            if (row[p].W > 0.0f)
                                                row[p].W =
                                                    row[p].W * ((Vector4)SubBp3rdModeColor).W;
                                        break;
                                    case 3:
                                        if (Score3ModeIconAlpha)
                                            if (row[p].W > 0.0f)
                                                row[p].W =
                                                    row[p].W * ((Vector4)SubBp4thModeColor).W;
                                        break;
                                    case 4:
                                        if (Score4ModeIconAlpha)
                                            if (row[p].W > 0.0f)
                                                row[p].W =
                                                    row[p].W * ((Vector4)SubBp5thModeColor).W;
                                        break;
                                }
                            }
                        })
                    );
                }
                else
                {
                    //随难度渐变
                    modeC = Utils.ForStarDifficulty(ppinfos[i].star);
                    osuscoremode_icon.Mutate(x =>
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
                }
                info.Mutate(x =>
                    x.DrawImage(osuscoremode_icon, new Point(1558, score_mode_iconpos_y), 1)
                );
                score_mode_iconpos_y += 186;
            }
            else
            {
                Color splitC = new(),
                    versionC = new(),
                    bidC = new(),
                    starC = new(),
                    accC = new(),
                    rankC = new(),
                    modeC = new();
                splitC = SubBpInfoSplitColor;
                switch (i)
                {
                    case 1:
                        versionC = SubBp2ndBPVersionColor;
                        bidC = SubBp2ndBPBIDColor;
                        starC = SubBp2ndBPStarsColor;
                        accC = SubBp2ndBPAccColor;
                        rankC = SubBp2ndBPRankColor;
                        modeC = SubBp2ndModeColor;
                        break;
                    case 2:
                        versionC = SubBp3rdBPVersionColor;
                        bidC = SubBp3rdBPBIDColor;
                        starC = SubBp3rdBPStarsColor;
                        accC = SubBp3rdBPAccColor;
                        rankC = SubBp3rdBPRankColor;
                        modeC = SubBp3rdModeColor;
                        break;
                    case 3:
                        versionC = SubBp4thBPVersionColor;
                        bidC = SubBp4thBPBIDColor;
                        starC = SubBp4thBPStarsColor;
                        accC = SubBp4thBPAccColor;
                        rankC = SubBp4thBPRankColor;
                        modeC = SubBp4thModeColor;
                        break;
                    case 4:
                        versionC = SubBp5thBPVersionColor;
                        bidC = SubBp5thBPBIDColor;
                        starC = SubBp5thBPStarsColor;
                        accC = SubBp5thBPAccColor;
                        rankC = SubBp5thBPRankColor;
                        modeC = SubBp5thModeColor;
                        break;
                    default:
                        break;
                }

                textOptions.Origin = new PointF(
                    MainTitleAndDifficultyTitlePos_X,
                    1925 + 186 * (i - 1)
                );
                info.Mutate(x =>
                    x.DrawText(textOptions,  "-", versionC)
                );
                var textMeasurePos =
                    MainTitleAndDifficultyTitlePos_X
                    + TextMeasurer.MeasureSize("-", textOptions).Width
                    + 5;
                //split
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  " | ", splitC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //bid
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  "-", bidC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize("-", textOptions).Width + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  " | ", splitC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //star
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  "-", starC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize("-", textOptions).Width + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  " | ", splitC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //acc
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  "-", accC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize("-", textOptions).Width + 5;

                //split
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  " | ", splitC)
                );
                textMeasurePos =
                    textMeasurePos + TextMeasurer.MeasureSize(" | ", textOptions).Width + 5;

                //ranking
                textOptions.Origin = new PointF(textMeasurePos, 1925 + 186 * (i - 1));
                info.Mutate(x =>
                    x.DrawText(textOptions,  "-", rankC)
                );
                otherbp_mods_pos_y += 186;
            }
        }

        if (0 <= bound) { }

        var pp_text = "-";

        //all pp
        textOptions.Font = TorusRegular.Get(90);
        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
        textOptions.Origin = new PointF(3642, 1670);

        if (0 <= bound)
        {
            pp_text = string.Format("{0:N1}", ppinfos[0].ppStat.total);
        }
        else
        {
            pp_text = "-";
        }

        info.Mutate(x =>
            x.DrawText(textOptions,  pp_text, MainBPppMainColor)
        );
        var bp1pptextMeasure = TextMeasurer.MeasureSize(pp_text, textOptions);
        int bp1pptextpos = 3642 - (int)bp1pptextMeasure.Width / 2;
        textOptions.Font = TorusRegular.Get(40);
        textOptions.Origin = new PointF(bp1pptextpos, 1610);
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        info.Mutate(x =>
            x.DrawText(textOptions,  "pp", MainBPppTitleColor)
        );

        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
        textOptions.Font = TorusRegular.Get(70);
        textOptions.Origin = new PointF(3642, 1895);

        if (1 <= bound)
        {
            pp_text = string.Format("{0:N0}pp", ppinfos[1].ppStat.total);
        }
        else
        {
            pp_text = "-";
        }

        info.Mutate(x =>
            x.DrawText(textOptions, pp_text, SubBp2ndBPppMainColor)
        );
        textOptions.Origin = new PointF(3642, 2081);

        if (2 <= bound)
        {
            pp_text = string.Format("{0:N0}pp", ppinfos[2].ppStat.total);
        }
        else
        {
            pp_text = "-";
        }

        info.Mutate(x =>
            x.DrawText(textOptions, pp_text, SubBp3rdBPppMainColor)
        );
        textOptions.Origin = new PointF(3642, 2266);

        if (3 <= bound)
        {
            pp_text = string.Format("{0:N0}pp", ppinfos[3].ppStat.total);
        }
        else
        {
            pp_text = "-";
        }

        info.Mutate(x =>
            x.DrawText(textOptions, pp_text, SubBp4thBPppMainColor)
        );
        textOptions.Origin = new PointF(3642, 2450);

        if (4 <= bound)
        {
            pp_text = string.Format("{0:N0}pp", ppinfos[4].ppStat.total);
        }
        else
        {
            pp_text = "-";
        }

        info.Mutate(x =>
            x.DrawText(textOptions, pp_text, SubBp5thBPppMainColor)
        );

        //badges
        if (data.badgeId != null)
            if (data.badgeId.Count > 0)
                if (data.badgeId[0] != -1)
                {
                    for (int i = 0; i < data.badgeId.Count; ++i)
                    {
                        if (data.badgeId[i] == -9)
                            continue;

                        if (!File.Exists($"./work/badges/{data.badgeId[i]}.png") && KanonBot.Config.inner!.dev!)
                        {
                            continue;
                        }
                        
                        var (_badge, format) = await Utils.ReadImageRgbaWithFormat(
                            $"./work/badges/{data.badgeId[i]}.png"
                        );
                        using var badge = _badge;
                        //检测上传的badge format是否正确，否则重新格式化
                        if (format.DefaultMimeType.Trim().ToLower()[..3] != "png")
                        {
                            File.Delete($"./work/badges/{data.badgeId[i]}.png");
                            badge.Save($"./work/badges/{data.badgeId[i]}.png", new PngEncoder());
                        }

                        // var roundedCorner = true;
                        // badge.ProcessPixelRows(row =>
                        // {
                        //     roundedCorner = row.GetRowSpan(0)[0] == Rgba32.ParseHex("#000000");
                        // });
                        // if (!roundedCorner)
                        //     badge.Mutate(x => x.RoundCorner(badge.Size, 20));

                        //绘制
                        if (i < 5)
                        {
                            //top
                            badge.Mutate(x => x.Resize(236, 110).Brightness(BadgeBrightness));

                            badge.Mutate(x =>
                                x.ProcessPixelRowsAsVector4(row =>
                                {
                                    for (int p = 0; p < row.Length; p++)
                                        if (row[p].W > 0.2f)
                                            row[p].W = BadgeAlpha;
                                })
                            );
                            if (data.userInfo.IsSupporter && DisplaySupporterStatus)
                                info.Mutate(x =>
                                    x.DrawImage(badge, new Point(3420 - i * 276, 93), 1)
                                );
                            else
                                info.Mutate(x =>
                                    x.DrawImage(badge, new Point(3566 - i * 276, 93), 1)
                                );
                        }
                        else
                        {
                            //bottom
                            badge.Mutate(x =>
                                x.Brightness(BadgeBrightness).Resize(108, 50)
                            //.RoundCorner(new Size(108, 50), 6.0f)
                            );

                            badge.Mutate(x =>
                                x.ProcessPixelRowsAsVector4(row =>
                                {
                                    for (int p = 0; p < row.Length; p++)
                                        if (row[p].W > 0.2f)
                                            row[p].W = BadgeAlpha;
                                })
                            );
                            if (data.userInfo.IsSupporter && DisplaySupporterStatus)
                                info.Mutate(x =>
                                    x.DrawImage(badge, new Point(3414 - (i - 6) * 132, 223), 1)
                                );
                            else
                                info.Mutate(x =>
                                    x.DrawImage(badge, new Point(3560 - (i - 6) * 132, 223), 1)
                                );
                        }
                    }
                }

        //osu!supporter
        if (data.userInfo.IsSupporter && DisplaySupporterStatus)
        {
            using var temp = await Utils.ReadImageRgba($"./work/panelv2/icons/supporter.png");
            temp.Mutate(x => x.Resize(110, 110).Brightness(OsuSupporterIconBrightness));
            temp.Mutate(x =>
                x.ProcessPixelRowsAsVector4(row =>
                {
                    for (int p = 0; p < row.Length; p++)
                        if (row[p].W > 0.2f)
                            row[p].W = OsuSupporterIconAlpha;
                })
            );
            info.Mutate(x => x.DrawImage(temp, new Point(3692, 93), 1));
        }

        //avatar
        using var avatar = await Utils.LoadOrDownloadAvatar(data.userInfo);

        // 亮度
        avatar.Mutate(x => x.Brightness(AvatarBrightness));
        avatar.Mutate(x => x.Resize(200, 200).RoundCorner(new Size(200, 200), 25));
        avatar.Mutate(x =>
            x.ProcessPixelRowsAsVector4(row =>
            {
                for (int p = 0; p < row.Length; p++)
                    if (row[p].W > 0.2f)
                        row[p].W = AvatarAlpha;
            })
        );
        info.Mutate(x => x.DrawImage(avatar, new Point(1531, 72), 1));

        //username
        textOptions.Font = TorusSemiBold.Get(120);
        textOptions.VerticalAlignment = VerticalAlignment.Bottom;
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        textOptions.Origin = new PointF(1780, 230);
        info.Mutate(x =>
            x.DrawText(textOptions, data.userInfo.Username, UsernameColor)
        );

        //osu!mode
        using var osuprofilemode_icon = await Utils.ReadImageRgba(
            $"./work/panelv2/icons/mode_icon/profile/{data.userInfo.Mode.ToStr()}.png"
        );
        var osuprofilemode_text = "";
        if (data.modeString is null)
        {
            osuprofilemode_text = data.userInfo.Mode.ToDisplay();
        }
        else
        {
            osuprofilemode_text = data.modeString;
        }
        textOptions.Font = TorusRegular.Get(55);
        textOptions.VerticalAlignment = VerticalAlignment.Center;
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        var osuprofilemode_text_measure = TextMeasurer.MeasureSize(
            osuprofilemode_text,
            textOptions
        );
        using var osuprofilemode = new Image<Rgba32>(
            (int)(70.0f + osuprofilemode_text_measure.Width),
            102
        ); //804x102(80?)
        osuprofilemode_icon.Mutate(x => x.Resize(60, 60));
        osuprofilemode.Mutate(x => x.DrawImage(osuprofilemode_icon, new Point(0, 21), 1));
        textOptions.Origin = new PointF(70, 48);
        osuprofilemode.Mutate(x =>
            x.DrawText(textOptions, osuprofilemode_text, footerColor)
        );
        osuprofilemode.Mutate(x =>
            x.ProcessPixelRowsAsVector4(row =>
            {
                for (int p = 0; p < row.Length; p++)
                {
                    //X、Y、Z和W字段分别映射 RGBA 通道。
                    row[p].X = ((Vector4)ModeIconColor).X;
                    row[p].Y = ((Vector4)ModeIconColor).Y;
                    row[p].Z = ((Vector4)ModeIconColor).Z;
                    if (ModeIconAlpha)
                        if (row[p].W > 0.0f)
                            row[p].W = row[p].W * ((Vector4)ModeIconColor).W;
                }
            })
        );

        var osuprofilemode_x_pos = 1531 + (804 / 2) - (osuprofilemode.Width / 2);
        info.Mutate(x => x.DrawImage(osuprofilemode, new Point(osuprofilemode_x_pos, 293), 1));

        //osu!rankChart
        long[] RankHistory;
        if (data.userInfo.RankHistory != null)
        {
            RankHistory = data.userInfo.RankHistory.Data.Reverse().Take(8).ToArray();
        }
        else
        {
            RankHistory = [0, 0, 0, 0, 0, 0, 0, 0];
        }
        using var rankChart = LineChart.Draw(
            714,
            240,
            7,
            RankHistory,
            RankLineChartColor,
            RankLineChartTextColor,
            RankLineChartDashColor,
            RankLineChartDotColor,
            RankLineChartDotStrokeColor,
            true,
            7f,
            5f
        );
        info.Mutate(x => x.DrawImage(rankChart, new Point(1576, 694), 1));

        //date
        var DateXPos = 2227;
        var DateValue = DateTime.Now;
        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
        textOptions.Font = TorusRegular.Get(34);
        for (int i = 0; i < 7; i++)
        {
            textOptions.Origin = new PointF(DateXPos, 960);
            info.Mutate(x =>
                x.DrawText(textOptions, $"{DateValue.Month}.{DateValue.Day}", RankLineChartDateTextColor)
            );
            DateXPos -= 100;
            DateValue = DateValue.AddDays(-1);
        }

        if (!hasSidePic && !isBonded) {
            info.Mutate(x => x.Crop(new(1500, 0, 2500, 2640)));
            var originalWidth = info.Width;
            var originalHeight = info.Height;
            // 创建扩展后的新图像
            using (var newImage = new Image<Rgba32>(originalWidth + 100, originalHeight))
            {
                // 将原图像绘制到右侧（偏移100px）
                newImage.Mutate(x => x.DrawImage(info, new Point(100, 0), 1f));
                
                // 获取最左边缘的像素列并重复填充到左侧扩展区域
                for (int x = 0; x < 100; x++)
                {
                    for (int y = 0; y < originalHeight; y++)
                    {
                        var edgeColor = info[0, y]; // 获取原图最左边缘的颜色
                        newImage[x, y] = edgeColor;
                    }
                }
                
                // 替换原图像
                info = newImage.Clone();
            }

            //desu.life
            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
            textOptions.Origin = new PointF(90, 2582);
            info.Mutate(x =>
                x.DrawText(textOptions, $"Kanonbot - desu.life", footerColor)
            );
        }


        //resize to 1920x?
        if (!output4k)
            info.Mutate(x =>
                x.Resize(new ResizeOptions() { Size = new Size(1920, 0), Mode = ResizeMode.Max })
            );

        // 不知道为啥更新了imagesharp后对比度(亮度)变了
        info.Mutate(x => x.Brightness(0.998f));

        return info;
    }

   
}
