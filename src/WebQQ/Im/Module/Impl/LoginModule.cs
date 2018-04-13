using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FclEx.Extensions;
using HttpAction.Event;
using HttpAction;
using WebQQ.Im.Actions;
using WebQQ.Im.Bean.Group;
using WebQQ.Im.Core;
using WebQQ.Im.Event;
using WebQQ.Im.Module.Interface;
using WebQQ.Util;

namespace WebQQ.Im.Module.Impl
{
    /// <summary>
    /// <para>��¼ģ�飬�����¼���˳�</para>
    /// </summary>
    public class LoginModule : QQModule, ILoginModule
    {
        public void BeginPoll()
        {
            new PollMsgAction(Context, (sender, @event) => // 1.��ȡ��ά��
            {
                if (@event.TryGet<List<QQNotifyEvent>>(out var notifyEvents))
                {
                    foreach (var notifyEvent in notifyEvents)
                    {
                        switch (notifyEvent.Type)
                        {
                            case QQNotifyEventType.NeedUpdateFriends:
                                new GetFriendsAction(Context, (a, e) =>
                                {
                                    if (@event.IsOk)
                                    {
                                        new GetOnlineFriendsAction(Context)
                                        .ExecuteAsyncAuto().Forget();
                                    }
                                    return Task.CompletedTask;
                                }).ExecuteAsyncAuto().Forget();
                                break;

                            case QQNotifyEventType.NeedUpdateGroups:
                                //new GetGroupNameListAction(Context, (s, e) =>
                                //{
                                //    if (e.IsOk)
                                //    {
                                //        Store.GroupDic.Values.ForEachAsync(m => new GetGroupInfoAction(Context, m)
                                //        .ExecuteAsyncAuto()).Forget();
                                //    }
                                //    return Task.CompletedTask;
                                //}).ExecuteAsyncAuto().Forget();

                                new GetGroupInfoAction(Context, notifyEvent.Target.CastTo<QQGroup>()).ExecuteAsyncAuto().Forget();
                                break;

                            default:
                                if (notifyEvent.Type >= 0) Context.FireNotifyAsync(notifyEvent).Forget(); // ��֪ͨ����0��
                                break;
                        }
                    }
                }
                return Task.CompletedTask;
            }).ExecuteForeverAsync(e => !Context.IsOnline()).Forget();
        }

        public LoginModule(IQQContext context) : base(context)
        {
        }

        public async Task<ActionEvent> Login(ActionEventListener listener)
        {
            Session.State = SessionState.Logining;

            var loginFutureResult = await new WebQQActionFuture(Context, listener)
             .PushAction<GetQRCodeAction>(async (sender, @event) => // 1.��ȡ��ά��
             {
                 if (!@event.IsOk) return;
                 await Context.FireNotifyAsync(QQNotifyEventType.QRCodeReady, @event.Target);
             })
             .PushAction<CheckQRCodeAction>(async (sender, @event) => // 2.��ȡ��ά��ɨ��״̬
             {
                 if (!@event.IsOk) return;

                 var args = (CheckQRCodeArgs)@event.Target;
                 switch (args.Status)
                 {
                     case QRCodeStatus.Ok:
                         Session.CheckSigUrl = args.Msg;
                         await Context.FireNotifyAsync(QQNotifyEventType.QRCodeSuccess);
                         break;

                     case QRCodeStatus.Valid:
                     case QRCodeStatus.Auth:
                         @event.Type = ActionEventType.EvtRepeat;
                         await Task.Delay(3000);
                         break;

                     case QRCodeStatus.Invalid:
                         await Context.FireNotifyAsync(QQNotifyEvent.CreateEvent(QQNotifyEventType.QRCodeInvalid, args.Msg));
                         @event.Type = ActionEventType.EvtCanceled;
                         break;
                 }
             })
             .PushAction<CheckSigAction>()
             .PushAction<GetVfwebqqAction>()
             .PushAction<ChannelLoginAction>(async (sender, @event) =>
             {
                 if (!@event.IsOk) return;
                 await Context.FireNotifyAsync(QQNotifyEvent.CreateEvent(QQNotifyEventType.LoginSuccess));
             }).ExecuteAsyncAuto();

            if (!loginFutureResult.IsOk)
            {
                Session.State = SessionState.Offline;
            }
            else
            {
                Session.State = SessionState.Online;
                await GetClientInfoAfterLogin(listener);
            }

            return loginFutureResult;
        }

        private Task<ActionEvent> GetClientInfoAfterLogin(ActionEventListener listener)
        {
            return new WebQQActionFuture(Context, listener)
                .PushAction<GetFriendsAction>(async (sender, @event) =>
                {
                    if (!@event.IsOk) return;
                    var obj = Store.FriendDic.FirstOrDefault().Value;
                    if (obj == null) return;
                    await new GetFriendLongNickAction(Context, obj).ExecuteAsyncAuto();
                    //await new GetFriendQQNumberAction(Context, obj).ExecuteAsyncAuto();
                    await new GetFriendInfoAction(Context, obj).ExecuteAsyncAuto();
                })
                .PushAction<GetGroupNameListAction>((sender, @event) =>
                {
                    if (@event.IsOk)
                    {
                        Store.GroupDic.Values.ForEachAsync(m => new GetGroupInfoAction(Context, m).ExecuteAsyncAuto()).Forget();
                    }
                    return Task.CompletedTask;
                })
                .PushAction<GetDiscussionListAction>((sender, @event) =>
                {
                    if (@event.IsOk)
                    {
                        Store.DiscussionDic.Values.ForEachAsync(m => new GetDiscussionInfoAction(Context, m).ExecuteAsyncAuto()).Forget();
                    }
                    return Task.CompletedTask;
                })
                .PushAction<GetSelfInfoAction>()
                .PushAction<GetOnlineFriendsAction>()
                .ExecuteAsyncAuto();
        }
    }
}
