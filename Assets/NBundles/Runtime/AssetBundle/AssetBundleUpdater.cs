using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using System;

/// <summary>
/// added by wsh @ 2017.12.29
/// 功能：Assetbundle更新器
/// </summary>

public enum EForceType
{
    Default,        //0：默认更新通知双按钮
    OnlyUpdate,     //1：强制更新通知单按钮
    AutoUpdate,     //2：是否强制后台自动更新
    DontUpdate      //3：不更新
}

public class AssetBundleUpdater
{
    static int MAX_DOWNLOAD_NUM = 5;
    static int UPDATE_SIZE_LIMIT = 5 * 1024 * 1024;
    static string APK_FILE_PATH = "/boom_{0}_{1}.apk";

    private AssetBundleVersion abVersion;
    private VersionList versionList;
    
    bool needDownloadGame = false;
    bool needUpdateGame = false;
    public bool HasUpdate => needUpdateGame || needDownloadGame;

    bool isDownloading = false;
    bool hasError = false;
    int retryCount = 0;

    EForceType resForceType = EForceType.Default;
    EForceType appForceType = EForceType.Default;
    
    List<string> needDownloadList = new List<string>();
    List<UnityWebAssetRequester> downloadingRequest = new List<UnityWebAssetRequester>();

    int totalDownloadSize = 0;
    int finishedDownloadSize = 0;
    int totalDownloadCount = 0;
    int finishedDownloadCount = 0;
    
    #region 主流程
    public IEnumerator RequestRemoteUrl()
    {
        // 初始化本地版本信息
        abVersion = AssetBundleManager.Instance.abVersion;
        
        yield return abVersion.InitAppVersion();
        yield return abVersion.GetUrlListAndCheckUpdate();

        needDownloadGame = abVersion.needDownloadGame;
        needUpdateGame = abVersion.needUpdateGame;
        resForceType = abVersion.resForceType;
        appForceType = abVersion.appForceType;
    }
    
    public IEnumerator StartCheckUpdate()
    {
        UILauncher.Instance.SetSatus("正在检测资源更新...");
#if UNITY_EDITOR
        if (AssetBundleConfig.IsEditorMode)
        {
            yield break;
        }
#endif
        
//        // 获取服务器地址，并检测大版本更新、资源更新
//        yield return GetUrlListAndCheckUpdate();

        // 执行大版本更新
        if (needDownloadGame)
        {
            //UINoticeTip.Instance.ShowOneButtonTip("游戏下载", "需要下载新的游戏版本！", "确定", null);
            bool doDownload = true;
            while (doDownload)
            {
                if(appForceType == EForceType.Default)
                {
                    UINoticeTip.Instance.ShowTwoButtonTip("游戏下载", "需要下载新的游戏版本！", "取消", "确定",
                        () => { doDownload = false; },
                        () => { doDownload = true; });
                    yield return UINoticeTip.Instance.WaitForResponse();
                }
                else if(appForceType == EForceType.OnlyUpdate)
                {
                    UINoticeTip.Instance.ShowOneButtonTip("游戏下载", "需要下载新的游戏版本！", "确定", null);
                    yield return UINoticeTip.Instance.WaitForResponse();
                }
                else if (appForceType == EForceType.AutoUpdate)
                {
                    doDownload = true;
                }
                else if (appForceType == EForceType.DontUpdate)
                {
                    doDownload = false;
                }
                
                if (doDownload)
                {
                    yield return DownloadGame();
                }
            }
        }
        
        //资源更新
        if (needUpdateGame)
        {
            yield return CheckGameUpdate();
        }
        else
        {
            yield return UpdateFinish(false);
        }
    }

    #endregion

