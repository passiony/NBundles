using UnityEditor;
using UnityEngine;
using System.IO;
using GameChannel;
using AssetBundles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Codice.CM.Client.Differences;

/// <summary>
/// added by wsh @ 2018.01.03
/// 说明：打包工具
/// </summary>
public class PackageTool : EditorWindow
{
    private static string hotFixTime;
    private static BuildTarget buildTarget = BuildTarget.Android;
    private static ChannelType channelType = ChannelType.Test;
    public static string appVersion = "1.0.0";
    public static string resVersion = "1.0.0.0";
    public static string serVersion = "1.0.0.0";
    public static string buildCode = "1";
    private static ServerType localServerType;
    private static bool androidBuildABForPerChannel;
    private static bool iosBuildABForPerChannel;
    private static bool buildABSForPerChannel;
    private static bool buildABAutoIncreaseAppVersion;
    private static bool buildABAutoIncreaseResVersion;
    private static Vector2 scrollPosition;
    
    private static EditorWindow instance;
    private static EditorWindow Instance
    {
        get
        {
            if (instance == null)
            {
                instance = EditorWindow.GetWindow(typeof(PackageTool));
            }

            return instance;
        }
    }


    [MenuItem("Tools/Package _F3", false, 0)]
    static void Init()
    {
        instance = EditorWindow.GetWindow(typeof(PackageTool));
        instance.autoRepaintOnSceneChange = true;
    }

