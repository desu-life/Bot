using KanonBot.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KanonBot.Message;

public interface IMsgSegment
{
    string Build();
}