    #region 游戏下载
    IEnumerator DownloadGame()
    {
        Application.OpenURL(URLSetting.APP_DOWNLOAD_URL);
        
//#if UNITY_ANDROID
//        if (Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork)
//        {
//            UINoticeTip.Instance.ShowOneButtonTip("游戏下载", "当前为非Wifi网络环境，下载需要消耗手机流量，继续下载？", "确定", null);
//            yield return UINoticeTip.Instance.WaitForResponse();
//        }
//        DownloadGameForAndroid();
//#elif UNITY_IPHONE
//        ChannelManager.Instance.StartDownloadGame(URLSetting.APP_DOWNLOAD_URL);
//#endif
        yield break;
    }

#if UNITY_ANDROID
    void DownloadGameForAndroid()
    {
//        UILauncher.Instance.SetSatus("正在下载游戏...");
        UILauncher.Instance.SetSatus("Download app...");
        UILauncher.Instance.SetProgressText("");
//        slider.normalizedValue = 0;
//        slider.gameObject.SetActive(true);
//        statusText.text = "正在下载游戏...";

        Application.OpenURL(URLSetting.APP_DOWNLOAD_URL);
    }

    void DownloadGameSuccess()
    {
        UINoticeTip.Instance.ShowOneButtonTip("下载完毕", "游戏下载完毕，确认安装？", "安装", () =>
        {
            
        });
    }

    void DownloadGameFail()
    {
        UINoticeTip.Instance.ShowOneButtonTip("下载失败", "游戏下载失败！", "重试", () =>
        {
            DownloadGameForAndroid();
        });
    }
#endif

    private bool ShowUpdatePrompt(int downloadSize)
    {
        if (UPDATE_SIZE_LIMIT <= 0 && Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            // wifi不提示更新了
            return false;
        }

        if (downloadSize < UPDATE_SIZE_LIMIT)
        {
            return false;
        }

        return true;
    }
    #endregion

    #region 资源更新
    IEnumerator CheckGameUpdate()
    {
        // 检测资源更新
        Logger.Log("Resource download url : " + URLSetting.RES_DOWNLOAD_URL);
        var start = DateTime.Now;
        yield return CheckIfNeededUpdate();
        Logger.Log(string.Format("CheckIfNeededUpdate use {0}ms", (DateTime.Now - start).Milliseconds));
        
        if (needDownloadList.Count <= 0)
        {
            Logger.Log("No resources to update...");
            yield return UpdateFinish(false);
            yield break;
        }
        
        Logger.Log("GetDownloadAssetBundlesSize : {0}", KBSizeToString(totalDownloadSize));
        bool doUpdate = true;
        //
        if(resForceType == EForceType.Default || ShowUpdatePrompt(totalDownloadSize))
        {
            UINoticeTip.Instance.ShowTwoButtonTip("Update", string.Format("New update is available. Download size: {0}", KBSizeToString(totalDownloadSize)), "Cancel", "Download", 
                () => { doUpdate = false; }, 
                () => { doUpdate = true; });
            yield return UINoticeTip.Instance.WaitForResponse();
        }
        else if(resForceType == EForceType.OnlyUpdate || ShowUpdatePrompt(totalDownloadSize))
        {
            UINoticeTip.Instance.ShowOneButtonTip("Update", string.Format("New update is available. Download size: {0}", KBSizeToString(totalDownloadSize)),"Download", 
                () => { doUpdate = true; });
            yield return UINoticeTip.Instance.WaitForResponse();
        }
        else if (resForceType == EForceType.AutoUpdate)
        {
            doUpdate = true;
        }
        else if (resForceType == EForceType.DontUpdate)
        {
            doUpdate = false;
        }
                        
        if (doUpdate)
        {
            UILauncher.Instance.SetSatus("Wiping perfume bottle");
            UILauncher.Instance.SetValue(0);
            UILauncher.Instance.SetProgressText("");
            
            totalDownloadCount = needDownloadList.Count;
            finishedDownloadCount = 0;
            Logger.Log(string.Format("{0} resources to update...", totalDownloadCount));

            start = DateTime.Now;
            retryCount = 0;
            yield return StartUpdate();
            Logger.Log(string.Format("Update use {0}ms", (DateTime.Now - start).Milliseconds));
            UILauncher.Instance.SetValue(1);
            
            start = DateTime.Now;
            yield return UpdateFinish(true);
            Logger.Log(string.Format("UpdateFinish use {0}ms", (DateTime.Now - start).Milliseconds));
        }
    }

