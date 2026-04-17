using System.Collections.Concurrent;
using KanonBot.Drivers;

namespace KanonBot.Functions;

public class HistoryBeatmapMapper
{
    class CacheItem
    {
        public long BeatmapID { get; set; }
        public DateTime CachedAt { get; set; }
    }

    private static ConcurrentDictionary<MessageSource, CacheItem> _beatmapCache = new();
    public static void Map(MessageSource source, long beatmapID)
    {
        _beatmapCache[source] = new CacheItem
        {
            BeatmapID = beatmapID,
            CachedAt = DateTime.Now
        };
    }

    public static long? Get(MessageSource source)
    {
        if (_beatmapCache.TryGetValue(source, out var item))
        {
            // 如果缓存时间超过10分钟，认为过期
            if ((DateTime.Now - item.CachedAt).TotalMinutes < 10)
            {
                return item.BeatmapID;
            }
            else
            {
                _beatmapCache.TryRemove(source, out _);
            }
        }
        return null;
    }
}