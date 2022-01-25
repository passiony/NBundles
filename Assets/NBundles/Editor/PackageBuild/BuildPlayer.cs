using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using AssetBundles;
using System;
using System.Linq;
using System.Text;
using UnityEditor.Build.Reporting;
using System.Diagnostics;
using GameChannel;
using LitJson;
using Debug = UnityEngine.Debug;

/// <summary>
/// added by wsh @ 2018.01.03
/// 功能： 打包相关配置和通用函数
/// 
/// 注意：
/// 1）如果为每个渠道分别打AB包，则将渠道名打入各个AB包
/// ---为了解决iOS各个渠道包的提审问题
/// </summary>

public class BuildPlayer : Editor
{
    public const string XCodeOutputPath = "vXCode";

//    public static void WriteChannelNameFile(BuildTarget buildTarget, string channelName)
//    {
//        var outputPath = PackageUtils.GetAssetBundleOutputPath(buildTarget, channelName);
//        GameUtility.SafeWriteAllText(Path.Combine(outputPath, BuildUtils.ChannelNameFileName), channelName);
//    }

    public static void WriteAssetBundleSize(AssetBundleManifest manifest)
    {
        var outputPath = PackageUtils.GetCurBuildSettingAssetBundleOutputPath();
        var allAssetbundles = manifest.GetAllAssetBundles();
        
        StringBuilder sb = new StringBuilder();
        if (allAssetbundles != null && allAssetbundles.Length > 0)
        {
            foreach (var assetbundle in allAssetbundles)
            {
                FileInfo fileInfo = new FileInfo(Path.Combine(outputPath, assetbundle));

                Hash128 hash = manifest.GetAssetBundleHash(assetbundle);
                int size = (int)(fileInfo.Length / 1024) + 1;
                sb.AppendFormat("{0}|{1}|{2}\n", FileUtility.FormatToUnityPath(assetbundle), hash, size);
            }
        }
        string content = sb.ToString().Trim();
        FileUtility.SafeWriteAllText(Path.Combine(outputPath, BuildUtility.VersionsFileName), content);
    }
    
    private static void InnerBuildAssetBundles(BuildTarget buildTarget, string channelName,string resVersion, bool writeConfig)
    {
        BuildAssetBundleOptions buildOption = BuildAssetBundleOptions.IgnoreTypeTreeChanges | BuildAssetBundleOptions.DeterministicAssetBundle;
        string outputPath = PackageUtils.GetAssetBundleOutputPath(buildTarget, channelName);
        var old_manifest = GetCurrentManifest();
        
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, buildOption, buildTarget);
        if (manifest != null && writeConfig)
        {
            AssetsPathMappingEditor.BuildPathMapping(manifest);
            VariantMappingEditor.BuildVariantMapping(manifest);
            manifest = BuildPipeline.BuildAssetBundles(outputPath, buildOption, buildTarget);
        }

