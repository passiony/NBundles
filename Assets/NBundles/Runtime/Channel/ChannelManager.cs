﻿using System;

namespace GameChannel
{
    public class ChannelManager : Singleton<ChannelManager>
    {
        private BaseChannel channel = null;

        private Action onInitCompleted = null;
        private Action onActionSucceed = null;
        private Action onActionFailed = null;
        private Action<int> onActionProgressValueChange = null;

        public string channelName
        {
            get;
            protected set;
        }
        
        public string appVersion
        {
            get;
            protected set;
        }
        
        public string resVersion
        {
            get;
            protected set;
        }

        public void Init(string channelName,string appV, string resV)
        {
            this.channelName = channelName;
            channel = CreateChannel(channelName);

            appVersion = appV;
            resVersion = resV;
            AndroidSDKListener.Instance.Startup();
        }
        
        public BaseChannel CreateChannel(string channelName)
        {
            ChannelType platName = (ChannelType)Enum.Parse(typeof(ChannelType), channelName);
            switch ((platName))
            {
                case ChannelType.Test:
                    return new TestChannel();
                default:
                    return new TestChannel();
            }
        }

        public bool IsInternalVersion()
        {
            if (channel == null)
            {
                return true;
            }
            return channel.IsInternalChannel();
        }

        public string GetPlatform()
        {
#if UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "iOS";
#elif UNITY_EDITOR_WIN
            return "Win";
#elif UNITY_EDITOR_OSX
            return "OSX";
#endif
        }
        
        public string GetProductName()
        {
            return channel?.GetProductName();
        }

        public bool IsGooglePlay()
        {
            if (channel == null)
            {
                return false;
            }
            return channel.IsGooglePlay();
        }

        #region 初始化SDK
        public void InitSDK(Action callback)
        {
            onInitCompleted = callback;

            channel.Init();
            channel.DataTrackInit();
        }

        public void OnInitSDKCompleted(string msg)
        {
            Logger.Log("OnInitSDKCompleted : " + msg);

            onInitCompleted?.Invoke();
            onInitCompleted = null;
        }
        #endregion

        #region 游戏下载、安装
        public void StartDownloadGame(string url, Action succeed = null, Action fail = null, Action<int> progress = null, string saveName = null)
        {
            onActionSucceed = succeed;
            onActionFailed = fail;
            onActionProgressValueChange = progress;
            channel.DownloadGame(url, saveName);
        }

        public void OnDownloadGameProgressValueChange(int progress)
        {
            OnActionProgressValueChange(progress);
        }

        public void OnDownloadGameFinished(bool succeed)
        {
            OnActionFinshed(succeed);
        }

        public void InstallGame(Action succeed, Action fail)
        {
            onActionSucceed = succeed;
            onActionFailed = fail;
            channel.InstallApk();
        }

        public void OnInstallGameFinished(bool succeed)
        {
            OnActionFinshed(succeed);
        }

        private void OnActionProgressValueChange(int progress)
        {
            if (onActionProgressValueChange != null)
            {
                onActionProgressValueChange.Invoke(progress);
            }
        }

        private void OnActionFinshed(bool succeed)
        {
            if (succeed)
            {
                if (onActionSucceed != null)
                {
                    onActionSucceed.Invoke();
                }
            }
            else
            {
                if (onActionFailed != null)
                {
                    onActionFailed.Invoke();
                }
            }

            onActionSucceed = null;
            onActionFailed = null;
            onActionProgressValueChange = null;
        }
        #endregion

        #region 登陆相关
        public void OnLogin(string msg)
        {
            // TODO：
        }

        public void OnLoginOut(string msg)
        {
            // TODO：
        }
        #endregion

        #region 支付相关
        public void OnSDKPay(string msg)
        {
            // TODO：
        }
        #endregion

        public override void Dispose()
        {
        }
    }
}
