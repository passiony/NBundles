using UnityEditor;
using System.IO;
using GameChannel;
using System;
using AssetBundles;
using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// added by wsh @ 2018.01.03
/// 功能： 打包相关配置和通用函数
/// </summary>

public enum ServerType
{
    NONE = 0,
    CN_IP = 1,
    USA_IP = 2,
}

public class PackageUtils
{
    public const string LocalServerPrefsKey = "AssetBundlesLocalServerType";
    public const string ChannelNamePrefsKey = "ChannelNamePrefsKey";
    public const string AndroidBuildABForPerChannelPrefsKey = "AndroidBuildABForPerChannelPrefsKey";
    public const string IOSBuildABForPerChannelPrefsKey = "IOSBuildABForPerChannelPrefsKey";
    public const string BuildABAutoIncreaseAppVersionPrefsKey = "BuildABAutoIncreaseAppVersionPrefsKey";
    public const string BuildABAutoIncreaseResVersionPrefsKey = "BuildABAutoIncreaseResVersionPrefsKey";
    public const string BuildAutoNoticeWechatPrefsKey = "BuildAutoNoticeWechatPrefsKey";
    public const string BuildAutoUploadAppPrefsKey = "BuildAutoUploadAppPrefsKey";
    public const string BuildCodePrefsKey = "BuildAutoUploadAppPrefsKey";
    
    public static bool GetAndroidBuildABForPerChannelSetting()
    {
        if (!EditorPrefs.HasKey(AndroidBuildABForPerChannelPrefsKey))
        {
            SaveAndroidBuildABForPerChannelSetting(false);
            return false;
        }

        bool enable = EditorPrefs.GetBool(AndroidBuildABForPerChannelPrefsKey, false);
        return enable;
    }
    public static void SaveAndroidBuildABForPerChannelSetting(bool enable)
    {
        EditorPrefs.SetBool(AndroidBuildABForPerChannelPrefsKey, enable);
    }

    
    public static bool GetIOSBuildABForPerChannelSetting()
    {
        if (!EditorPrefs.HasKey(IOSBuildABForPerChannelPrefsKey))
        {
            SaveIOSBuildABForPerChannelSetting(false);
            return false;
        }

        bool enable = EditorPrefs.GetBool(IOSBuildABForPerChannelPrefsKey, false);
        return enable;
    }
    public static void SaveIOSBuildABForPerChannelSetting(bool enable)
    {
        EditorPrefs.SetBool(IOSBuildABForPerChannelPrefsKey, enable);
    }

    
    
    public static bool GetAutoIncreaseAppVersionSetting()
    {
        if (!EditorPrefs.HasKey(BuildABAutoIncreaseAppVersionPrefsKey))
        {
            SaveAutoIncreaseAppVersionSetting(false);
            return false;
        }

        bool enable = EditorPrefs.GetBool(BuildABAutoIncreaseAppVersionPrefsKey, false);
        return enable;
    }
    public static void SaveAutoIncreaseAppVersionSetting(bool enable)
    {
        EditorPrefs.SetBool(BuildABAutoIncreaseAppVersionPrefsKey, enable);
    }
    
    public static bool GetAutoIncreaseResVersionSetting()
    {
        if (!EditorPrefs.HasKey(BuildABAutoIncreaseResVersionPrefsKey))
        {
            SaveAutoIncreaseResVersionSetting(false);
            return false;
        }

        bool enable = EditorPrefs.GetBool(BuildABAutoIncreaseResVersionPrefsKey, false);
        return enable;
    }
    
    public static void SaveAutoIncreaseResVersionSetting(bool enable)
    {
        EditorPrefs.SetBool(BuildABAutoIncreaseResVersionPrefsKey, enable);
    }

    public static void SaveBuildCode(string code)
    {
        EditorPrefs.SetString(BuildCodePrefsKey, code);
    }
    
    public static string GetBuildCode()
    {
        const string code = "0";
        if (!EditorPrefs.HasKey(BuildCodePrefsKey))
        {
            SaveBuildCode(code);
            return code;
        }
        
        return  EditorPrefs.GetString(BuildCodePrefsKey, code);
    }

