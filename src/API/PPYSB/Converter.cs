using KanonBot.API.OSU;
using KanonBot.OsuPerformance;
using KanonBot.Serializer;

namespace KanonBot.API.PPYSB;

public static class PPYSBConverters
{
    public static OSU.Models.Status ToOsu(this Models.Status status)
    {
        return status switch
        {
            Models.Status.NotSubmitted => OSU.Models.Status.Unknown,
            Models.Status.UpdateAvailable => OSU.Models.Status.Unknown,
            Models.Status.Pending => OSU.Models.Status.Pending,
            Models.Status.Ranked => OSU.Models.Status.Ranked,
            Models.Status.Approved => OSU.Models.Status.Approved,
            Models.Status.Qualified => OSU.Models.Status.Qualified,
            Models.Status.Loved => OSU.Models.Status.Loved,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static OSU.Mode ToOsu(this Mode mode) {
        return mode switch
        {
            Mode.OSU => OSU.Mode.OSU,
            Mode.Taiko => OSU.Mode.Taiko,
            Mode.Fruits => OSU.Mode.Fruits,
            Mode.Mania => OSU.Mode.Mania,
            Mode.RelaxOsu => OSU.Mode.OSU,
            Mode.RelaxTaiko => OSU.Mode.Taiko,
            Mode.RelaxFruits => OSU.Mode.Fruits,
            Mode.RelaxMania => OSU.Mode.Mania,
            Mode.AutoPilotOsu => OSU.Mode.OSU,
            Mode.AutoPilotTaiko => OSU.Mode.Taiko,
            Mode.AutoPilotFruits => OSU.Mode.Fruits,
            Mode.AutoPilotMania => OSU.Mode.Mania,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static Mode ToSb(this OSU.Mode mode) {
        return mode switch
        {
            OSU.Mode.OSU => Mode.OSU,
            OSU.Mode.Taiko => Mode.Taiko,
            OSU.Mode.Fruits => Mode.Fruits,
            OSU.Mode.Mania => Mode.Mania,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static OSU.Models.UserStatistics ToOsu(this Models.UserStat u) {
        return new OSU.Models.UserStatistics
        {
            TotalScore = u.TotalScore,
            RankedScore = u.RankedScore,
            PP = u.PP,
            PlayTime = u.PlayTime,
            PlayCount = u.Plays,
            HitAccuracy = u.Accuracy,
            MaximumCombo = u.MaxCombo,
            TotalHits = u.TotalHits,
            ReplaysWatchedByOthers = u.ReplayViews,
            GlobalRank = u.Rank,
            CountryRank = u.CountryRank,
            Level = Utils.GetLevelWithProgress(u.TotalScore),
            GradeCounts = new OSU.Models.UserGradeCounts {
                SS = u.XCount,
                SSH = u.XhCount,
                S = u.SCount,
                SH = u.ShCount,
                A = u.ACount
            }
        };
    }

    public static OSU.Models.UserExtended? ToOsu(this Models.User u, Mode? mode) {
        mode ??= u.Info.PreferredMode;
        
        var userStat = mode switch {
            Mode.OSU => u.Stats.StatOsu,
            Mode.Taiko => u.Stats.StatTaiko,
            Mode.Fruits => u.Stats.StatFruits,
            Mode.Mania => u.Stats.StatMania,
            Mode.RelaxOsu => u.Stats.StatRelaxOsu,
            Mode.RelaxTaiko => u.Stats.StatRelaxTaiko,
            Mode.RelaxFruits => u.Stats.StatRelaxFruits,
            Mode.RelaxMania => null,
            Mode.AutoPilotOsu => u.Stats.StatAutoPilotOsu,
            Mode.AutoPilotTaiko => null,
            Mode.AutoPilotFruits => null,
            Mode.AutoPilotMania => null,
            _ => throw new ArgumentOutOfRangeException(),
        };

        if (userStat == null) return null;
        
        return new OSU.Models.UserExtended
        {
            Id = u.Info.Id,
            Username = u.Info.Name,
            AvatarUrl = new Uri($"https://a.ppy.sb/{u.Info.Id}"),
            CountryCode = u.Info.Country,
            Country = new OSU.Models.Country { Code = u.Info.Country, Name = u.Info.Country },
            JoinDate = u.Info.CreationTime,
            LastVisit = u.Info.LatestActivity,
            Mode = u.Info.PreferredMode.ToOsu(),
            StatisticsCurrent = userStat.ToOsu(),
        };
    }

    public static OSU.Models.ScoreLazer ToOsu(this Models.Score s, Models.User user, Mode mode) {
        var rmods = RosuPP.Mods.FromBits(s.Mods, mode.ToOsu().ToRosu());
        var js = RosuPP.OwnedString.Empty();
        rmods.Json(ref js);
        var mods = Json.Deserialize<List<OSU.Models.Mod>>(js.ToCstr());
        mods?.Add(OSU.Models.Mod.FromString("CL"));

        OSU.Models.ScoreStatisticsLazer statistics = new OSU.Models.ScoreStatistics { 
            CountGreat = s.Count300,
            CountKatu = s.CountKatu,
            CountMeh = s.Count50,
            CountOk = s.Count100,
            CountGeki = s.CountGeki,
            CountMiss = s.CountMiss,
        };

        var bm = s.Beatmap.ToOsu();

        return new OSU.Models.ScoreLazer
        {
            Accuracy = statistics.Accuracy(mode.ToOsu()),
            EndedAt = s.PlayTime,
            Id = s.Id,
            MaxCombo = s.MaxCombo,
            ModeInt = mode.ToOsu().ToNum(),
            Mods = mods?.ToArray() ?? [],
            Passed = s.Rank != "F",
            pp = s.PP,
            Rank = s.Rank,
            HasReplay = false,
            Score = 0,
            LegacyTotalScore = s.Scores,
            Statistics = statistics,
            UserId = user.Info.Id,
            BeatmapId = s.Beatmap.BeatmapId,
            LegacyScoreId = s.Id,
            Beatmap = bm,
            Beatmapset = bm.Beatmapset,
            User = null,
            Weight = null,
            ConvertFromOld = true
        };
    }

    public static OSU.Models.Beatmap ToOsu(this Models.Beatmap b) {
        var mode = b.Mode.ToOsu();
        return new OSU.Models.Beatmap
        {
            BeatmapId = b.BeatmapId,
            BeatmapsetId = b.BeatmapsetId,
            Checksum = b.Md5,
            Version = b.Version,
            LastUpdated = b.LastUpdate,
            TotalLength = b.TotalLength,
            MaxCombo = b.MaxCombo,
            Status = b.Status.ToOsu(),
            Playcount = b.Plays,
            Passcount = b.Passes,
            Mode = mode,
            ModeInt = mode.ToNum(),
            BPM = b.BPM,
            CS = b.CS,
            OD = b.OD,
            AR = b.AR,
            HPDrain = b.HP,
            DifficultyRating = b.DifficultyRating,
            Beatmapset = new OSU.Models.Beatmapset
            {
                Artist = b.Artist,
                ArtistUnicode = b.Artist,
                Title = b.Title,
                TitleUnicode = b.Title,
                Creator = b.Creator,
                Id = b.BeatmapsetId,
            }
        };
    }

}