namespace desu_life_Bot;

public static partial class Utils
{
    public static async Task<Option<T>> TimeOut<T>(this Task<T> task, TimeSpan delay)
    {
        var timeOutTask = Task.Delay(delay); // 设定超时任务
        var doing = await Task.WhenAny(task, timeOutTask); // 返回任何一个完成的任务
        if (doing == timeOutTask) // 如果超时任务先完成了 就返回none
            return None;
        return Some<T>(await task);
    }
}