        WriteAssetBundleSize(manifest);
        ClearUnuseFiles(outputPath, manifest);
        AssetDatabase.Refresh();
    }

    private static void ClearUnuseFiles(string outputPath, AssetBundleManifest manifest)
    {
        var items = manifest.GetAllAssetBundles();
        var buildVersions = items.ToDictionary(item => item, item => manifest.GetAssetBundleHash(item).ToString());
        
        //clear no use files
        string[] ignoredFiles = { "AssetBundles","AssetBundleServerUrl", "versions.bytes", "assetsmap_bytes", "app_version.bytes" };
        var files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
        var deletes = (from t in files
            let file = t.Replace('\\', '/').Replace(outputPath.Replace('\\', '/') + '/', "")
            where !file.EndsWith(".manifest", StringComparison.Ordinal) && !Array.Exists(ignoredFiles, s => s.Equals(file))
            where !buildVersions.ContainsKey(file)
            select t).ToList();

        foreach (string delete in deletes)
        {
            if (!File.Exists(delete))
            {
                continue;
            }

            File.Delete(delete);
            File.Delete(delete + ".manifest");
        }
        deletes.Clear();
    }

    public static Manifest GetCurrentManifest()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        string channelName = PackageUtils.GetCurSelectedChannel().ToString();
        
        string path = PackageUtils.GetAssetbundleManifestPath(buildTarget, channelName);
        Manifest manifest=new Manifest();

        if (File.Exists(path))
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
            manifest.LoadFromAssetbundle(assetBundle);
            assetBundle.Unload(false);
        }
              
        return manifest;
    }


    public static void BuildAssetBundles(BuildTarget buildTarget, ChannelType channelType,string resVersion)
    {
        string channelName = channelType.ToString();
        bool buildForPerChannel = PackageUtils.BuildAssetBundlesForPerChannel(buildTarget);

        bool forceCheck = false;
        PackageUtils.CheckAndRunAllCheckers(buildForPerChannel, forceCheck);
            
        DateTime start = DateTime.Now;
        if (buildForPerChannel)
        {
            start = DateTime.Now;
            CheckAssetBundles.SwitchChannel(channelName);
            Debug.Log("Finished CheckAssetBundles.SwitchChannel! use " + (DateTime.Now - start).TotalSeconds + "s");
        }

        start = DateTime.Now;
        InnerBuildAssetBundles(buildTarget, channelName, resVersion, true);
        Debug.Log("Finished InnerBuildAssetBundles! use " + (DateTime.Now - start).TotalSeconds + "s");

        var targetName = PackageUtils.GetPlatformName(buildTarget);
        Debug.Log(string.Format("Build assetbundles for platform : {0} and channel : {1} done!", targetName, channelName));
    }

    public static void BuildAssetBundlesForAllChannels(BuildTarget buildTarget)
    {
        bool buildForPerChannel = PackageUtils.BuildAssetBundlesForPerChannel(buildTarget);
        var targetName = PackageUtils.GetPlatformName(buildTarget);
        Debug.Assert(buildForPerChannel == true);
        PackageUtils.CheckAndRunAllCheckers(buildForPerChannel, false);

        int index = 0;
        double switchChannel = 0;
        double buildAssetbundles = 0;
        var start = DateTime.Now;
        foreach (var current in (ChannelType[])Enum.GetValues(typeof(ChannelType)))
        {
            var channelName = current.ToString();
            var versions = PackageUtils.ReadAppAndResVersionFile(buildTarget,current);
            
            start = DateTime.Now;
            CheckAssetBundles.SwitchChannel(channelName);
            switchChannel = (DateTime.Now - start).TotalSeconds;

            start = DateTime.Now;
            InnerBuildAssetBundles(buildTarget, channelName, versions[1], index == 0);
            buildAssetbundles = (DateTime.Now - start).TotalSeconds;

            index++;
            Debug.Log(string.Format("{0}.Build assetbundles for platform : {1} and channel : {2} done! use time : switchChannel = {3}s , build assetbundls = {4} s", index, targetName, channelName, switchChannel, buildAssetbundles));
        }

        Debug.Log(string.Format("Build assetbundles for platform : {0} for all {1} channels done!", targetName, index));
    }

    public static void BuildAssetBundlesForCurSetting()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var channelType = PackageUtils.GetCurSelectedChannel();
        var versions = PackageUtils.ReadAppAndResVersionFile(buildTarget,channelType);
        BuildAssetBundles(buildTarget, channelType, versions[1]);
    }

    private static void SetPlayerSetting(BaseChannel channel)
    {
        if (channel != null)
        {
            PlayerSettings.applicationIdentifier = channel.GetBundleID();
            PlayerSettings.productName = channel.GetProductName();
            PlayerSettings.companyName = channel.GetCompanyName();
            PlayerSettings.bundleVersion = PackageTool.appVersion;
            
            if (int.TryParse(PackageTool.buildCode, out int code))
            {
                PlayerSettings.Android.bundleVersionCode = code;
                PlayerSettings.iOS.buildNumber = code.ToString();
            }
            
            PlayerSettings.iOS.appleDeveloperTeamID= "";
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.iOSManualProvisioningProfileID = "";
            PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Development;
        }

        PlayerSettings.keystorePass = "";
        PlayerSettings.keyaliasPass = "";
    }

    public static void BuildAndroid(string channelName, bool isTest)
    {
        BuildTarget buildTarget = BuildTarget.Android;
        PackageUtils.CopyAssetBundlesToStreamingAssets(buildTarget, channelName);
        BaseChannel channel = ChannelManager.Instance.CreateChannel(channelName);
        SetPlayerSetting(channel);

        string savePath = PackageUtils.GetChannelOutputPath(buildTarget, channelName);
        string dateTime = DateTime.Now.ToString("MM.dd-HH.mm");
        string serverType = PackageUtils.GetLocalServerType().ToString();
//        string develop = SymbolsSetting.isDevelop ? "-Develop" : "-Product";
        string appName = string.Format("{0}-v{1}-{2}-{3}-t{4}.apk",channel.GetProductName(), PackageTool.appVersion , channelName, serverType, dateTime).ToLower();
        
        string buildFolder = Path.Combine(savePath, appName);
        var report = BuildPipeline.BuildPlayer(GetBuildScenes(), buildFolder, buildTarget, BuildOptions.None);
        
        string outputPath = Path.Combine(Application.persistentDataPath, AssetBundleConfig.AssetBundlesFolderName);
        FileUtility.SafeDeleteDir(outputPath);
                
        //打包成功
        if (report.summary.result == BuildResult.Succeeded) {
            Debug.Log("Build succeeded: " + report.summary.totalSize + " bytes");
            EditorUtility.RevealInFinder(buildFolder);
        }
        if (report.summary.result == BuildResult.Failed) {
            Debug.Log("Build failed");
        }
    }

    public static void BuildXCode(string channelName, bool isTest)
    {
        BuildTarget buildTarget = BuildTarget.iOS;
        PackageUtils.CopyAssetBundlesToStreamingAssets(buildTarget, channelName);
        string buildFolder = Path.Combine(System.Environment.CurrentDirectory, XCodeOutputPath);
        buildFolder = Path.Combine(buildFolder, $"{channelName}_{PackageTool.appVersion}_{PackageTool.resVersion}");
        FileUtility.CheckDirAndCreateWhenNeeded(buildFolder);
                
        BaseChannel channel = ChannelManager.Instance.CreateChannel(channelName);
        SetPlayerSetting(channel);

//        PackageUtils.CheckAndAddSymbolIfNeeded(buildTarget, channelName);
        var report = BuildPipeline.BuildPlayer(GetBuildScenes(), buildFolder, buildTarget, BuildOptions.None);

        string outputPath = Path.Combine(Application.persistentDataPath, AssetBundleConfig.AssetBundlesFolderName);
        FileUtility.SafeDeleteDir(outputPath);
        
        //打包成功
        if (report.summary.result == BuildResult.Succeeded) {
            Debug.Log("Build succeeded: " + report.summary.totalSize + " bytes");
            EditorUtility.RevealInFinder(buildFolder);
        }
        if (report.summary.result == BuildResult.Failed) {
            Debug.Log("Build failed");
        }
    }

    
    public static void ExportIPA(string xcodePath)
    {
        Debug.Log("Start ExportIPA");
        string source = Path.Combine(Application.dataPath, "../Build/iOS/AutoPack");
        string target = Path.Combine(xcodePath, "AutoPack");
        if(Directory.Exists(target))
            Directory.Delete(target, true);
        FileUtil.CopyFileOrDirectoryFollowSymlinks(source, target);

        string fileName = "AutoPack/ExportIPA.sh";
        Process process = new Process();
        process.StartInfo.FileName = "sh";
        process.StartInfo.Arguments = fileName;
        process.StartInfo.WorkingDirectory = xcodePath;
        Debug.Log(process.StartInfo.FileName+" "+process.StartInfo.Arguments);

        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.OutputDataReceived += OnOutputDataReceived;
        process.ErrorDataReceived += OnErrorDataReceived;
        process.Start();
        process.BeginOutputReadLine();

        Debug.Log("End ExportIPA");
    }
    
    private static void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e != null && !string.IsNullOrEmpty(e.Data))
        {
            Debug.LogWarning(e.Data);
        }
    }

    private static void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e != null && !string.IsNullOrEmpty(e.Data))
        {
            Debug.LogError(e.Data);
        }
    }

    static string[] GetBuildScenes()
	{
		List<string> names = new List<string>();
		foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
        {
            if (e != null && e.enabled)
            {
                names.Add(e.path);
            }
        }
        return names.ToArray();
    }
}