    IEnumerator CheckIfNeededUpdate()
    {
        yield return versionList.LoadLocalVersionList();
        
        yield return versionList.LoadServerVersionList();
        
        totalDownloadSize = versionList.CompareVersionList(ref needDownloadList);
    }
    
    IEnumerator StartUpdate()
    {
        downloadingRequest.Clear();
        isDownloading = true;
        hasError = false;
        yield return new WaitUntil(()=>
        {
            return isDownloading == false;
        });
        if (needDownloadList.Count > 0)
        {
            retryCount++;
            if (retryCount < 3)
            {
                yield return StartUpdate();
            }
            else
            {
                bool retry = false;
                retryCount = 0;
                UINoticeTip.Instance.ShowOneButtonTip("网络错误", "游戏更新失败，请确认网络已经连接！", "重试", ()=> { retry = true; });
                yield return UINoticeTip.Instance.WaitForResponse();
                if (retry)
                {
                    yield return StartUpdate();
                }
            }
        }
        yield break;
    }

    IEnumerator UpdateFinish(bool hasUpdate = true)
    {
//        statusText.text = "正在准备资源...";
        UILauncher.Instance.SetSatus("正在准备资源...");

        if (hasUpdate && !hasError)
        {
            // 存储版本号
            abVersion.SaveVersion();
            
            // 拷贝临时文件
            versionList.SaveAllUpdate();
                        
            // 重启资源管理器
            yield return AssetBundleManager.Instance.Cleanup();
            yield return AssetBundleManager.Instance.Initialize();
        }
    }

    
	void Update () {
        if (!isDownloading)
        {
            return;
        }

        for (int i = downloadingRequest.Count - 1; i >= 0; i--)
        {
            var request = downloadingRequest[i];
            if (request.isDone)
            {
                if (!string.IsNullOrEmpty(request.error))
                {
                    Logger.LogError("Error when downloading file : " + request.assetbundleName + "\n from url : " + request.url + "\n err : " + request.error);
                    hasError = true;
                    needDownloadList.Add(request.assetbundleName);
                }
                else
                {
                    // TODO：是否需要显示下载流量进度？
                    Logger.Log($"Finish downloading file :{request.assetbundleName} \n{KBSizeToString(request.bytes.Length)}.bytes \nfrom url : {request.url}");
                    finishedDownloadCount++;
                    
                    //缓存到temp临时文件夹，防止下载中断，弄脏沙盒数据
                    var filePath = AssetBundleUtility.GetPersistentTempPath(request.assetbundleName);
                    FileUtility.SafeWriteAllBytes(filePath, request.bytes);
                    versionList.SaveToTempForder(request.assetbundleName);
                }
                request.Dispose();
                downloadingRequest.RemoveAt(i);
            }
        }

        if (!hasError)
        {
            while (downloadingRequest.Count < MAX_DOWNLOAD_NUM && needDownloadList.Count > 0)
            {
                var fileName = needDownloadList[needDownloadList.Count - 1];
                needDownloadList.RemoveAt(needDownloadList.Count - 1);
                var request = AssetBundleManager.Instance.DownloadAssetBundleAsync(fileName);
                downloadingRequest.Add(request);
            }
        }

        if (downloadingRequest.Count == 0)
        {
            isDownloading = false;
        }
        float progressSlice = 1.0f / totalDownloadCount;
        float progressValue = finishedDownloadCount * progressSlice;
        for (int i = 0; i < downloadingRequest.Count; i++)
        {
            progressValue += (progressSlice * downloadingRequest[i].progress);
        }
        finishedDownloadSize = (int) (totalDownloadSize * progressValue);
        
        UILauncher.Instance.SetValue(progressValue);
        UILauncher.Instance.SetProgressText(KBSizeToString(finishedDownloadSize)+"/"+KBSizeToString(totalDownloadSize));
//        slider.normalizedValue = progressValue;
    }

    private string KBSizeToString(int kbSize)
    {
        string sizeStr = string.Empty;
        if (kbSize >= 1024)
        {
            sizeStr = (kbSize / 1024.0f).ToString("0.0") + " MB";
        }
        else
        {
            sizeStr = kbSize + " KB";
        }

        return sizeStr;
    }
    #endregion
}
