using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameChannel;
using LitJson;
using UnityEngine;

namespace AssetBundles
{
    public class AssetBundleVersion
    {

        public string clientAppVersion { get; private set; }
        public string serverAppVersion { get; private set; }

        public string clientResVersion { get; private set; }
        public string serverResVersion { get; private set; }
        
        public bool needDownloadGame { get; private set; }
        public bool needUpdateGame { get; private set; }
        public EForceType resForceType { get; private set; }
        public EForceType appForceType { get; private set; }
        public bool isNewVersion { get; private set; }

        public const string RESPONSE_PREFS = "Server_Response";


        public IEnumerator InitAppVersion()
        {
            var streamingAppVersion = "0.0.0";
            var streamingResVersion = "0.0.0";
            var streamingChannel = "Test";

#if UNITY_EDITOR
            if (AssetBundleConfig.IsEditorMode)
            {
                clientAppVersion = streamingAppVersion;
                clientResVersion = streamingResVersion;
                ChannelManager.Instance.Init(streamingChannel,clientAppVersion,clientResVersion);
                isNewVersion = false;

                yield break;
            }
#endif
            // init streamingasset.versions
            var appVersionRequest = AssetBundleManager.Instance.RequestAssetFileAsync(BuildUtility.AppVersionFileName);
            yield return appVersionRequest;
            var streamingTxt = appVersionRequest.text;
            appVersionRequest.Dispose();

            if (!string.IsNullOrEmpty(streamingTxt))
            {
                var array = streamingTxt.Split('|');
                streamingAppVersion = array[0];
                streamingResVersion = array[1];
                streamingChannel = array[2];
            }

            //init persistentasset.versions
            var persistentAppVersion = streamingAppVersion;
            var persistentResVersion = streamingResVersion;
            var persistentChannel = streamingChannel;

            var appVersionPath = AssetBundleUtility.GetPersistentDataPath(BuildUtility.AppVersionFileName);
            var persistentTxt = FileUtility.SafeReadAllText(appVersionPath);
            if (string.IsNullOrEmpty(persistentTxt))
            {
                FileUtility.SafeWriteAllText(appVersionPath,
                    streamingAppVersion + "|" + streamingResVersion + "|" + streamingChannel);
            }
            else
            {
                var array = persistentTxt.Split('|');
                persistentAppVersion = array[0];
                persistentResVersion = array[1];
                persistentChannel = array[2];
            }

            //init client.versions
            clientAppVersion = persistentAppVersion;
            clientResVersion = persistentResVersion;
            ChannelManager.Instance.Init(persistentChannel,clientAppVersion,clientResVersion);

            Logger.Log(string.Format("persistentAppVersion = {0}, persistentResVersion = {1}, persistentChannel = {2}",
                persistentAppVersion, persistentResVersion, persistentChannel));
            Logger.Log(string.Format("streamingAppVersion = {0}, streamingAppVersion = {1}, streamingChannel = {2}",
                streamingAppVersion, streamingResVersion, streamingChannel));

            // ?????????????????????
            // ??????persistent???????????????streamingAssets??????app??????????????????????????????????????????????????????????????????
            if (BuildUtility.CheckIsNewVersion(persistentAppVersion, streamingAppVersion))
            {
                Debug.Log("?????????????????????????????????????????????");

                clientAppVersion = streamingAppVersion;
                clientResVersion = streamingResVersion;
                ChannelManager.Instance.Init(streamingChannel,clientAppVersion,clientResVersion);
                isNewVersion = true;

                FileUtility.SafeDeleteDir(AssetBundleUtility.GetPersistentDataPath());
                FileUtility.SafeDeleteDir(AssetBundleUtility.GetPersistentTempPath());
                FileUtility.SafeWriteAllText(appVersionPath,
                    streamingAppVersion + "|" + streamingResVersion + "|" + streamingChannel);

                // ?????????????????????
                yield return AssetBundleManager.Instance.Cleanup();
                yield return AssetBundleManager.Instance.Initialize();
            }
        }

        #region ?????????????????????????????????????????????

        IEnumerator DownloadInternalLocalAppVersion()
        {
            var request = AssetBundleManager.Instance.DownloadAssetFileAsync(BuildUtility.AppVersionFileName);
            yield return request;
            if (request.error != null)
            {
                UINoticeTip.Instance.ShowOneButtonTip("????????????", "???????????????????????????????????????????????????", "??????", null);
                yield return UINoticeTip.Instance.WaitForResponse();
                Logger.LogError("Download :  " + request.assetbundleName + "\n from url : " + request.url + "\n err : " + request.error);
                request.Dispose();

                // ?????????????????????????????????????????????????????????????????????
                yield break;
            }

            var versionTxt = request.text.Trim().Replace("\r", "");
            var array= versionTxt.Split('|');
            serverAppVersion = array[0];
            serverResVersion = array[1];
            request.Dispose();

            yield break;
        }