    public static ServerType GetLocalServerType()
    {
#if CN_IP
        return ServerType.CN_IP;
#elif USA_IP
        return ServerType.USA_IP;
#elif DEBUG_IP1
        return ServerType.DEBUG_IP_ZKK;
#elif DEBUG_IP2
        return ServerType.DEBUG_IP_WPF;
#endif
        return ServerType.CN_IP;
    }

    public static void SaveLocalServerType(ServerType type)
    {
        SymbolsSetting.SetServerType(type);
    }

    public static string GetLocalServerIP()
    {
        return URLSetting.BASE_URL;
    }
    
    public static string GetCurrentMachineLocalIP()
    {
        try
        {
            // 注意：这里获取所有内网地址后选择一个最小的，因为可能存在虚拟机网卡
            var ips = new List<string>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip.ToString());
                }
            }
            ips.Sort();
            if (ips.Count <= 0)
            {
                Logger.LogError("Get inter network ip failed!");
            }
            else
            {
                return ips[0];
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError("Get inter network ip failed with err : " + ex.Message);
            Logger.LogError("Go Tools/Package to specify any machine as local server!!!");
        }
        return string.Empty;
    }

    public static bool BuildAssetBundlesForPerChannel(BuildTarget buildTarget)
    {
        return true;
//        if (buildTarget == BuildTarget.Android && GetAndroidBuildABForPerChannelSetting() ||
//            buildTarget == BuildTarget.iOS && GetIOSBuildABForPerChannelSetting())
//        {
//            return true;
//        }
//        return false;
    }

    public static string GetCurPlatformName()
    {
        return GetPlatformName(EditorUserBuildSettings.activeBuildTarget);
    }
    
    public static string GetPlatformName(BuildTarget buildTarget)
    {
        switch (buildTarget)
        {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            default:
                Logger.LogError("Error buildTarget!!!");
                return null;
        }
    }

    public static ChannelType GetCurSelectedChannel()
    {
        ChannelType channelType = ChannelType.Test;
        string channelName = EditorPrefs.GetString(ChannelNamePrefsKey);
        if (Enum.IsDefined(typeof(ChannelType), channelName))
        {
            channelType = (ChannelType)Enum.Parse(typeof(ChannelType), channelName);
        }
        else
        {
            EditorPrefs.SetString(ChannelNamePrefsKey, ChannelType.Test.ToString());
        }
        return channelType;
    }

    public static void SaveCurSelectedChannel(ChannelType channelType)
    {
        EditorPrefs.SetString(ChannelNamePrefsKey, channelType.ToString());
    }

    public static string GetPlatformChannelFolderName(BuildTarget target, string channelName)
    {
        if (BuildAssetBundlesForPerChannel(target))
        {
            // 不同渠道的AB输出到不同的文件夹
            return channelName;
        }
        else
        {
            // 否则写入通用的平台文件夹
            return GetPlatformName(target);
        }
    }

    public static string GetChannelRelativePath(BuildTarget target, string channelName)
    {
        string outputPath = Path.Combine(GetPlatformName(target), GetPlatformChannelFolderName(target, channelName));
        return outputPath;
    }

    public static string GetAssetBundleRelativePath(BuildTarget target, string channelName)
    {
        string outputPath = GetChannelRelativePath(target, channelName);
        outputPath = Path.Combine(outputPath, BuildUtility.ManifestBundleName);
        return outputPath;
    }

    public static string GetChannelOutputPath(BuildTarget target, string channelName)
    {
        string outputPath = Path.Combine(AssetBundleConfig.AssetBundlesBuildOutputPath, GetChannelRelativePath(target, channelName));
        FileUtility.CheckDirAndCreateWhenNeeded(outputPath);
        return outputPath;
    }

    public static string GetAssetBundleOutputPath(BuildTarget target, string channelName)
    {
        string outputPath = Path.Combine(AssetBundleConfig.AssetBundlesBuildOutputPath, GetAssetBundleRelativePath(target, channelName));
        FileUtility.CheckDirAndCreateWhenNeeded(outputPath);
        return outputPath;
    }
    
    public static string GetAssetBundleFilePath(BuildTarget target, string channelName, string fileName)
    {
        string outputPath = GetAssetBundleOutputPath(target, channelName);
        return Path.Combine(outputPath, fileName);
    }

    public static string GetAssetbundleManifestPath(BuildTarget target, string channelName)
    {
        string outputPath = GetAssetBundleOutputPath(target, channelName);
        return Path.Combine(outputPath, BuildUtility.ManifestBundleName);
    }

    public static string GetCurPlatformChannelRelativePath()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var channelName = GetCurSelectedChannel().ToString();
        return GetChannelRelativePath(buildTarget, channelName);
    }

    public static string GetCurBuildSettingAssetBundleOutputPath()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var channelType = GetCurSelectedChannel();

        return GetAssetBundleOutputPath(buildTarget, channelType.ToString());
    }

    public static string GetCurBuildSettingAssetBundleManifestPath()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var channelType = GetCurSelectedChannel();
        return GetAssetbundleManifestPath(buildTarget, channelType.ToString());
    }

    public static string GetCurBuildSettingStreamingManifestPath()
    {
        string path = AssetBundleUtility.GetStreamingAssetsDataPath();
        path = Path.Combine(path, BuildUtility.ManifestBundleName);
        return path;
    }

    public static AssetBundleManifest GetManifestFormLocal(string manifestPath)
    {
        FileInfo fileInfo = new FileInfo(manifestPath);
        if (!fileInfo.Exists)
        {
            Debug.LogError("You need to build assetbundles first to get assetbundle dependencis info!");
            return null;
        }
        byte[] bytes = FileUtility.SafeReadAllBytes(fileInfo.FullName);
        if (bytes == null)
        {
            return null;
        }
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(bytes);
        AssetBundleManifest manifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        assetBundle.Unload(false);
        return manifest;
    }

    public static void CopyAssetBundlesToStreamingAssets(BuildTarget buildTarget, string channelName)
    {
        string source = GetAssetBundleOutputPath(buildTarget, channelName);
        string destination = AssetBundleUtility.GetStreamingAssetsDataPath();
        // 有毒，竟然在有的windows系统这个函数删除不了目录，不知道是不是Unity的Bug
        // GameUtility.SafeDeleteDir(destination);
        AssetDatabase.DeleteAsset(FileUtility.FullPathToAssetPath(destination));
        AssetDatabase.Refresh();

        try
        {
            FileUtil.CopyFileOrDirectoryFollowSymlinks(source, destination);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Something wrong, you need manual delete AssetBundles folder in StreamingAssets, err : " + ex);
            return;
        }

        var allManifest = FileUtility.GetSpecifyFilesInFolder(destination, new string[] { ".manifest" });
        if (allManifest != null && allManifest.Length > 0)
        {
            for (int i = 0; i < allManifest.Length; i++)
            {
                FileUtility.SafeDeleteFile(allManifest[i]);
            }
        }

        AssetDatabase.Refresh();
    }

    public static void ClearPersistentAssets()
    {
        string outputPath = Path.Combine(Application.persistentDataPath, AssetBundleConfig.AssetBundlesFolderName);
        FileUtility.SafeDeleteDir(outputPath);
        Debug.Log("clear persistent assets done!");
    }
    public static void CopyCurSettingAssetBundlesToStreamingAssets()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var channelName = GetCurSelectedChannel().ToString();
        CopyAssetBundlesToStreamingAssets(buildTarget, channelName);
        Debug.Log("Copy channel assetbundles to streaming assets done!");
    }

    public static void CheckAndAddSymbolIfNeeded(BuildTarget buildTarget, string targetSymbol)
    {
        if (buildTarget != BuildTarget.Android && buildTarget != BuildTarget.iOS)
        {
            Debug.LogError("Only support Android and IOS !");
            return;
        }

        var buildTargetGroup = buildTarget == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS;
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        if (!symbols.Contains("HOTFIX_ENABLE"))
        {
            symbols = string.Format("{0};{1};", symbols, "HOTFIX_ENABLE");
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, symbols);
    }

    public static void CheckAndRunAllCheckers(bool buildForPerChannel, bool forceRun)
    {
        // 这东西有点浪费时间，没必要的时候不跑它
        if (AssetBundleDispatcherInspector.hasAnythingModified || forceRun)
        {
            AssetBundleDispatcherInspector.hasAnythingModified = false;
            var start = DateTime.Now;
            CheckAssetBundles.Run(buildForPerChannel);
            Debug.Log("Finished CheckAssetBundles.Run! use " + (DateTime.Now - start).TotalSeconds + "s");
        }
    }
    
    public static void CopyAndroidSDKResources(string channelName)
    {
        string targetPath = Path.Combine(Application.dataPath, "Plugins");
        targetPath = Path.Combine(targetPath, "Android");
        FileUtility.SafeClearDir(targetPath);
        
        string channelPath = Path.Combine(Environment.CurrentDirectory, "Channel");
        string resPath = Path.Combine(channelPath, "UnityCallAndroid_" + channelName);
        if (!Directory.Exists(resPath))
        {
            resPath = Path.Combine(channelPath, "UnityCallAndroid");
        }

        EditorUtility.DisplayProgressBar("提示", "正在拷贝SDK资源，请稍等", 0f);
        PackageUtils.CopyJavaFolder(resPath + "/assets", targetPath + "/assets");
        EditorUtility.DisplayProgressBar("提示", "正在拷贝SDK资源，请稍等", 0.3f);
        PackageUtils.CopyJavaFolder(resPath + "/libs", targetPath + "/libs");
        EditorUtility.DisplayProgressBar("提示", "正在拷贝SDK资源，请稍等", 0.6f);
        PackageUtils.CopyJavaFolder(resPath + "/res", targetPath + "/res");
        if (File.Exists(resPath + "/bin/UnityCallAndroid.jar"))
        {
            File.Copy(resPath + "/bin/UnityCallAndroid.jar", targetPath + "/libs/UnityCallAndroid.jar", true);
        }
        if (File.Exists(resPath + "/AndroidManifest.xml"))
        {
            File.Copy(resPath + "/AndroidManifest.xml", targetPath + "/AndroidManifest.xml", true);
        }

        EditorUtility.DisplayProgressBar("提示", "正在拷贝SDK资源，请稍等", 1f);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    public static void CopyJavaFolder(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            return;
        }
        if (!Directory.Exists(destination))
        {
            Directory.CreateDirectory(destination);
            AssetDatabase.Refresh();
        }

        string[] sourceDirs = Directory.GetDirectories(source);
        for (int i = 0; i < sourceDirs.Length; i++)
        {
            CopyJavaFolder(sourceDirs[i] + "/", destination + "/" + Path.GetFileName(sourceDirs[i]));
        }

        string[] sourceFiles = Directory.GetFiles(source);
        for (int j = 0; j < sourceFiles.Length; j++)
        {
            if (sourceFiles[j].Contains("classes.jar"))
            {
                continue;
            }
            File.Copy(sourceFiles[j], destination + "/" + Path.GetFileName(sourceFiles[j]), true);
        }
    }
    
