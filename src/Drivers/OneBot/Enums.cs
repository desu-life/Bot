using System.ComponentModel;
using System.Text.Json.Serialization;

namespace KanonBot.Drivers;

public partial class OneBot
{
    public class Enums
    {
        /// <summary>
        /// API集合
        /// </summary>
        [DefaultValue(Unknown)]
        [JsonConverter(typeof(JsonStringEnumConverter<Actions>))]
        public enum Actions
        {
            /// <summary>
            /// 未知，在转换类型错误时为此值
            /// </summary>
            Unknown,

            #region OnebotAPI

            /// <summary>
            /// 发送消息
            /// </summary>
            [JsonStringEnumMemberName("send_msg")]
            SendMsg,

            /// <summary>
            /// 获取登录号信息
            /// </summary>
            [JsonStringEnumMemberName("get_login_info")]
            GetLoginInfo,

            /// <summary>
            /// 获取版本信息
            /// </summary>
            [JsonStringEnumMemberName("get_version_info")]
            GetVersion,

            /// <summary>
            /// 撤回消息
            /// </summary>
            [JsonStringEnumMemberName("delete_msg")]
            RecallMsg,

            /// <summary>
            /// 获取好友列表
            /// </summary>
            [JsonStringEnumMemberName("get_friend_list")]
            GetFriendList,

            /// <summary>
            /// 获取群列表
            /// </summary>
            [JsonStringEnumMemberName("get_group_list")]
            GetGroupList,

            /// <summary>
            /// 获取群成员信息
            /// </summary>
            [JsonStringEnumMemberName("get_group_info")]
            GetGroupInfo,

            /// <summary>
            /// 获取群成员信息
            /// </summary>
            [JsonStringEnumMemberName("get_group_member_info")]
            GetGroupMemberInfo,

            /// <summary>
            /// 获取陌生人信息
            /// </summary>
            [JsonStringEnumMemberName("get_stranger_info")]
            GetStrangerInfo,

            /// <summary>
            /// 获取群成员列表
            /// </summary>
            [JsonStringEnumMemberName("get_group_member_list")]
            GetGroupMemberList,

            /// <summary>
            /// 处理加好友请求
            /// </summary>
            [JsonStringEnumMemberName("set_friend_add_request")]
            SetFriendAddRequest,

            /// <summary>
            /// 处理加群请求/邀请
            /// </summary>
            [JsonStringEnumMemberName("set_group_add_request")]
            SetGroupAddRequest,

            /// <summary>
            /// 设置群名片
            /// </summary>
            [JsonStringEnumMemberName("set_group_card")]
            SetGroupCard,

            /// <summary>
            /// 设置群组专属头衔
            /// </summary>
            [JsonStringEnumMemberName("set_group_special_title")]
            SetGroupSpecialTitle,

            /// <summary>
            /// 群组T人
            /// </summary>
            [JsonStringEnumMemberName("set_group_kick")]
            SetGroupKick,

            /// <summary>
            /// 群组单人禁言
            /// </summary>
            [JsonStringEnumMemberName("set_group_ban")]
            SetGroupBan,

            /// <summary>
            /// 群全体禁言
            /// </summary>
            [JsonStringEnumMemberName("set_group_whole_ban")]
            SetGroupWholeBan,

            /// <summary>
            /// 群组匿名用户禁言
            /// </summary>
            [JsonStringEnumMemberName("set_group_anonymous_ban")]
            SetGroupAnonymousBan,

            /// <summary>
            /// 设置群管理员
            /// </summary>
            [JsonStringEnumMemberName("set_group_admin")]
            SetGroupAdmin,

            /// <summary>
            /// 群退出
            /// </summary>
            [JsonStringEnumMemberName("set_group_leave")]
            SetGroupLeave,

            /// <summary>
            /// 是否可以发送图片
            /// </summary>
            [JsonStringEnumMemberName("can_send_image")]
            CanSendImage,

            /// <summary>
            /// 是否可以发送语音
            /// </summary>
            [JsonStringEnumMemberName("can_send_record")]
            CanSendRecord,

            /// <summary>
            /// 获取插件运行状态
            /// </summary>
            [JsonStringEnumMemberName("get_status")]
            GetStatus,

            /// <summary>
            /// 重启客户端
            /// </summary>
            [JsonStringEnumMemberName("set_restart")]
            Restart,

            #endregion

            #region GoCQ API

            /// <summary>
            /// 获取图片信息
            /// </summary>
            [JsonStringEnumMemberName("get_image")]
            GetImage,

            /// <summary>
            /// 获取消息
            /// </summary>
            [JsonStringEnumMemberName("get_msg")]
            GetMessage,

            /// <summary>
            /// 设置群名
            /// </summary>
            [JsonStringEnumMemberName("set_group_name")]
            SetGroupName,

            /// <summary>
            /// 获取合并转发消息
            /// </summary>
            [JsonStringEnumMemberName("get_forward_msg")]
            GetForwardMessage,

