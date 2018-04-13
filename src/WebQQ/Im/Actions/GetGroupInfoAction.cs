﻿using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FclEx.Extensions;
using HttpAction.Core;
using HttpAction.Event;
using HttpAction;
using Newtonsoft.Json.Linq;
using WebQQ.Im.Bean;
using WebQQ.Im.Bean.Group;
using WebQQ.Im.Core;

namespace WebQQ.Im.Actions
{
    public class GetGroupInfoAction : WebQQInfoAction
    {
        private readonly QQGroup _group;

        public GetGroupInfoAction(IQQContext context, QQGroup group, ActionEventListener listener = null) : base(context, listener)
        {
            _group = group;
        }

        protected override EnumRequestType RequestType { get; } = EnumRequestType.Get;

        protected override void ModifyRequest(HttpRequestItem req)
        {
            req.AddData("gcode", _group.Code);
            req.AddData("vfwebqq", Session.Vfwebqq);
            req.AddData("t", Timestamp);
            req.Referrer = ApiUrls.ReferrerS;
        }

        protected override void HandleResult(JToken json)
        {
            /*
                {
                    "retcode": 0,
                    "result": {
                        "stats": [
                            {
                                "client_type": 1,
                                "uin": 2146674552,
                                "stat": 50
                            }
                        ],
                        "minfo": [
                            {
                                "nick": "昵称",
                                "province": "北京",
                                "gender": "male",
                                "uin": 3623536468,
                                "country": "中国",
                                "city": ""
                            }
                        ],
                        "ginfo": {
                            "face": 0,
                            "memo": "群公告！",
                            "class": 25,
                            "fingermemo": "",
                            "code": 591539174,
                            "createtime": 1231435199,
                            "flag": 721421329,
                            "level": 4,
                            "name": "群名称",
                            "gid": 2419762790,
                            "owner": 3509557797,
                            "members": [
                                {
                                    "muin": 3623536468,
                                    "mflag": 192
                                }
                            ],
                            "option": 2
                        },
                        "cards": [
                            {
                                "muin": 3623536468,
                                "card": "群名片"
                            }
                        ],
                        "vipinfo": [
                            {
                                "vip_level": 6,
                                "u": 2390929289,
                                "is_vip": 1
                            }
                        ]
                    }
                } 
            */
            var result = json["result"];
            var groupInfo = result["ginfo"].ToObject<GroupInfo>();
            var members = result["ginfo"]["members"].ToObject<GroupMember[]>().Distinct(m => m.Uin).ToDictionary(m => m.Uin, m => m);

            // 成员信息
            result["minfo"].ToObject<GroupMemberInfo[]>().ForEach(m =>
            {
                members.GetAndDo(m.Uin, member => Mapper.Map(m, member));
            });

            // 成员状态
            result["stats"]?.ToObject<UserStatus[]>().ForEach(m =>
            {
                members.GetAndDo(m.Uin, member => Mapper.Map(m, member));
            });

            // 成员名片
            result["cards"]?.ToObject<GroupMemberCard[]>().ForEach(card =>
            {
                members.GetAndDo(card.Uin, member => Mapper.Map(card, member));
            });

            // vip信息
            result["vipinfo"]?.ToObject<UserVipInfo[]>().ForEach(m =>
            {
                members.GetAndDo(m.Uin, member => Mapper.Map(m, member));
            });

            groupInfo.MapTo(_group);
            _group.Members.ReplaceBy(members);
        }
    }
}
