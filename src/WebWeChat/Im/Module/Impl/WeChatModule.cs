﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebWeChat.Im.Core;
using WebWeChat.Im.Module.Interface;

namespace WebWeChat.Im.Module.Impl
{
    public abstract class WeChatModule : IWeChatModule
    {
        protected WeChatModule(IWeChatContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IWeChatContext Context { get; }
        protected ILogger Logger => Context.GetSerivce<ILogger>();
        protected IConfigurationRoot Config => Context.GetSerivce<IConfigurationRoot>();
        protected SessionModule Session => Context.GetModule<SessionModule>();
        protected StoreModule Store => Context.GetModule<StoreModule>();

        public virtual void Dispose()
        {
        }
    }
}
