using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace KanonBot.Image;

static class Fonts
{
    private static FontCollection fonts = new();
    public static FontFamily Exo2SemiBold = fonts.Add("./work/fonts/Exo2/Exo2-SemiBold.ttf");
    public static FontFamily Exo2Regular = fonts.Add("./work/fonts/Exo2/Exo2-Regular.ttf");
    public static FontFamily HarmonySans = fonts.Add(
        "./work/fonts/HarmonyOS_Sans_SC/HarmonyOS_Sans_SC_Regular.ttf"
    );

    public static FontFamily HarmonySansArabic = fonts.Add(
        "./work/fonts/HarmonyOS_Sans_Naskh_Arabic/HarmonyOS_Sans_Naskh_Arabic_Regular.ttf"
    );
    public static FontFamily TorusRegular = fonts.Add("./work/fonts/Torus-Regular.ttf");
    public static FontFamily TorusSemiBold = fonts.Add("./work/fonts/Torus-SemiBold.ttf");
    public static FontFamily avenirLTStdMedium = fonts.Add("./work/fonts/AvenirLTStd-Medium.ttf");

    public static FontFamily Mizolet = fonts.Add("./work/fonts/mizolet.ttf");
    public static FontFamily MizoletBokutoh = fonts.Add("./work/fonts/mizolet-bokutoh.ttf");
    public static FontFamily FredokaRegular = fonts.Add("./work/fonts/fredoka/Fredoka-Regular.ttf");
    public static FontFamily FredokaBold = fonts.Add("./work/fonts/fredoka/Fredoka-Bold.ttf");

    public static Font Get(this FontFamily fontFamily, float size) =>
        memo<(FontFamily, float), Font>(static args => new Font(args.Item1, args.Item2)).Invoke((fontFamily, size));

    public static Font Get(this FontFamily fontFamily, float size, FontStyle style) =>
        memo<(FontFamily, float, FontStyle), Font>(static args => new Font(args.Item1, args.Item2, args.Item3)).Invoke((fontFamily, size, style));

    
}