//    public static string[] ReadCurAppAndResVersionFile()
//    {
//        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
//        var channelType = GetCurSelectedChannel();
//        
//        return ReadAppAndResVersionFile(buildTarget,channelType);
//    }
    
    public static string IncreaseResSubVersion(string versionstr)
    {
        // 每一次构建资源，子版本号自增，注意：前两个字段这里不做托管，自行编辑设置
        string[] vers = versionstr.Split('.');
        if (vers.Length > 0)
        {
            int subVer = 0;
            int.TryParse(vers[vers.Length - 1], out subVer);
//            vers[vers.Length - 1] = string.Format("{0:D3}", subVer + 1);
            vers[vers.Length - 1] = (subVer + 1).ToString();
        }
        versionstr = string.Join(".", vers);
        return versionstr;
    }
    
    public static string[] ReadAppAndResVersionFile(BuildTarget buildTarget,ChannelType channelType)
    {
        // 从资源版本号文件（当前渠道AB输出目录中）加载资源版本号
        string rootPath = GetAssetBundleOutputPath(buildTarget,channelType.ToString());
        string appPath = rootPath + "/" + BuildUtility.AppVersionFileName;
        appPath = FileUtility.FormatToUnityPath(appPath);
        string content = FileUtility.SafeReadAllText(appPath);
        if (content != null)
        {
            var arr = content.Split('|');
            if (arr.Length >= 2)
            {
                return arr;
            }
        }
        Debug.LogError("找不到 appVersion.bytes 文件");
        return null;
    }
}
