using KanonBot.LegacyImage;

namespace KanonBot.OsuPerformance
{
    public static class UniversalCalculator
    {
       

        public static async Task<Draw.ScorePanelData> CalculatePanelDataAuto(
            API.OSU.Models.ScoreLazer score
        )
        {
            // if (score.IsClassic)
            // {
            //     return await RosuCalculator.CalculatePanelData(score);
            // }
            // else
            // {
                return await OsuCalculator.CalculatePanelData(score);
            // }
        }

        public static async Task<PPInfo> CalculateDataAuto(API.OSU.Models.ScoreLazer score)
        {
            // if (score.IsClassic)
            // {
            //     return await RosuCalculator.CalculateData(score);
            // }
            // else
            // {
                return await OsuCalculator.CalculateData(score);
            // }
        }
    }
}
