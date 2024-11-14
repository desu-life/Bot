using KanonBot.LegacyImage;

namespace KanonBot.OsuPerformance
{
    public enum CalculatorKind
    {
        Unset,
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
            if (kind is CalculatorKind.Oppai && score.Mode != API.OSU.Mode.OSU && !score.Mods.Any(m => m.IsClassic)) {
                kind = CalculatorKind.Unset;
            }

            return kind switch
            {
                CalculatorKind.Rosu => RosuCalculator.CalculatePanelData(b, score),
                CalculatorKind.Oppai => OppaiCalculator.CalculatePanelData(b, score),
                CalculatorKind.Sb => SBRosuCalculator.CalculatePanelData(b, score),
                _ => RosuCalculator.CalculatePanelData(b, score),
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
            if (kind is CalculatorKind.Oppai && score.Mode != API.OSU.Mode.OSU && !score.Mods.Any(m => m.IsClassic)) {
                kind = CalculatorKind.Unset;
            }

            return kind switch
            {
                CalculatorKind.Rosu => RosuCalculator.CalculateData(b, score),
                CalculatorKind.Oppai => OppaiCalculator.CalculateData(b,score),
                CalculatorKind.Sb => SBRosuCalculator.CalculateData(b,score),
                _ => RosuCalculator.CalculateData(b,score),
            };
        }
    }
}