            /// <summary>
            /// 发送合并转发(群)
            /// </summary>
            [JsonStringEnumMemberName("send_group_forward_msg")]
            SendGroupForwardMsg,

            /// <summary>
            /// 设置群头像
            /// </summary>
            [JsonStringEnumMemberName("set_group_portrait")]
            SetGroupPortrait,

            /// <summary>
            /// 获取群系统消息
            /// </summary>
            [JsonStringEnumMemberName("get_group_system_msg")]
            GetGroupSystemMsg,

            /// <summary>
            /// 获取中文分词
            /// </summary>
            [JsonStringEnumMemberName(".get_word_slices")]
            GetWordSlices,

            /// <summary>
            /// 获取群文件系统信息
            /// </summary>
            [JsonStringEnumMemberName("get_group_file_system_info")]
            GetGroupFileSystemInfo,

            /// <summary>
            /// 获取群根目录文件列表
            /// </summary>
            [JsonStringEnumMemberName("get_group_root_files")]
            GetGroupRootFiles,

            /// <summary>
            /// 获取群子目录文件列表
            /// </summary>
            [JsonStringEnumMemberName("get_group_files_by_folder")]
            GetGroupFilesByFolder,

            /// <summary>
            /// 获取群文件资源链接
            /// </summary>
            [JsonStringEnumMemberName("get_group_file_url")]
            GetGroupFileUrl,

            /// <summary>
            /// 获取群@全体成员剩余次数
            /// </summary>
            [JsonStringEnumMemberName("get_group_at_all_remain")]
            GetGroupAtAllRemain,

            /// <summary>
            /// 调用腾讯的OCR接口
            /// </summary>
            [JsonStringEnumMemberName("ocr_image")]
            Ocr,

            /// <summary>
            /// 下载文件到缓存目录
            /// </summary>
            [JsonStringEnumMemberName("download_file")]
            DownloadFile,

            /// <summary>
            /// 获取群消息历史记录
            /// </summary>
            [JsonStringEnumMemberName("get_group_msg_history")]
            GetGroupMsgHistory,

            /// <summary>
            /// 获取当前账号在线客户端列表
            /// </summary>
            [JsonStringEnumMemberName("get_online_clients")]
            GetOnlineClients,

            /// <summary>
            /// 重载事件过滤器
            /// </summary>
            [JsonStringEnumMemberName("reload_event_filter")]
            ReloadEventFilter,

            /// <summary>
            /// 上传群文件
            /// </summary>
            [JsonStringEnumMemberName("upload_group_file")]
            UploadGroupFile,

            /// <summary>
            /// 设置精华消息
            /// </summary>
            [JsonStringEnumMemberName("set_essence_msg")]
            SetEssenceMsg,

            /// <summary>
            /// 移出精华消息
            /// </summary>
            [JsonStringEnumMemberName("delete_essence_msg")]
            DeleteEssenceMsg,

            /// <summary>
            /// 获取精华消息列表
            /// </summary>
            [JsonStringEnumMemberName("get_essence_msg_list")]
            GetEssenceMsgList,

            /// <summary>
            /// 检查链接安全性
            /// </summary>
            [JsonStringEnumMemberName("check_url_safely")]
            CheckUrlSafely,

            /// <summary>
            /// 发送群公告
            /// </summary>
            [JsonStringEnumMemberName("_send_group_notice")]
            SendGroupNotice,

            /// <summary>
            /// 获取企点账号信息
            /// </summary>
            [JsonStringEnumMemberName("qidian_get_account_info")]
            GetQidianAccountInfo,

            /// <summary>
            /// 主动删除好友
            /// </summary>
            [JsonStringEnumMemberName("delete_friend")]
            DeleteFriend,

            /// <summary>
            /// 获取好友在线机型展示信息
            /// </summary>
            [JsonStringEnumMemberName("_get_model_show")]
            GetModelShow,

            /// <summary>
            /// 设置好友在线机型展示信息
            /// </summary>
            [JsonStringEnumMemberName("_set_model_show")]
            SetModelShow,

            /// <summary>
            /// 新建群文件文件夹
            /// </summary>
            [JsonStringEnumMemberName("create_group_file_folder")]
            CreateGroupFileFolder,

            /// <summary>
            /// 删除群文件文件夹
            /// </summary>
            [JsonStringEnumMemberName("delete_group_folder")]
            DeleteGroupFolder,

            /// <summary>
            /// 删除群文件
            /// </summary>
            [JsonStringEnumMemberName("delete_group_file")]
            DeleteGroupFile,

            /// <summary>
            /// 标记消息已读
            /// </summary>
            [JsonStringEnumMemberName("mark_msg_as_read")]
            MarkMsgAsRead,

            /// <summary>
            /// 获取单向好友列表
            /// </summary>
            [JsonStringEnumMemberName("get_unidirectional_friend_list")]
            GetUnidirectionalFriendList,

            /// <summary>
            /// 获取单向好友列表
            /// </summary>
            [JsonStringEnumMemberName("delete_unidirectional_friend")]
            DeleteUnidirectionalFriend,

