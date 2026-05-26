namespace KanonBot.Drivers;

public partial class OneBot
{
    public static readonly Platform platform = Platform.OneBot;
    event IDriver.MessageDelegate? msgAction;
    event IDriver.EventDelegate? eventAction;
}
