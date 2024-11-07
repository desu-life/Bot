using System;
using System.Collections.Generic;
using System.Numerics;

namespace KanonBot;

public static partial class Utils
{
    private static readonly BigInteger ONE_HUNDRED_BILLION = BigInteger.Parse("100000000000");
    private static readonly BigInteger MAX_ALLOWED_SCORE = BigInteger.Parse("10000000000000000");

    public static BigInteger GetRequiredScoreForLevel(int level)
    {
        BigInteger score;
        if (level <= 100)
        {
            if (level > 1)
            {
                score = new BigInteger(
                    Math.Floor(
                        (5000.0 / 3) * (4 * Math.Pow(level, 3) - 3 * Math.Pow(level, 2) - level)
                            + Math.Floor(1.25 * Math.Pow(1.8, level - 60))
                    )
                );
            }
            else
            {
                score = BigInteger.One;
            }
        }
        else
        {
            score = BigInteger.Parse("26931190829") + ONE_HUNDRED_BILLION * level - 100;
        }

        return score;
    }

    public static int GetLevel(BigInteger score)
    {
        int left = 1;
        int right = 1_000_000;

        while (left < right)
        {
            int mid = (left + right + 1) / 2;
            BigInteger requiredScore = GetRequiredScoreForLevel(mid);

            if (score >= requiredScore)
            {
                left = mid;
            }
            else
            {
                right = mid - 1;
            }
        }

        return left - 1;
    }

    public static API.OSU.Models.UserLevel GetLevelWithProgress(BigInteger score)
    {
        if (score > MAX_ALLOWED_SCORE)
        {
            return new API.OSU.Models.UserLevel();
        }

        int baseLevel = GetLevel(score);
        BigInteger baseLevelScore = GetRequiredScoreForLevel(baseLevel);
        BigInteger scoreProgress = score - baseLevelScore;
        BigInteger scoreLevelDifference = GetRequiredScoreForLevel(baseLevel + 1) - baseLevelScore;

        double progress = (double)scoreProgress / (double)scoreLevelDifference;
        if (double.IsNaN(progress) || double.IsInfinity(progress))
        {
            progress = 0;
        }

        return new API.OSU.Models.UserLevel
        {
            Current = baseLevel,
            Progress = (int)Math.Round(progress * 100)
        };
    }
}
