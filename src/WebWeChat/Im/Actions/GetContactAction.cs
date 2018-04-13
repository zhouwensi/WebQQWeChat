﻿using System.ComponentModel;
using System.Threading.Tasks;
using FclEx.Extensions;
using HttpAction.Core;
using HttpAction.Event;
using Microsoft.Extensions.Logging;
using WebWeChat.Im.Bean;
using WebWeChat.Im.Core;

namespace WebWeChat.Im.Actions
{
    /// <summary>
    /// 获取好友列表
    /// </summary>
    [Description("获取好友列表")]
    public class GetContactAction : WebWeChatAction
    {
        public GetContactAction(IWeChatContext context, ActionEventListener listener = null)
            : base(context, listener)
        {
        }

        protected override HttpRequestItem BuildRequest()
        {
            var url = string.Format(ApiUrls.GetContact, Session.BaseUrl, Session.PassTicket, Session.Skey, Timestamp);
            var obj = new { Session.BaseRequest };
            var req = new HttpRequestItem(HttpMethodType.Post, url)
            {
                StringData = obj.ToJson(),
                ContentType = HttpConstants.JsonContentType
            };
            return req;
        }

        protected override Task<ActionEvent> HandleResponse(HttpResponseItem responseItem)
        {
            /*
                {
                    "BaseResponse": {
                        "Ret": 0,
                        "ErrMsg": ""
                    },
                    "MemberCount": 334,
                    "MemberList": [
                        {
                            "Uin": 0,
                            "UserName": xxx,
                            "NickName": "Urinx",
                            "HeadImgUrl": xxx,
                            "ContactFlag": 3,
                            "MemberCount": 0,
                            "MemberList": [],
                            "RemarkName": "",
                            "HideInputBarFlag": 0,
                            "Sex": 0,
                            "Signature": "我是二蛋",
                            "VerifyFlag": 8,
                            "OwnerUin": 0,
                            "PYInitial": "URINX",
                            "PYQuanPin": "Urinx",
                            "RemarkPYInitial": "",
                            "RemarkPYQuanPin": "",
                            "StarFriend": 0,
                            "AppAccountFlag": 0,
                            "Statues": 0,
                            "AttrStatus": 0,
                            "Province": "",
                            "City": "",
                            "Alias": "Urinxs",
                            "SnsFlag": 0,
                            "UniFriend": 0,
                            "DisplayName": "",
                            "ChatRoomId": 0,
                            "KeyWord": "gh_",
                            "EncryChatRoomId": ""
                        },
                        ...
                    ],
                    "Seq": 0
                }
            */
            var json = responseItem.ResponseString.ToJToken();
            if (json["BaseResponse"]["Ret"].ToString() == "0")
            {
                Store.MemberCount = json["MemberCount"].ToObject<int>();
                var list = json["MemberList"].ToObject<ContactMember[]>();
                Store.ContactMemberDic.ReplaceBy(list, m => m.UserName);

                var selfName = Session.UserToken["UserName"].ToString();
                Session.User = Store.ContactMemberDic[selfName];
                // Store.ContactMemberDic.Remove(selfName);

                Logger.LogInformation($"应有{Store.MemberCount}个联系人，读取到联系人{Store.ContactMemberDic.Count}个");
                Logger.LogInformation($"共有{Store.GroupCount}个群|{Store.FriendCount}个好友|{Store.SpecialUserCount}个特殊账号|{Store.PublicUserCount}公众号或服务号");
                return NotifyOkEventAsync();
            }
            else
            {
                throw new WeChatException(WeChatErrorCode.ResponseError, responseItem.ResponseString);
            }
        }
    }
}
