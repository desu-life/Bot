using KanonBot.LegacyImage;

namespace KanonBot.OsuPerformance
{
    public enum CalculatorKind
    {
        Unset,
        Osu,
        Rosu,
        Oppai,
        Sb
    }

    public static class UniversalCalculator
    {
        public static async Task<Draw.ScorePanelData> CalculatePanelData(
            API.OSU.Models.ScoreLazer score,
            CalculatorKind kind = CalculatorKind.Unset
        )
        {
            var b = await Utils.LoadOrDownloadBeatmap(score.Beatmap!);
            return CalculatePanelData(b, score, kind);
        }

        public static Draw.ScorePanelData CalculatePanelData(
            byte[] b,
            API.OSU.Models.ScoreLazer score,
            CalculatorKind kind = CalculatorKind.Unset
        )
        {
            if (kind is CalculatorKind.Oppai && score.Mode != API.OSU.Mode.OSU) {
                kind = CalculatorKind.Unset;
            }

            // oldpp_calc
            if (kind == CalculatorKind.Unset && KanonBot.Config.inner!.calcOldPP) {
                var currpp = OsuCalculator.CalculatePanelData(b, score);
                var oldpp = RosuCalculator.CalculatePanelData(b, score);
                currpp.oldPP = oldpp.ppInfo!.ppStat.total;
                return currpp;
            }

            return kind switch
            {
                CalculatorKind.Osu => OsuCalculator.CalculatePanelData(b, score),
                CalculatorKind.Rosu => RosuCalculator.CalculatePanelData(b, score),
                CalculatorKind.Oppai => OppaiCalculator.CalculatePanelData(b, score),
                CalculatorKind.Sb => SBRosuCalculator.CalculatePanelData(b, score),
                _ => OsuCalculator.CalculatePanelData(b, score),
            };
        }

        public static async Task<PPInfo> CalculateData(
            API.OSU.Models.ScoreLazer score,
            CalculatorKind kind = CalculatorKind.Unset
        )
        {
            var b = await Utils.LoadOrDownloadBeatmap(score.Beatmap!);
            return CalculateData(b, score, kind);
        }

        public static PPInfo CalculateData(
            byte[] b,
            API.OSU.Models.ScoreLazer score,
            CalculatorKind kind = CalculatorKind.Unset
        )
        {
            if (kind is CalculatorKind.Oppai && score.Mode != API.OSU.Mode.OSU) {
                kind = CalculatorKind.Unset;
            }

            return kind switch
            {
                CalculatorKind.Osu => OsuCalculator.CalculateData(b, score),
                CalculatorKind.Rosu => RosuCalculator.CalculateData(b, score),
                CalculatorKind.Oppai => OppaiCalculator.CalculateData(b,score),
                CalculatorKind.Sb => SBRosuCalculator.CalculateData(b,score),
                _ => OsuCalculator.CalculateData(b,score),
            };
        }
    }
}