    void OnEnable()
    {
        SymbolsSetting.Init();

        buildTarget = EditorUserBuildSettings.activeBuildTarget;
        channelType = PackageUtils.GetCurSelectedChannel();

        ReadLocalVersionFile(buildTarget, channelType);
        LoadServerResVersion(true);

        buildCode = PackageUtils.GetBuildCode();
        localServerType = PackageUtils.GetLocalServerType();

        androidBuildABForPerChannel = PackageUtils.GetAndroidBuildABForPerChannelSetting();
        iosBuildABForPerChannel = PackageUtils.GetIOSBuildABForPerChannelSetting();

        buildABAutoIncreaseAppVersion = PackageUtils.GetAutoIncreaseAppVersionSetting();
        buildABAutoIncreaseResVersion = PackageUtils.GetAutoIncreaseResVersionSetting();
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(5);
        var build = (BuildTarget) EditorGUILayout.EnumPopup("Build Target : ", buildTarget);
        GUILayout.Space(5);
        var channel = (ChannelType) EditorGUILayout.EnumPopup("Build Channel : ", channelType);

        bool buildTargetSupport = false;
        if (buildTarget != BuildTarget.Android && buildTarget != BuildTarget.iOS)
        {
            GUILayout.Label("Error : Only android or iOS build target supported!!!");
        }
        else
        {
            buildTargetSupport = true;
            SetBuildTarget(build, channel);
        }

        GUILayout.Space(5);
        GUILayout.EndVertical();

        if (buildTargetSupport)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginVertical("box");
            DrawConfigGUI();
            DrawLocalServerGUI();
            DrawAssetBundlesGUI();
            DrawBuildPlayerGUI();
            GUILayout.EndVertical();

            DrawAudoBuildGUI();
            GUILayout.EndScrollView();
        }
    }

    #region AB相关配置

    void DrawAssetBundlesConfigGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[AssetBundles Config]-------------");
        GUILayout.Space(3);

        // 是否为每个channel打一个AB包
        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Build For Per Channel : ", GUILayout.Width(150));
        if (buildTarget == BuildTarget.Android)
        {
            buildABSForPerChannel = EditorGUILayout.Toggle(androidBuildABForPerChannel, GUILayout.Width(50));
            if (buildABSForPerChannel != androidBuildABForPerChannel)
            {
                androidBuildABForPerChannel = buildABSForPerChannel;
                PackageUtils.SaveAndroidBuildABForPerChannelSetting(buildABSForPerChannel);
            }
        }
        else
        {
            buildABSForPerChannel = EditorGUILayout.Toggle(iosBuildABForPerChannel, GUILayout.Width(50));
            if (buildABSForPerChannel != iosBuildABForPerChannel)
            {
                iosBuildABForPerChannel = buildABSForPerChannel;
                PackageUtils.SaveIOSBuildABForPerChannelSetting(buildABSForPerChannel);
            }
        }

        if (GUILayout.Button("Run All Checkers", GUILayout.Width(200)))
        {
            bool checkChannel = PackageUtils.BuildAssetBundlesForPerChannel(buildTarget);
            PackageUtils.CheckAndRunAllCheckers(checkChannel, true);
        }

        GUILayout.EndHorizontal();
    }

    #endregion

    #region 资源配置GUI

    void DrawConfigGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Config]-------------");

        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Label("app_version", GUILayout.Width(80));
        string curBundleVersion = GUILayout.TextField(appVersion, GUILayout.Width(100));
        if (curBundleVersion != appVersion)
        {
            SetAppVersion(curBundleVersion);
        }

        GUILayout.Label("Auto build will increase sub version.", GUILayout.Width(220));
        if (PackageUtils.BuildAssetBundlesForPerChannel(buildTarget))
        {
            if (GUILayout.Button("Validate All Server Version", GUILayout.Width(200)))
            {
                ValidateAllServerVersions();
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Label("res_version", GUILayout.Width(80));
        EditorGUI.BeginDisabledGroup(true);
        string curResVersion = GUILayout.TextField(resVersion, GUILayout.Width(100));
        if (curResVersion != resVersion)
        {
            resVersion = curResVersion;
            SaveAllLocalVersionFile(buildTarget, channelType);
        }

        EditorGUI.EndDisabledGroup();

        GUILayout.Label("Auto build will increase sub version.", GUILayout.Width(220));

        if (PackageUtils.BuildAssetBundlesForPerChannel(buildTarget))
        {
            if (GUILayout.Button("Validate All Local Version", GUILayout.Width(200)))
            {
                ValidateAllLocalVersions();
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Label("ser_Version", GUILayout.Width(80));
        EditorGUI.BeginDisabledGroup(true);
        var curSerVersion = GUILayout.TextField(serVersion, GUILayout.Width(100));
        if (curSerVersion != serVersion)
        {
            serVersion = curSerVersion;
        }

        EditorGUI.EndDisabledGroup();
        GUILayout.Label("server res version...", GUILayout.Width(220));
        GUILayout.EndHorizontal();

        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Build Code", GUILayout.Width(80));
        var curBuildCode = GUILayout.TextField(buildCode, GUILayout.Width(100));
        if (curBuildCode != buildCode)
        {
            buildCode = curBuildCode;
            PackageUtils.SaveBuildCode(buildCode);
        }

        GUILayout.Space(3);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Load Server Version", GUILayout.Width(200)))
        {
            LoadServerResVersion();
        }

        if (GUILayout.Button("Save Version To Server", GUILayout.Width(200)))
        {
            SaveServerVersionFile();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    #endregion

    #region 本地服务器配置GUI

    void DrawLocalServerGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Server IP]-------------");
        GUILayout.Space(3);

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        var curSelected =
            (ServerType) EditorGUILayout.EnumPopup("Server Type : ", localServerType, GUILayout.Width(300));
        bool typeChanged = curSelected != localServerType;
        if (typeChanged)
        {
            SetServerType(curSelected);
        }

        GUILayout.Space(3);
        GUILayout.Label("Http:", GUILayout.Width(30));
        GUILayout.Label(URLSetting.BASE_URL);
        GUILayout.EndHorizontal();
        GUILayout.Space(3);

        GUILayout.EndVertical();
    }

    #endregion

    #region AB相关操作GUI

    void DrawAssetBundlesGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Build AssetBundles]-------------");
        GUILayout.Space(3);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Auto Increase Sub Version", GUILayout.Width(180));
        var autoIncrease = EditorGUILayout.Toggle(buildABAutoIncreaseResVersion, GUILayout.Width(50));
        if (autoIncrease != buildABAutoIncreaseResVersion)
        {
            buildABAutoIncreaseResVersion = autoIncrease;
            PackageUtils.SaveAutoIncreaseResVersionSetting(autoIncrease);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
//        if (buildABSForPerChannel)
//        {
//            if (GUILayout.Button("Current Channel Only", GUILayout.Width(200)))
//            {
//                EditorApplication.delayCall += BuildAssetBundlesForCurrentChannel;
//            }
//            if (GUILayout.Button("For All Channels", GUILayout.Width(200)))
//            {
//                EditorApplication.delayCall += BuildAssetBundlesForAllChannels;
//            }
//            if (GUILayout.Button("Open Current Output", GUILayout.Width(200)))
//            {
//                AssetBundleMenuItems.ToolsOpenOutput();
//            }
//        }
//        else
        {
            if (GUILayout.Button("Execute Build", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += () => { BuildAssetBundlesForCurrentChannel(); };
            }

            if (GUILayout.Button("Open Output Folder", GUILayout.Width(200)))
            {
                AssetBundleMenuItems.ToolsOpenOutput();
            }

            if (GUILayout.Button("Copy To StreamingAsset", GUILayout.Width(200)))
            {
                AssetBundleMenuItems.ToolsCopyAssetbundles();
            }
        }
        GUILayout.EndHorizontal();
    }

    #endregion

    #region 打包相关GUI

    void DrawBuildAndroidPlayerGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Build Android Player]-------------");
        GUILayout.Space(3);

        GUILayout.BeginHorizontal();
        if (PackageUtils.BuildAssetBundlesForPerChannel(buildTarget))
        {
//            if (GUILayout.Button("Copy SDK Resources", GUILayout.Width(200)))
//            {
//                EditorApplication.delayCall += CopyAndroidSDKResources;
//            }
            if (GUILayout.Button("Execute Build", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += () => { BuildAndroidPlayerForCurrentChannel(); };
            }

            if (GUILayout.Button("For All Channels", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildAndroidPlayerForAllChannels;
            }

            if (GUILayout.Button("Open Output Folder", GUILayout.Width(200)))
            {
                var folder = PackageUtils.GetChannelOutputPath(buildTarget, channelType.ToString());
                ProcessUtils.ExplorerFolder(folder);
            }
        }
        else
        {
//            if (GUILayout.Button("Copy SDK Resource", GUILayout.Width(200)))
//            {
//                EditorApplication.delayCall += CopyAndroidSDKResources;
//            }
            if (GUILayout.Button("Execute Build", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += () => { BuildAndroidPlayerForCurrentChannel(); };
            }

            if (GUILayout.Button("Open Output Folder", GUILayout.Width(200)))
            {
                var folder = PackageUtils.GetChannelOutputPath(buildTarget, channelType.ToString());
                ProcessUtils.ExplorerFolder(folder);
            }
        }

        GUILayout.EndHorizontal();
    }

    void DrawBuildIOSPlayerGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Build IOS Player]-------------");
        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        if (PackageUtils.BuildAssetBundlesForPerChannel(buildTarget))
        {
            if (GUILayout.Button("Execute Build", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += () => { BuildIOSPlayerForCurrentChannel(); };
            }

            if (GUILayout.Button("For All Channels", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += BuildIOSPlayerForAllChannels;
            }

            if (GUILayout.Button("Open Current Output", GUILayout.Width(200)))
            {
                var folder = Path.Combine(System.Environment.CurrentDirectory, BuildPlayer.XCodeOutputPath);
                ProcessUtils.ExplorerFolder(folder);
            }
        }
        else
        {
            if (GUILayout.Button("Execute Build", GUILayout.Width(200)))
            {
                EditorApplication.delayCall += () => { BuildIOSPlayerForCurrentChannel(); };
            }

            if (GUILayout.Button("Open Output Folder", GUILayout.Width(200)))
            {
                var folder = Path.Combine(System.Environment.CurrentDirectory, BuildPlayer.XCodeOutputPath);
                ProcessUtils.ExplorerFolder(folder);
            }
        }

        GUILayout.EndHorizontal();
    }

    void DrawBuildPlayerGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Build]-------------");
        GUILayout.Space(3);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Auto Increase Sub Version", GUILayout.Width(180));
        var autoIncrease = EditorGUILayout.Toggle(buildABAutoIncreaseAppVersion, GUILayout.Width(50));
        if (autoIncrease != buildABAutoIncreaseAppVersion)
        {
            buildABAutoIncreaseAppVersion = autoIncrease;
            PackageUtils.SaveAutoIncreaseAppVersionSetting(autoIncrease);
        }

        GUILayout.EndHorizontal();

        if (buildTarget == BuildTarget.Android)
        {
            DrawBuildAndroidPlayerGUI();
        }
        else if (buildTarget == BuildTarget.iOS)
        {
            DrawBuildIOSPlayerGUI();
        }
    }

    #endregion

    #region 自动化Build

    void DrawAudoBuildGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label("-------------[Auto Build]-------------");
        GUILayout.Space(3);

        GUILayout.BeginVertical("box");
        if (buildABAutoIncreaseResVersion)
            GUILayout.Label("increase local and server res version");
        else
            GUILayout.Label("increase only server res version");

        if (GUILayout.Button("Build Bundle", GUILayout.Width(200)))
        {
            string gameMainPath = Path.Combine(Application.dataPath, "LuaScripts/GameMain.lua");
            string[] infoTemp = File.ReadAllLines(gameMainPath);
            hotFixTime = DateTime.Now.ToString();
            string timeTemp = $"local updateBundleTime = \"{hotFixTime}\"";
            infoTemp[0] = timeTemp;
            File.WriteAllLines(gameMainPath, infoTemp);
            EditorApplication.delayCall += BuildBundleAutoIncreaseVersion;
        }

        GUILayout.Space(5);

        if (buildABAutoIncreaseAppVersion)
            GUILayout.Label("increase local and server app version");
        else
            GUILayout.Label("increase only server app version");

        if (GUILayout.Button("Build App", GUILayout.Width(200)))
        {
            EditorApplication.delayCall += BuildPlayerAutoIncreaseVersion;
        }

        GUILayout.EndVertical();
    }

    public static void BuildBundleAutoIncreaseVersion()
    {
        BuildAssetBundlesForCurrentChannel(true);
        IncreaseServerVersionFile(false, () =>
        {
//            PushWechat($"补丁包(日志对比时间:{hotFixTime})");
        });
//        AwscliGeneratorHelper.ExcuteUpload();
    }

    public static void BuildPlayerAutoIncreaseVersion()
    {
        if (buildABAutoIncreaseAppVersion)
        {
            IncreaseAppSubVersion();
        }

        if (buildABAutoIncreaseResVersion)
        {
            IncreaseResSubVersion();
        }

        AutoBuildPlayer();
    }

    public static void AutoBuildPlayer()
    {
        BuildPlayer.BuildAssetBundles(buildTarget, channelType, resVersion);
        PackageUtils.ClearPersistentAssets();
        PackageUtils.CopyCurSettingAssetBundlesToStreamingAssets();

        if (buildTarget == BuildTarget.Android)
        {
            BuildPlayer.BuildAndroid(channelType.ToString(), channelType == ChannelType.Test);
        }
        else if (buildTarget == BuildTarget.iOS)
        {
            BuildPlayer.BuildXCode(channelType.ToString(), channelType == ChannelType.Test);
        }

        SaveServerVersionFile(false, () =>
        {
//            PushWechat("App包");
        });
    }

    public static void SetAppVersion(string version)
    {
        appVersion = version;
        PlayerSettings.bundleVersion = version;
        resVersion = appVersion + ".1";

        SaveAllLocalVersionFile(buildTarget, channelType);
    }

    public static void SetBuildCode(string code)
    {
        buildCode = code;
        PackageUtils.SaveBuildCode(buildCode);
    }

    public static void SetBuildTarget(BuildTarget target, ChannelType channel)
    {
        if (buildTarget != target || channel != channelType)
        {
            buildTarget = target;
            channelType = channel;

            SaveAllLocalVersionFile(buildTarget, channelType);
            PackageUtils.SaveCurSelectedChannel(channelType);
        }
    }

    public static void SetServerType(ServerType server)
    {
        localServerType = server;
        PackageUtils.SaveLocalServerType(server);
    }

    #endregion

    #region 资源配置操作

    public static bool ReadLocalVersionFile(BuildTarget target, ChannelType channel)
    {
        // 从资源版本号文件（当前渠道AB输出目录中）加载资源版本号
        string rootPath = PackageUtils.GetAssetBundleOutputPath(target, channel.ToString());

        string app_path = rootPath + "/" + BuildUtility.AppVersionFileName;
        app_path = FileUtility.FormatToUnityPath(app_path);
        appVersion = "0.0.0";
        resVersion = "0.0.0.0";
        channelType = ChannelType.Test;

        string content = FileUtility.SafeReadAllText(app_path);
        if (content == null)
        {
            return false;
        }

        var arr = content.Split('|');
        if (arr.Length >= 2)
        {
            appVersion = arr[0];
            resVersion = arr[1];
            channelType = (ChannelType) Enum.Parse(typeof(ChannelType), arr[2]);
        }

        return true;
    }

    public static void SaveAllLocalVersionFile(BuildTarget target, ChannelType channel)
    {
        string rootPath = PackageUtils.GetAssetBundleOutputPath(target, channel.ToString());

        string path = rootPath + "/" + BuildUtility.AppVersionFileName;
        path = FileUtility.FormatToUnityPath(path);

        string content = appVersion + "|" + resVersion + "|" + channel;
        Debug.Log("save app_version.bytes->" + content);
        // 保存所有版本号信息到资源版本号文件（当前渠道AB输出目录中）
        FileUtility.SafeWriteAllText(path, content);
    }

    public static void LoadServerResVersion(bool silence = false, Action<bool> callback = null)
    {
        /**
        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        StartUpVersionHelper.SelectCurrentVersion(appVersion, (version) =>
        {
            if (version == null)
            {
                string msg = string.Format("No res version file for : \n\nplatform : {0} \nchannel : {1} \n\n",
                    buildTargetName, channelType.ToString());
                if (!silence)
                {
                    EditorUtility.DisplayDialog("Error", msg, "Confirm");
                }
                else Logger.LogError(msg);

                callback?.Invoke(false);
                return;
            }

            appVersion = version.appVersion;
            resVersion = StartUpVersionHelper.GetResVersion(version.resUrl);
            serVersion = version.resVersion;

            // load server version后，先保存本地，同步local version
            SaveAllLocalVersionFile(buildTarget, channelType);
            callback?.Invoke(true);

            if (!silence)
            {
                string msg = "\nApp Version : " + appVersion + "\nS3 Res Version : " + resVersion +
                             "\nServer Res Version : " + version.resVersion;
                EditorUtility.DisplayDialog("Success", string.Format(
                    "Current Versions : \n{0}\n\nplatform : {1} \nchannel : {2} \n\n",
                    msg, buildTargetName, channelType.ToString()), "Confirm");
            }
        });
        **/
    }

    public static void SaveServerVersionFile(bool silence = false, Action callback = null)
    {
        /**
        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        SaveAllLocalVersionFile(buildTarget, channelType);

        StartUpVersionHelper.SaveCurrentVersion(appVersion, resVersion, (version) =>
        {
            serVersion = version.resVersion;
            callback?.Invoke();

            if (!silence)
            {
                string msg = "\nappVersion:" + appVersion + "\nresVersion:" + resVersion + "\nserVersion:" +
                             version.resVersion;
                EditorUtility.DisplayDialog("Success", string.Format(
                    "Save all version file : {0}\n\nplatform : {1} \nchannel : {2} \n\n",
                    msg, buildTargetName, channelType.ToString()), "Confirm");
            }
        });
        **/
    }

    public static async void IncreaseServerVersionFile(bool silence = false, Action callback = null)
    {
        await Task.Delay(100);
        /**
        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        StartUpVersionHelper.IncreaseServerCurrentVersion(appVersion, resVersion, (version) =>
        {
            serVersion = version.resVersion;
            callback?.Invoke();

            if (!silence)
            {
                string local = "\nappVersion:" + appVersion + "\tresVersion:" + resVersion;
                string server = "\nappVersion:" + version.appVersion + "\tresVersion:" + version.resVersion;

                EditorUtility.DisplayDialog("Success", string.Format(
                    "local version: {0}\n\n server version : {1}\n\nplatform : {2} \nchannel : {3} \n\n",
                    local, server, buildTargetName, channelType.ToString()), "Confirm");
            }
        });
        **/
    }

    public static void ValidateAllServerVersions()
    {
        /**
        StartUpVersionHelper.SelectAllVersion((versions) =>
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} Channels : \n\n", versions.Count);

            foreach (var current in versions)
            {
                var channel = current.channel;
                var appV = current.appVersion;
                var resV = current.resVersion;

                sb.AppendFormat("Channel : {0}, AppVersion : {1}, ResVersion : {2}\n", channel, appV, resV);
            }

            string result = sb.ToString();
            Debug.LogWarning(result);

            if (result.Length > 400) result = result.Substring(0, 400);
            EditorUtility.DisplayDialog("Result", result, "Confirm");
        });
        **/
    }

    public static void ValidateAllLocalVersions()
    {
        // 校验所有渠道AB输出目录下资源版本号信息
        Dictionary<ChannelType, string[]> versionMap = new Dictionary<ChannelType, string[]>();

        foreach (var current in (ChannelType[]) Enum.GetValues(typeof(ChannelType)))
        {
            string[] versions = PackageUtils.ReadAppAndResVersionFile(buildTarget, current);
            if (versions == null)
            {
                continue;
            }

            versionMap.Add(current, versions);
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("{0} Channels : \n\n", versionMap.Count);

        foreach (var current in versionMap)
        {
            var channel = current.Key.ToString();
            var appV = current.Value[0];
            var resV = current.Value[1];

            sb.AppendFormat("Channel : {0}, AppVersion : {1}, ResVersion : {2}\n", channel, appV, resV);
        }

        string result = sb.ToString();
        Debug.LogWarning(result);

        if (result.Length > 400) result = result.Substring(0, 400);
        EditorUtility.DisplayDialog("Result", result, "Confirm");
    }

    void SaveAllVersionFile()
    {
        // 保存当前版本号信息到所有渠道AB输出目录
        foreach (var current in (ChannelType[]) Enum.GetValues(typeof(ChannelType)))
        {
            SaveAllLocalVersionFile(buildTarget, current);
        }

        EditorUtility.DisplayDialog("Success", "Save all version files to all channels done!", "Confirm");
    }

    #endregion

    #region AB相关操作

    public static void IncreaseResSubVersion()
    {
        // 每一次构建资源，子版本号自增，注意：前两个字段这里不做托管，自行编辑设置
        resVersion = PackageUtils.IncreaseResSubVersion(resVersion);
        Instance.Repaint();
        Debug.Log("IncreaseResSubVersion:" + resVersion);
        SaveServerVersionFile(true);
    }

    public static void BuildAssetBundlesForCurrentChannel(bool silence = false)
    {
        if (buildABAutoIncreaseResVersion)
        {
            IncreaseResSubVersion();
        }

        var start = DateTime.Now;
        BuildPlayer.BuildAssetBundles(buildTarget, channelType, resVersion);
        if (!silence)
        {
            var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
            EditorUtility.DisplayDialog("Success", string.Format(
                "Build AssetBundles for : \n\nplatform : {0} \nchannel : {1} \n\ndone! use {2}s",
                buildTargetName, channelType, (DateTime.Now - start).TotalSeconds), "Confirm");
        }

//        AwscliGeneratorHelper.GenerateAwsCliFile(resVersion);
    }

    public static void BuildAssetBundlesForAllChannels()
    {
        if (buildABAutoIncreaseResVersion)
        {
            IncreaseResSubVersion();
        }

        var start = DateTime.Now;
        BuildPlayer.BuildAssetBundlesForAllChannels(buildTarget);

        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success", string.Format(
            "Build AssetBundles for : \n\nplatform : {0} \nchannel : all \n\ndone! use {1}s",
            buildTargetName, (DateTime.Now - start).TotalSeconds), "Confirm");

        AssetBundleMenuItems.ToolsCopyAssetbundles();
    }

    public static bool CheckSymbolsToCancelBuild()
    {
        var buildTargetGroup = buildTarget == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS;
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

        if (string.IsNullOrEmpty(symbols))
        {
            symbols = "null";
        }

        bool yes = EditorUtility.DisplayDialog("Build Symbol Warning",
            string.Format("Now symbols : \n\n{0}\n\nAre you sure to build ?", symbols),
            "Yes", "No");
        return !yes;
    }

    #endregion

    #region 打包相关操作

    public static void IncreaseAppSubVersion()
    {
        // 每一次构建安装包，子版本号自增，注意：前两个字段这里不做托管，自行到PlayerSetting中设置
        appVersion = PackageUtils.IncreaseResSubVersion(appVersion);
        resVersion = appVersion + ".1";
        serVersion = resVersion;

        Instance.Repaint();
        Debug.Log("IncreaseAppSubVersion:" + appVersion);
        SaveServerVersionFile(true);
    }

    public static void CopyAndroidSDKResources()
    {
        PackageUtils.CopyAndroidSDKResources(channelType.ToString());
    }

    public static void BuildAndroidPlayerForCurrentChannel(bool silence = false)
    {
        if (!silence && CheckSymbolsToCancelBuild())
        {
            return;
        }

        if (buildABAutoIncreaseAppVersion)
        {
            IncreaseAppSubVersion();
        }

        var start = DateTime.Now;
        BuildPlayer.BuildAndroid(channelType.ToString(), channelType == ChannelType.Test);
        if (!silence)
        {
            var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
            EditorUtility.DisplayDialog("Success",
                string.Format("Build player for : \n\nplatform : {0} \nchannel : {1} \n\ndone! use {2}s",
                    buildTargetName, channelType, (DateTime.Now - start).TotalSeconds), "Confirm");
        }
    }

    public static void BuildAndroidPlayerForAllChannels()
    {
        if (CheckSymbolsToCancelBuild())
        {
            return;
        }

        if (buildABAutoIncreaseAppVersion)
        {
            IncreaseAppSubVersion();
        }

        var start = DateTime.Now;
        foreach (var current in (ChannelType[]) Enum.GetValues(typeof(ChannelType)))
        {
            BuildPlayer.BuildAndroid(current.ToString(), current == ChannelType.Test);
        }

        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success",
            string.Format("Build player for : \n\nplatform : {0} \nchannel : all \n\ndone! use {2}s", buildTargetName,
                (DateTime.Now - start).TotalSeconds), "Confirm");
    }

    public static void BuildIOSPlayerForCurrentChannel(bool silence = false)
    {
        if (!silence && CheckSymbolsToCancelBuild())
        {
            return;
        }

        if (buildABAutoIncreaseAppVersion)
        {
            IncreaseAppSubVersion();
        }

        var start = DateTime.Now;
        BuildPlayer.BuildXCode(channelType.ToString(), channelType == ChannelType.Test);
        if (!silence)
        {
            var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
            EditorUtility.DisplayDialog("Success",
                string.Format("Build player for : \n\nplatform : {0} \nchannel : {1} \n\ndone! use {2}s",
                    buildTargetName, channelType, (DateTime.Now - start).TotalSeconds), "Confirm");
        }
    }

    public static void BuildIOSPlayerForAllChannels()
    {
        if (CheckSymbolsToCancelBuild())
        {
            return;
        }

        if (buildABAutoIncreaseAppVersion)
        {
            IncreaseAppSubVersion();
        }

        var start = DateTime.Now;
        foreach (var current in (ChannelType[]) Enum.GetValues(typeof(ChannelType)))
        {
            BuildPlayer.BuildXCode(current.ToString(), channelType == ChannelType.Test);
        }

        var buildTargetName = PackageUtils.GetPlatformName(buildTarget);
        EditorUtility.DisplayDialog("Success",
            string.Format("Build player for : \n\nplatform : {0} \nchannel : all \n\ndone! use {2}s", buildTargetName,
                (DateTime.Now - start).TotalSeconds), "Confirm");
    }

    #endregion
}