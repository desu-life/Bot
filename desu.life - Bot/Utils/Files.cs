namespace desu_life_Bot;

public static partial class Utils
{
    public static Stream Byte2Stream(byte[] buffer)
    {
        var stream = new MemoryStream(buffer);
        //设置 stream 的 position 为流的开始
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    public static string Byte2File(string fileName, byte[] buffer)
    {
        using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        {
            fs.Write(buffer, 0, buffer.Length);
        }
        return Path.GetFullPath(fileName);
        ;
    }

    public static Stream LoadFile2ReadStream(string filePath)
    {
        var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return fs;
    }

    public static async Task<byte[]> LoadFile2Byte(string filePath)
    {
        using var fs = LoadFile2ReadStream(filePath);
        byte[] bt = new byte[fs.Length];
        var mem = new Memory<Byte>(bt);
        await fs.ReadAsync(mem);
        fs.Close();
        return mem.ToArray();
    }
}