        IEnumerator InternalGetUrlList()
        {
            // ???????????????????????????
            var localSerUrlRequest = AssetBundleManager.Instance.RequestAssetFileAsync(AssetBundleConfig.AssetBundleServerUrlFileName);
            yield return localSerUrlRequest;
#if UNITY_ANDROID
            // TODO???GooglePlay?????????????????????
#elif UNITY_IPHONE
        // TODO???ios?????????????????????
#endif
            URLSetting.RES_DOWNLOAD_URL = localSerUrlRequest.text + BuildUtility.ManifestBundleName + "/";
            localSerUrlRequest.Dispose();

            // ????????????????????????????????????
            yield return DownloadInternalLocalAppVersion();
        }

        public IEnumerator GetUrlListAndCheckUpdate()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                yield break;
            }

            // ?????????????????????
            yield return OutnetGetUrlList();
            //???????????????
            needDownloadGame = BuildUtility.CheckIsNewVersion(clientAppVersion, serverAppVersion);
            //?????????????????????
            needUpdateGame = BuildUtility.CheckIsNewVersion(clientResVersion, serverResVersion);

#if LOGGER_ON
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendFormat("BASE_URL = {0}\n", URLSetting.BASE_URL);
        sb.AppendFormat("APP_DOWNLOAD_URL = {0}\n", URLSetting.APP_DOWNLOAD_URL);
        sb.AppendFormat("RES_DOWNLOAD_URL = {0}\n", URLSetting.RES_DOWNLOAD_URL);
        sb.AppendFormat("App Version = {0}\n", serverAppVersion);
        sb.AppendFormat("Res Version = {0}\n", serverResVersion);
        sb.AppendFormat("NeedDownloadGame = {0}\n", needDownloadGame);
        sb.AppendFormat("NeedUpdateGame = {0}\n", needUpdateGame);
        sb.AppendFormat("WhiteList = {0}\n", Define.isWhitelist.ToString());
        
        Logger.LogWarning(sb.ToString());
#endif
        }

        IEnumerator OutnetGetUrlList()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("platform", GameUtility.GetPlatform());
            dic.Add("appVersion", clientAppVersion);
            dic.Add("resVersion", clientResVersion);
            dic.Add("device", SystemInfo.deviceModel);

            string args = JsonMapper.ToJson(dic);
            Debug.LogWarning($"[StartUp.Req]: {URLSetting.START_UP_URL}?{args}");

            bool GetUrlListComplete = false;
            string response = null;
            NetworkHttp.Instance.Post(URLSetting.START_UP_URL, null, ByteUtility.StringToBytes(args), (result) =>
            {
                response = result;
                GetUrlListComplete = true;
            }, 2);

            yield return new WaitUntil(() => { return GetUrlListComplete; });

            if (response == null)
            {
                yield break;
            }

#if !LOGGER_ON
            Debug.LogWarning($"[StartUp.Rsp]: {response}");
#endif
            var urlList = JsonMapper.ToObject(response);
            if (urlList == null)
            {
                Logger.LogError("Get url list for args {0} with err : {1}", args, "Deserialize url list null!");
                yield break;
            }

            if (!urlList.ContainsKey("code") || Convert.ToInt32(urlList["code"].ToString()) != 0 ||
                !urlList.ContainsKey("data"))
            {
                Logger.LogError("Get url list for args {0} with err : {1}", args, urlList["msg"]);
                yield break;
            }

            ParseServerResult(urlList["data"]);
        }

        /// <summary>
        /// ??????ServerResult
        /// </summary>
        void ParseServerResult(JsonData jsonData)
        {
            //???????????????????????????????????????????????????
            string json = jsonData.ToJson();
            PlayerPrefs.SetString(RESPONSE_PREFS, json);

            //??????
            resForceType = (EForceType) jsonData.TryGetInt("resForceType");
            appForceType = (EForceType) jsonData.TryGetInt("appForceType");
            serverAppVersion = jsonData.TryGetString("appVersion");
            serverResVersion = jsonData.TryGetString("resVersion");
            URLSetting.APP_DOWNLOAD_URL = jsonData.TryGetString("appUrl");
            URLSetting.RES_DOWNLOAD_URL = jsonData.TryGetString("resUrl");
        }


        public void SaveVersion()
        {
            clientResVersion = serverResVersion;
            
            var appVersionPath = AssetBundleUtility.GetPersistentDataPath(BuildUtility.AppVersionFileName);
            FileUtility.SafeWriteAllText(appVersionPath, clientAppVersion + "|" + clientResVersion + "|" + ChannelManager.Instance.channelName);
        }
    
        #endregion
        
       
    }
}