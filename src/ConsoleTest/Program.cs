﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using FclEx.Extensions;
using HttpAction;
using HttpAction.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebQQ.Im;
using WebQQ.Im.Bean.Friend;
using WebQQ.Im.Bean.Group;
using WebQQ.Im.Event;
using WebQQ.Im.Service.Impl;
using WebQQ.Util;
using WebWeChat.Im;
using WebWeChat.Im.Bean;
using WebWeChat.Im.Event;
using WebWeChat.Im.Service.Impl;
using ImageSharp;
using ImageSharp.Processing;
using SixLabors.Primitives;
using WebIm.Utils;

namespace ConsoleTest
{
    /// <summary>
    /// 使用二维码登录WebQQ
    /// </summary>
    public class Program
    {
        private static readonly QQNotifyEventListener _qqListener = (client, notifyEvent) =>
        {
            var logger = client.GetSerivce<ILogger>();
            switch (notifyEvent.Type)
            {
                case QQNotifyEventType.LoginSuccess:
                    logger.LogInformation("登录成功");
                    break;

                case QQNotifyEventType.QRCodeReady:
                    {
                        var verify = notifyEvent.Target.CastTo<Image<Rgba32>>();
                        verify.ConsoleWrite();
                        const string path = "verify.jpg";
                        verify.Save(path);
                        logger.LogInformation("请扫描显示在控制台的二维码或者保存在项目根目录的二维码图片");
                        break;
                    }

                case QQNotifyEventType.QRCodeSuccess:
                    break;

                case QQNotifyEventType.QRCodeInvalid:
                    logger.LogWarning("二维码已失效");
                    break;

                case QQNotifyEventType.GroupMsg:
                    {
                        var msg = (GroupMessage)notifyEvent.Target;
                        logger.LogInformation($"[群消息][{msg.Group.ShowName}]{msg.GetText()}");
                        break;
                    }
                case QQNotifyEventType.ChatMsg:
                    {
                        var msg = (FriendMessage)notifyEvent.Target;
                        logger.LogInformation($"[好友消息][{msg.Friend.ShowName}]{msg.GetText()}");
                        break;
                    }

                default:
                    logger.LogInformation(notifyEvent.Type.GetFullDescription());
                    break;
            }

            return Task.CompletedTask;
        };

        private static readonly WeChatNotifyEventListener _weChatListener = async (client, notifyEvent) =>
        {
            var logger = client.GetSerivce<ILogger>();
            switch (notifyEvent.Type)
            {
                case WeChatNotifyEventType.LoginSuccess:
                    logger.LogInformation("登录成功");
                    break;

                case WeChatNotifyEventType.QRCodeReady:
                    {
                        var verify = notifyEvent.Target.CastTo<Image<Rgba32>>();
                        // verify.ConsoleWrite(); // 显示出来不正常，需要调整
                        const string path = "verify.png";
                        verify.Save(path);
                        logger.LogInformation("请扫描保存在项目根目录的二维码图片");
                        break;
                    }

                case WeChatNotifyEventType.QRCodeSuccess:
                    logger.LogInformation("请在手机上点击确认以登录");
                    break;

                case WeChatNotifyEventType.QRCodeInvalid:
                    logger.LogWarning("二维码已失效");
                    break;

                case WeChatNotifyEventType.Message:
                    {
                        var msg = (Message)notifyEvent.Target;
                        logger.LogInformation($"[{msg.MsgType.GetDescription()} 来自 {msg.FromUser?.ShowName}]: {msg.Content}");
                        var userName = client.GetModule<WebWeChat.Im.Module.Impl.SessionModule>().User.UserName;
                        if (msg.FromUserName == userName && msg.MsgType == MessageType.Text && !msg.Content.IsNullOrEmpty())
                        {
                            var reply = await client.GetRobotReply(WebWeChat.Im.RobotType.Tuling, msg.Content);
                            if (reply.IsOk)
                            {
                                var text = (string)reply.Target;
                                text = $"{text}  --来自机器人回复";
                                await client.SendMsg(MessageSent.CreateTextMsg(text, userName, msg.ToUserName));
                            }
                        }
                        break;
                    }

                case WeChatNotifyEventType.Offline:
                    logger.LogCritical("微信已经掉线");
                    break;

                default:
                    logger.LogInformation(notifyEvent.Type.GetFullDescription());
                    break;

            }
        };

        private static async Task TestQQ()
        {
            var client = new WebQQClient(m => new QQConsoleLogger(m, LogLevel.Debug), _qqListener);
            if ((await client.Login()).IsOk)
            {
                client.BeginPoll();
            }
        }

        private static async Task TestWeChat()
        {
            var client = new WebWeChatClient(m => new WeChatConsoleLogger(m, LogLevel.Debug), _weChatListener);
            if ((await client.Login()).IsOk)
            {
                client.BeginSyncCheck();
            }
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;

            Console.WriteLine("输入1测试qq，输入2测试微信");
            switch (Console.ReadLine())
            {
                case "1":
                    TestQQ().Forget();
                    break;

                case "2":
                    TestWeChat().Forget();
                    break;
            }

            Console.Read();
        }
    }
}