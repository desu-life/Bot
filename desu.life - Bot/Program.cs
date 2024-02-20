using static desu.life_Bot.Initializer;
using static desu.life_Bot.Core.ServiceManager;

// 初始化
await InitializationAsync();

// 启动服务
RunService();

// 关闭日志
Log.CloseAndFlush();