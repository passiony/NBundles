using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AssetBundles
{
    public class VersionList
    {
        private const char splitKey = '|';
        private Dictionary<string, string> clientData = new Dictionary<string, string>();
        private Dictionary<string, string> serverData = new Dictionary<string, string>();
        private Dictionary<string, string> tempData = new Dictionary<string, string>();
        private Dictionary<string, int> BundleSize = new Dictionary<string, int>();
        
         public IEnumerator LoadLocalVersionList()
        {
            var versionsPath = AssetBundleUtility.GetPersistentDataPath(BuildUtility.VersionsFileName);
            var versionTxt = FileUtility.SafeReadAllText(versionsPath);
            if (string.IsNullOrEmpty(versionTxt))
            {
                var request = AssetBundleManager.Instance.RequestAssetFileAsync(BuildUtility.VersionsFileName);
                yield return request;
                versionTxt = request.text.Trim().Replace("\r", "");
                request.Dispose();

                LoadText2Map(versionTxt, ref clientData);
            }
            else
            {
                LoadText2Map(versionTxt, ref clientData);
            }

            LoadTempVersionListHash();
        }

        private bool LoadTempVersionListHash()
        {
            string versionTxt =
                FileUtility.SafeReadAllText(AssetBundleUtility.GetPersistentTempPath(BuildUtility.VersionsFileName));
            if (!string.IsNullOrEmpty(versionTxt))
            {
                LoadText2Map(versionTxt, ref tempData);
                return true;
            }

            return false;
        }

        public IEnumerator LoadServerVersionList(bool isInternal = false)
        {
            var request = AssetBundleManager.Instance.DownloadAssetBundleAsync(BuildUtility.VersionsFileName);
            yield return request;

            if (!string.IsNullOrEmpty(request.error))
            {
                bool retry = false;
                Logger.LogError("Download host manifest :  " + request.assetbundleName + "\n from url : " +
                                request.url + "\n err : " + request.error);
                UINoticeTip.Instance.ShowTwoButtonTip("Connetion Lost", "Update failed. Please try again!",
                    "Cancel", "Retry", () => { retry = false; }, () => { retry = true; });
                yield return UINoticeTip.Instance.WaitForResponse();

                if (retry)
                {
                    yield return LoadServerVersionList(isInternal);
                }

                request.Dispose();
                yield break;
            }

            var versionTxt = request.text.Trim().Replace("\r", "");
            request.Dispose();

            LoadText2SizeMap(versionTxt, ref serverData, ref BundleSize);
            yield break;
        }


        void LoadText2Map(string text, ref Dictionary<string, string> map)
        {
            map.Clear();
            if (string.IsNullOrEmpty(text))
                return;

            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split(splitKey);
                    if (fields.Length > 1)
                    {
                        map.Add(fields[0], fields[1]);
                    }
                }
            }
        }

        void LoadText2SizeMap(string text, ref Dictionary<string, string> hashMap, ref Dictionary<string, int> sizeMap)
        {
            hashMap.Clear();
            sizeMap.Clear();
            if (string.IsNullOrEmpty(text))
                return;

            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split(splitKey);
                    if (fields.Length > 2)
                    {
                        hashMap.Add(fields[0], fields[1]);
                        sizeMap.Add(fields[0], int.Parse(fields[2]));
                    }
                }
            }
        }

        public int GetSize(string path)
        {
            if (BundleSize.TryGetValue(path, out int size))
            {
                return size;
            }

            return 0;
        }

        public int CompareVersionList(ref List<string> updateList)
        {
            int size = 0;
            foreach (var item in serverData)
            {
                bool has1 = clientData.TryGetValue(item.Key, out string ver1);
                bool has2 = tempData.TryGetValue(item.Key, out string ver2);
                //Log.Warning(item.Key+"->"+item.Value+"|"+ver);
                if ((!has1 || !ver1.Equals(item.Value)) && (!has2 || !ver2.Equals(item.Value)))
                {
                    updateList.Add(item.Key);
                    size += GetSize(item.Key);
                }
            }

            if (updateList.Count > 0)
            {
                updateList.Add(BuildUtility.ManifestBundleName);
            }

            Debug.LogWarning(string.Format("updateList count = {0}", updateList.Count));
            return size;
        }

        public void SaveToTempForder(string bundleName)
        {
            if (serverData.TryGetValue(bundleName, out string md5))
            {
                if (tempData.ContainsKey(bundleName))
                    tempData[bundleName] = md5;
                else
                    tempData.Add(bundleName, md5);

                SaveVersionListCahce(tempData);
            }
        }

        public void SaveAllUpdate()
        {
            SaveVersionListCahce(serverData);

            CopyTemp2Data();
        }

        void SaveVersionListCahce(Dictionary<string, string> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dict)
            {
                sb.AppendLine(string.Format("{0}|{1}", item.Key, item.Value));
            }

            //save to disk
            var versionsPath = AssetBundleUtility.GetPersistentTempPath(BuildUtility.VersionsFileName);
            FileUtility.SafeWriteAllText(versionsPath, sb.ToString());
        }

        
        void CopyTemp2Data()
        {
            var tempPath = AssetBundleUtility.GetPersistentTempPath();
            var files = FileUtility.GetAllFilesInFolder(tempPath);
            if (files.Length > 0)
            {
                foreach (string fromfile in files)
                {
                    string tofile = fromfile.Replace(AssetBundleConfig.TempFolderName,AssetBundleConfig.AssetBundlesFolderName);
                    FileUtility.SafeCopyFile(fromfile, tofile);
                    FileUtility.SafeDeleteFile(fromfile);
                }
            
                FileUtility.SafeDeleteDir(tempPath);
            }
        }
        
    }
}