using System;

/// <summary>
/// added by wsh @ 2018.01.04
/// 功能：构建相关配置和通用函数
/// </summary>

public class BuildUtility
{
    public const string ManifestBundleName = "AssetBundles";
    public const string VersionsFileName = "versions.bytes";
    public const string AppVersionFileName = "app_version.bytes";

    public static bool CheckIsNewVersion(string sourceVersion, string targetVersion)
    {
        if (sourceVersion == null || targetVersion == null)
        {
            return false;
        }
        
        string[] sVerList = sourceVersion.Split('.');
        string[] tVerList = targetVersion.Split('.');

        if (sVerList.Length == tVerList.Length)
        {
            int count = sVerList.Length;
            if (count >= 3)
            {
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        int sV0 = int.Parse(sVerList[i]);
                        int tV0 = int.Parse(tVerList[i]);

                        if (tV0 > sV0)
                        {
                            return true;
                        }

                        if (tV0 < sV0)
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(string.Format("parse version error. clientversion: {0} serverversion: {1}\n {2}\n{3}",
                        sourceVersion, targetVersion, ex.Message, ex.StackTrace));
                    return false;
                }
            }
        }
        else if (sVerList.Length < tVerList.Length)
        {
            return true;
        }

        return false;
    }

}