            #endregion
        }

        /// <summary>
        /// 群请求类型
        /// </summary>
        [DefaultValue(Unknown)]
        [JsonConverter(typeof(JsonStringEnumConverter<GroupRequestType>))]
        public enum GroupRequestType
        {
            /// <summary>
            /// 未知，在转换错误时为此值
            /// </summary>
            Unknown,

            /// <summary>
            /// 添加
            /// </summary>
            [JsonStringEnumMemberName("add")]
            Add,

            /// <summary>
            /// 邀请
            /// </summary>
            [JsonStringEnumMemberName("invite")]
            Invite
        }

        /// <summary>
        /// 群角色
        /// </summary>
        [DefaultValue(Unknown)]
        [JsonConverter(typeof(JsonStringEnumConverter<GroupRole>))]
        public enum GroupRole
        {
            /// <summary>
            /// 未知，在转换错误时为此值
            /// </summary>
            Unknown,

            /// <summary>
            /// 群员
            /// </summary>
            [JsonStringEnumMemberName("member")]
            Member,

            /// <summary>
            /// 群主
            /// </summary>
            [JsonStringEnumMemberName("owner")]
            Owner,

            /// <summary>
            /// 管理
            /// </summary>
            [JsonStringEnumMemberName("admin")]
            Admin
        }

        /// <summary>
        /// 消息类型
        /// </summary>
        [DefaultValue(Unknown)]
        [JsonConverter(typeof(JsonStringEnumConverter<MessageType>))]
        public enum MessageType
        {
            /// <summary>
            /// 未知，在转换错误时为此值
            /// </summary>
            Unknown,

            /// <summary>
            /// 私聊消息
            /// </summary>
            [JsonStringEnumMemberName("private")]
            Private,

            /// <summary>
            /// 群消息
            /// </summary>
            [JsonStringEnumMemberName("group")]
            Group
        }

        /// <summary>
        /// 消息段类型
        /// </summary>
        [DefaultValue(Unknown)]
        [JsonConverter(typeof(JsonStringEnumConverter<SegmentType>))]
        public enum SegmentType
        {
            /// <summary>
            /// 未知
            /// </summary>
            Unknown,

            /// <summary>
            /// 忽略
            /// </summary>
            Ignore,

            #region 基础消息段

            /// <summary>
            /// 纯文本
            /// </summary>
            [JsonStringEnumMemberName("text")]
            Text,

            /// <summary>
            /// QQ 表情
            /// </summary>
            [JsonStringEnumMemberName("face")]
            Face,

            /// <summary>
            /// 图片
            /// </summary>
            [JsonStringEnumMemberName("image")]
            Image,

            /// <summary>
            /// 语音
            /// </summary>
            [JsonStringEnumMemberName("record")]
            Record,

            /// <summary>
            /// 短视频
            /// </summary>
            [JsonStringEnumMemberName("video")]
            Video,

            /// <summary>
            /// <para>音乐分享</para>
            /// <para>只能发送</para>
            /// </summary>
            [JsonStringEnumMemberName("music")]
            Music,

            /// <summary>
            /// @某人
            /// </summary>
            [JsonStringEnumMemberName("at")]
            At,

            /// <summary>
            /// 链接分享
            /// </summary>
            [JsonStringEnumMemberName("share")]
            Share,

            /// <summary>
            /// 回复
            /// </summary>
            [JsonStringEnumMemberName("reply")]
            Reply,

            /// <summary>
            /// <para>合并转发</para>
            /// <para>只能接收</para>
            /// </summary>
            [JsonStringEnumMemberName("forward")]
            Forward,

            #endregion

            #region GoCQ扩展消息段

            /// <summary>
            /// 群戳一戳
            /// </summary>
            [JsonStringEnumMemberName("poke")]
            Poke,

            /// <summary>
            /// XML 消息
            /// </summary>
            [JsonStringEnumMemberName("xml")]
            Xml,

            /// <summary>
            /// JSON 消息
            /// </summary>
            [JsonStringEnumMemberName("json")]
            Json,

            /// <summary>
            /// 接收红包
            /// </summary>
            [JsonStringEnumMemberName("redbag")]
            RedBag,

            /// <summary>
            /// 装逼大图
            /// </summary>
            [JsonStringEnumMemberName("cardimage")]
            CardImage,

            /// <summary>
            /// 文本转语音
            /// </summary>
            [JsonStringEnumMemberName("tts")]
            TTS

            #endregion
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum PostType
        {
            [JsonStringEnumMemberName("message")]
            Message,

            [JsonStringEnumMemberName("notice")]
            Notice,

            [JsonStringEnumMemberName("request")]
            Request,

            [JsonStringEnumMemberName("meta_event")]
            MetaEvent
        }

        public enum MetaEventType
        {
            Unknown,

            [JsonStringEnumMemberName("heartbeat")]
            Heartbeat,

            [JsonStringEnumMemberName("lifecycle")]
            Lifecycle
        }
    }
}
