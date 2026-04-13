namespace KanonBot.API.OSU;

public static class KagamiExtensions
{
    public static string ToOsuModeApiValue(this API.OSU.Mode mode)
    {
        return mode switch
        {
            API.OSU.Mode.OSU => "osu",
            API.OSU.Mode.Taiko => "taiko",
            API.OSU.Mode.Mania => "mania",
            API.OSU.Mode.Fruits => "ctb",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }


    public static string ToPpysbModeApiValue(this API.PPYSB.Mode mode)
    {
        return mode switch
        {
            API.PPYSB.Mode.OSU => "osu",
            API.PPYSB.Mode.Taiko => "taiko",
            API.PPYSB.Mode.Mania => "mania",
            API.PPYSB.Mode.Fruits => "ctb",
            API.PPYSB.Mode.RelaxOsu => "rx_osu",
            API.PPYSB.Mode.RelaxTaiko => "rx_taiko",
            API.PPYSB.Mode.RelaxMania => "rx_mania",
            API.PPYSB.Mode.RelaxFruits => "rx_ctb",
            API.PPYSB.Mode.AutoPilotOsu => "ap_osu",
            API.PPYSB.Mode.AutoPilotTaiko => "ap_taiko",
            API.PPYSB.Mode.AutoPilotMania => "ap_mania",
            API.PPYSB.Mode.AutoPilotFruits => "ap_ctb",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public static API.PPYSB.Mode? ParseKagamiPpysbMode(string? mode)
    {
        if (mode == null) return null;
        mode = mode.ToLower();
        return mode switch
        {
            "osu" => API.PPYSB.Mode.OSU,
            "taiko" => API.PPYSB.Mode.Taiko,
            "mania" => API.PPYSB.Mode.Mania,
            "ctb" => API.PPYSB.Mode.Fruits,
            "rx_osu" => API.PPYSB.Mode.RelaxOsu,
            "rx_taiko" => API.PPYSB.Mode.RelaxTaiko,
            "rx_mania" => API.PPYSB.Mode.RelaxMania,
            "rx_ctb" => API.PPYSB.Mode.RelaxFruits,
            "ap_osu" => API.PPYSB.Mode.AutoPilotOsu,
            "ap_taiko" => API.PPYSB.Mode.AutoPilotTaiko,
            "ap_mania" => API.PPYSB.Mode.AutoPilotMania,
            "ap_ctb" => API.PPYSB.Mode.AutoPilotFruits,
            _ => null
        };
    }

    public static Mode? ParseKagamiMode(string? mode)
    {
        if (mode == null) return null;
        mode = mode.ToLower();
        return mode switch
        {
            "osu" => Mode.OSU,
            "taiko" => Mode.Taiko,
            "mania" => Mode.Mania,
            "ctb" => Mode.Fruits,
            _ => null
        };
    }

}