using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using global::Takumi.Render.UniFFI;
using SixLabors.ImageSharp.Formats.Png;
using IOPath = System.IO.Path;

namespace KanonBot.Image.Takumi;

public static class PPVSTakumi
{
    private const int PanelWidth = 1134;
    private const int PanelHeight = 1553;

    private static readonly string templateRoot = IOPath.Combine(
        AppContext.BaseDirectory,
        "resources",
        "templates",
        "PPVS"
    );
    private static readonly string workingRoot = IOPath.Combine(
        Directory.GetCurrentDirectory(),
        "work"
    );
    private static readonly JsonSerializerOptions TemplateJsonOptions =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static readonly string[] TemplateFontPaths =
    [
        IOPath.Combine(workingRoot, "fonts", "Torus-Regular.ttf"),
        IOPath.Combine(workingRoot, "fonts", "Torus-SemiBold.ttf"),
    ];

    private static readonly Renderer Renderer = CreateTemplateRenderer();

    public static Task<RenderedImage> Draw(KanonBot.Image.PPVS.PPVSPanelData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.u1);
        ArgumentNullException.ThrowIfNull(data.u2);

        var templatePath = IOPath.Combine(templateRoot, "index.jinja");
        var context = BuildTemplateContext(data);

        return Task.FromResult(Renderer.Render(
            new RenderRequest
            {
                Input = RenderInput.File(RenderContentKind.JinjaHtml, templatePath),
                ContextJson = JsonSerializer.Serialize(context, TemplateJsonOptions),
                Viewport = new RenderSize((uint)PanelWidth, (uint)PanelHeight),
                Format = ImageFormat.Png,
                LoadLinkedStylesheets = true,
                ResolveLocalAssets = true
            }
        ));
    }

    private static Renderer CreateTemplateRenderer()
    {
        var renderer = new Renderer();
        renderer.AddSearchPath(workingRoot);
        renderer.AddSearchPath(templateRoot);

        foreach (var path in TemplateFontPaths)
        {
            renderer.AddFontFile(path);
        }

        return renderer;
    }

    private static PPVSContext BuildTemplateContext(KanonBot.Image.PPVS.PPVSPanelData data)
    {
        var u1Values = GetPpPlusValues(data.u1);
        var u2Values = GetPpPlusValues(data.u2);

        return new PPVSContext
        {
            BackgroundSrc = AssetPath("legacy", "ppvs.png"),
            TitleSrc = AssetPath("legacy", "ppvs_title.png"),
            U1SvgPoints = HexagramHelper.ToSvgPoints(u1Values, PpPlusMulti, PpPlusExp, 6, 1134, 12000, 2),
            U2SvgPoints = HexagramHelper.ToSvgPoints(u2Values, PpPlusMulti, PpPlusExp, 6, 1134, 12000, 2),
            U1FillColor = "rgba(255, 123, 172, 0.196)",
            U1StrokeColor = "rgba(255, 123, 172, 1)",
            U2FillColor = "rgba(41, 171, 226, 0.196)",
            U2StrokeColor = "rgba(41, 171, 226, 1)",
            U1DrawFirst = data.u1.PerformanceTotal < data.u2.PerformanceTotal,
            U1Name = data.u1Name,
            U2Name = data.u2Name,
            U1Values = u1Values.Select(v => Math.Round(v).ToString()).ToList(),
            U2Values = u2Values.Select(v => Math.Round(v).ToString()).ToList(),
            U1Total = data.u1.PerformanceTotal.ToString("0.##"),
            U2Total = data.u2.PerformanceTotal.ToString("0.##"),
            DiffTotal = Math.Round(data.u2.PerformanceTotal - data.u1.PerformanceTotal).ToString(),
            DiffValues =
            [
                Math.Round(u2Values[0] - u1Values[0]).ToString(),
                Math.Round(u2Values[1] - u1Values[1]).ToString(),
                Math.Round(u2Values[2] - u1Values[2]).ToString(),
                Math.Round(u2Values[3] - u1Values[3]).ToString(),
                Math.Round(u2Values[4] - u1Values[4]).ToString(),
                Math.Round(u2Values[5] - u1Values[5]).ToString()
            ]
        };
    }

    private static readonly double[] PpPlusMulti = [14.1, 69.7, 1.92, 19.8, 0.588, 3.06];
    private static readonly double[] PpPlusExp = [0.769, 0.596, 0.953, 0.8, 1.175, 0.993];

    private static double[] GetPpPlusValues(API.OSU.Models.PPlusData.UserPerformancesNext data)
    {
        return
        [
            data.AccuracyTotal,
            data.FlowAimTotal,
            data.JumpAimTotal,
            data.PrecisionTotal,
            data.SpeedTotal,
            data.StaminaTotal
        ];
    }

    private static string AssetPath(params string[] parts)
    {
        return IOPath.GetFullPath(IOPath.Combine(workingRoot, IOPath.Combine(parts)))
            .Replace('\\', '/');
    }
}
