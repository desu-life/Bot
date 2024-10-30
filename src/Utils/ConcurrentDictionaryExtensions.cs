#pragma warning disable CS8714 // 类型不能用作泛型类型或方法中的类型参数。类型参数的为 Null 性与 "notnull" 约束不匹配。
using System;
using System.Threading.Tasks;

namespace System.Collections.Concurrent
{
    public static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// Provides an alternative to <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/> that disposes values that implement <see cref="IDisposable"/>.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        public static TValue GetOrAddWithDispose<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> valueFactory) where TValue : IDisposable
        {
            while (true)
            {
                if (dictionary.TryGetValue(key, out var value))
                {
                    // Try to get the value
                    return value;
                }

                /// Try to add the value
                value = valueFactory(key);
                if (dictionary.TryAdd(key, value))
                {
                    // Won the race, so return the instance
                    return value;
                }

                // Lost the race, dispose the created object
                value.Dispose();
            }
        }


        /// <summary>
        /// Provides an alternative to <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/> specifically for asynchronous values. The factory method will only run once.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
            this ConcurrentDictionary<TKey, Task<TValue>> dictionary,
            TKey key,
            Func<TKey, Task<TValue>> valueFactory)
        {
            while (true)
            {
                if (dictionary.TryGetValue(key, out var task))
                {
                    return await task;
                }

                // This is the task that we'll return to all waiters. We'll complete it when the factory is complete
                var tcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (dictionary.TryAdd(key, tcs.Task))
                {
                    try
                    {
                        var value = await valueFactory(key);
                        tcs.TrySetResult(value);
                        return await tcs.Task;
                    }
                    catch (Exception ex)
                    {
                        // Make sure all waiters see the exception
                        tcs.SetException(ex);

                        // We remove the entry if the factory failed so it's not a permanent failure
                        // and future gets can retry (this could be a pluggable policy)
                        dictionary.TryRemove(key, out _);
                        throw;
                    }
                }
            }
        }
    }
}