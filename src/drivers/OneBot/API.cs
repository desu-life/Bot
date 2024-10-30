using System.Collections.Concurrent;
using DotNext.Threading;
using KanonBot.Message;
using KanonBot.Serializer;
using LanguageExt.UnsafeValueAccess;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
namespace KanonBot.Drivers;
public partial class OneBot
{
    // API 部分 * 包装 Driver
    public class API
    {
        readonly ISocket socket;
        public API(ISocket socket)
        {
            this.socket = socket;
        }

        #region 消息收发
        public class RetCallback
        {
            public AsyncManualResetEvent ResetEvent { get; } = new AsyncManualResetEvent(false);
            public Models.CQResponse? Data { get; set; }
        }

        private AsyncExclusiveLock l = new();
        public ConcurrentDictionary<Guid, RetCallback> CallbackList = new();
        public async Task Echo(Models.CQResponse res)
        {
            using(await l.AcquireLockAsync(CancellationToken.None))
            {
                this.CallbackList[res.Echo].Data = res;
                this.CallbackList[res.Echo].ResetEvent.Set();
            }
        }
        private async Task<Models.CQResponse> Send(Models.CQRequest req)
        {
            using(await l.AcquireLockAsync(CancellationToken.None))
            {
                // 创建回调
                this.CallbackList[req.Echo] = new RetCallback();
            }

            // 发送
            await this.socket.SendAsync(req);
            await this.CallbackList[req.Echo].ResetEvent.WaitAsync();

            RetCallback ret;
            using(await l.AcquireLockAsync(CancellationToken.None)) {
                // 获取并移除回调
                this.CallbackList.Remove(req.Echo, out ret!);
            }
            return ret.Data!;
        }
        #endregion

        // 发送群消息
        public async Task<long?> SendGroupMessage(long groupId, Chain msgChain)
        {
            var message = Message.Build(msgChain);
            var req = new Models.CQRequest
            {
                action = Enums.Actions.SendMsg,
                Params = new Models.SendMessage
                {
                    MessageType = Enums.MessageType.Group,
                    GroupId = groupId,
                    Message = message,
                    AutoEscape = false
                },
            };

            var res = await this.Send(req);
            if (res.Status == "ok")
                return (long)res.Data["message_id"]!;
            else
                return null;
        }

        // 发送私聊消息
        public async Task<long?> SendPrivateMessage(long userId, Chain msgChain)
        {
            var message = Message.Build(msgChain);
            var req = new Models.CQRequest
            {
                action = Enums.Actions.SendMsg,
                Params = new Models.SendMessage
                {
                    MessageType = Enums.MessageType.Private,
                    UserId = userId,
                    Message = message,
                    AutoEscape = false
                },
            };

            var res = await this.Send(req);
            if (res.Status == "ok")
                return (long)res.Data["message_id"]!;
            else
                return null;
        }

    }
}
