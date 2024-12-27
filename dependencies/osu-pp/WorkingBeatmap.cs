using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Skinning;

namespace OsuPP;

public class CalculatorWorkingBeatmap : WorkingBeatmap
{
    private readonly Beatmap _beatmap;

    public CalculatorWorkingBeatmap(Stream beatmapStream) : this(ReadFromStream(beatmapStream)) { }
    public CalculatorWorkingBeatmap(byte[] b) : this(ReadFromBytes(b)) { }

    private CalculatorWorkingBeatmap(Beatmap beatmap) : base(beatmap.BeatmapInfo, null)
    {
        _beatmap = beatmap;
        _beatmap.BeatmapInfo.Ruleset = Utils.ParseRuleset(beatmap.BeatmapInfo.Ruleset.OnlineID)!.RulesetInfo;
    }

    static Beatmap ReadFromStream(Stream stream)
    {
        using var reader = new LineBufferedReader(stream);
        return Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
    }

    static Beatmap ReadFromBytes(byte[] bytes) {
        using var stream = new MemoryStream(bytes);
        return ReadFromStream(stream);
    }

    protected override IBeatmap GetBeatmap() => _beatmap;
    public override Texture? GetBackground() => null;
    protected override Track? GetBeatmapTrack() => null;
    protected override ISkin? GetSkin() => null;
    public override Stream? GetStream(string storagePath) => null;
}
