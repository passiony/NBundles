using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// added by wsh @ 2017-12-21
/// 功能：assetbundle管理类，为外部提供统一的资源加载界面、协调Assetbundle各个子系统的运行
/// 注意：
/// 1、抛弃Resources目录的使用，官方建议：https://unity3d.com/cn/learn/tutorials/temas/best-practices/resources-folder?playlist=30089
/// 2、提供Editor和Simulate模式，前者不适用Assetbundle，直接加载资源，快速开发；后者使用Assetbundle，用本地服务器模拟资源更新
/// 3、场景不进行打包，场景资源打包为预设
/// 4、只提供异步接口，所有加载按异步进行
/// 5、采用LZMA压缩方式，性能瓶颈在Assetbundle加载上，ab加载异步，asset加载同步，ab加载后导出全部asset并卸载ab
/// 6、所有公共ab包（被多个ab包依赖）常驻内存，非公共包加载asset以后立刻卸载，被依赖的公共ab包会随着资源预加载自动加载并常驻内存
/// 7、随意卸载公共ab包可能导致内存资源重复，最好在切换场景时再手动清理不需要的公共ab包
/// 8、常驻包（公共ab包）引用计数不为0时手动清理无效，正在等待加载的所有ab包不能强行终止---一旦发起创建就一定要等操作结束，异步过程进行中清理无效
/// 9、切换场景时最好预加载所有可能使用到的资源，所有加载器用完以后记得Dispose回收，清理GC时注意先释放所有Asset缓存
/// 10、逻辑层所有Asset路径带文件类型后缀，且是AssetBundleConfig.ResourcesFolderName下的相对路径，注意：路径区分大小写
/// TODO：
/// 1、区分场景常驻包和全局公共包，切换场景时自动卸载场景公共包
/// 使用说明：
/// 1、由Asset路径获取AssetName、AssetBundleName：ParseAssetPathToNames
/// 2、设置常驻(公共)ab包：SetAssetBundleResident(assebundleName, true)---公共ab包已经自动设置常驻
/// 2、(预)加载资源：var loader = LoadAssetBundleAsync(assetbundleName)，协程等待加载完毕后Dispose：loader.Dispose()
/// 3、加载Asset资源：var loader = LoadAssetAsync(assetPath, TextAsset)，协程等待加载完毕后Dispose：loader.Dispose()
/// 4、离开场景清理所有Asset缓存：ClearAssetsCache()，UnloadUnusedAssetBundles(), Resources.UnloadUnusedAssets()
/// 5、离开场景清理必要的(公共)ab包：TryUnloadAssetBundle()，注意：这里只是尝试卸载，所有引用计数不为0的包（还正在加载）不会被清理
/// </summary>

namespace AssetBundles
{
    public class AssetBundleManager : MonoSingleton<AssetBundleManager>
    {
        public AssetBundleUpdater abUpdater;
        public AssetBundleLoader abLoader;
        public AssetBundleVersion abVersion;

        protected override void Init()
        {
            abUpdater = new AssetBundleUpdater();
            abLoader = new AssetBundleLoader();
            abVersion = new AssetBundleVersion();
        }

        #region Loader

        public IEnumerator Initialize()
        {
            yield return abLoader.Initialize();

            yield return abVersion.InitAppVersion();
        }

        public IEnumerator Cleanup()
        {
            return abLoader.Cleanup();
        }
        
        void Update()
        {
            abLoader.Update();
        }


        public void SetAssetBundleResident(string assetbundleName, bool resident)
        {
            abLoader.SetAssetBundleResident(assetbundleName, resident);
        }

        public void AddAssetbundleAssetsCache(string assetbundleName, Type assetType, bool isAtlas, string postfix = null)
        {
            abLoader.AddAssetbundleAssetsCache(assetbundleName, assetType, isAtlas, postfix);
        }

        public void AddAssetbundleAtlasCache(string assetbundleName, string postfix = null)
        {
            abLoader.AddAssetbundleAtlasCache(assetbundleName, postfix);
        }

        public Manifest GetManifest
        {
            get { return abLoader.curManifest; }
        }
        
        public AssetBundle GetAssetBundleCache(string assetbundleName)
        {
            return abLoader.GetAssetBundleCache(assetbundleName);
        }
        
        public object GetAssetCache(string assetName)
        {
            return abLoader.GetAssetCache(assetName);
        }
        
        public List<string> GetAllAssetNames(string bundleName)
        {
            return abLoader.GetAllAssetNames(bundleName);
        }
        
        public bool IsAssetBundleLoaded(string assetbundleName)
        {
            return abLoader.IsAssetBundleLoaded(assetbundleName);
        }

        public BaseAssetAsyncLoader LoadAssetAsync(string assetPath, System.Type assetType, bool isAtlas = false)
        {
            return abLoader.LoadAssetAsync(assetPath, assetType, isAtlas);
        }
        
        // 异步请求Assetbundle资源，AB是否缓存取决于是否设置为常驻包，Assets一律缓存，处理依赖
        public BaseAssetBundleAsyncLoader LoadAssetBundleAsync(string assetbundleName, Type type, bool isatlas = false)
        {
            return abLoader.LoadAssetBundleAsync(assetbundleName, type, isatlas);
        }

        
        // 从资源服务器下载Assetbundle资源，非AB（不计引用计数、不缓存），Creater使用后记得回收
        public UnityWebAssetRequester DownloadAssetBundleAsync(string filePath)
        {
            return abLoader.DownloadAssetBundleAsync(filePath);
        }

        // 从服务器下载网页内容，需提供完整url，非AB（不计引用计数、不缓存），Creater使用后记得回收
        public UnityWebAssetRequester DownloadWebResourceAsync(string url)
        {
            return abLoader.DownloadWebResourceAsync(url);
        }

        // 从资源服务器下载非Assetbundle资源，非AB（不计引用计数、不缓存），Creater使用后记得回收
        public UnityWebAssetRequester DownloadAssetFileAsync(string filePath)
        {
            return abLoader.DownloadAssetFileAsync(filePath);
        }

        // 本地异步请求非Assetbundle资源，非AB（不计引用计数、不缓存），Creater使用后记得回收
        public UnityWebAssetRequester RequestAssetFileAsync(string filePath, bool streamingAssetsOnly = true)
        {
            return abLoader.RequestAssetFileAsync(filePath, streamingAssetsOnly);
        }
        
        
        // 本地异步请求Assetbundle资源，不计引用计数、不缓存，Creater使用后记得回收
        public AssetBundleRequester RequestAssetBundleAsync(string assetbundleName)
        {
            return abLoader.RequestAssetBundleAsync(assetbundleName);
        }

        // 用于卸载无用AB包：如果该AB包还在使用，则卸载失败
        public bool UnloadUnusedAssetBundle(string assetbundleName, bool unloadAllLoadedObjects = false,
            bool unloadDependencies = true)
        {
            return abLoader.UnloadUnusedAssetBundle(assetbundleName, unloadAllLoadedObjects, unloadDependencies);
        }

        public int UnloadAllUnusedResidentAssetBundles(bool unloadAllLoadedObjects = false, bool unloadDependencies = true)
        {
            return abLoader.UnloadAllUnusedResidentAssetBundles(unloadAllLoadedObjects,unloadDependencies);
        }

        public void ClearAssetsCache()
        {
            abLoader.ClearAssetsCache();
        }

        public void ClearBundleCache(string[] bundleNames)
        {
            abLoader.ClearBundleCache(bundleNames);
        }

        #endregion
    }

}
