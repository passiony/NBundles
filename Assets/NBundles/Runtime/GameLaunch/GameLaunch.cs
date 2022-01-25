using UnityEngine;
using System.Collections;
using AssetBundles;
using System;

public class GameLaunch : MonoBehaviour
{
    const string launchPrefabPath = "UI/Prefabs/UILoading/UILoading.prefab";
    const string noticeTipPrefabPath = "UI/Prefabs/Common/UINoticeTip.prefab";
        
    GameObject launchPrefab;
    GameObject noticeTipPrefab;
    AssetBundleUpdater updater;

    private Transform _launchLayer;
    public Transform LaunchLayer
    {
        get
        {
            if (_launchLayer == null)
            {
                _launchLayer = transform.Find("UIRoot/Canvas");
            }
            return _launchLayer;
        }
    }

    
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
        Input.multiTouchEnabled = true;
        GameUtility.NeverSleep();
        
        // 启动资源管理模块
        var start = DateTime.Now;
        yield return AssetBundleManager.Instance.Initialize();

        Logger.Log(string.Format("AssetBundleManager Initialize use {0}ms", (DateTime.Now - start).TotalMilliseconds));
        yield return 0;
        
        // 初始化UI界面
        start = DateTime.Now;
        yield return InitLaunchPrefab();
        UILauncher.Instance.SetSatus("Loading...");
        Logger.Log(string.Format("Load LaunchPrefab use {0}ms", (DateTime.Now - start).TotalMilliseconds));
        yield return 0;
        
        // 初始化App版本
        start = DateTime.Now;
        var updater = AssetBundleManager.Instance.abUpdater;
        yield return updater.RequestRemoteUrl();
        Logger.Log(string.Format("Init AppVersion use {0}ms", (DateTime.Now - start).TotalMilliseconds));
        yield return 0;

        start = DateTime.Now;
        yield return InitNoticeTipPrefab();
        Logger.Log(string.Format("Load noticeTipPrefab use {0}ms", (DateTime.Now - start).TotalMilliseconds));
        
        // 更新
        if (updater.HasUpdate)
        {
            start = DateTime.Now;
            yield return updater.StartCheckUpdate();
            Logger.Log(string.Format("CheckUpdate use {0}ms", (DateTime.Now - start).TotalMilliseconds));
        }

        gameObject.AddComponent<Sample>();
    }

    GameObject InstantiateGameObject(GameObject prefab)
    {
        GameObject go = GameObject.Instantiate(prefab, LaunchLayer);
        go.name = prefab.name;
        return go;
    }
    IEnumerator InitNoticeTipPrefab()
    {
        var loader = AssetBundleManager.Instance.LoadAssetAsync(noticeTipPrefabPath, typeof(GameObject));
        yield return loader;
        noticeTipPrefab = loader.asset as GameObject;
        loader.Dispose();
        if (noticeTipPrefab == null)
        {
            Logger.LogError("LoadAssetAsync noticeTipPrefab err : " + noticeTipPrefabPath);
            yield break;
        }
        var go = InstantiateGameObject(noticeTipPrefab);
        UINoticeTip.Instance.UIGameObject = go;
    }

    IEnumerator InitLaunchPrefab()
    {
        var loader = AssetBundleManager.Instance.LoadAssetAsync(launchPrefabPath,typeof(GameObject));
        yield return loader;
        launchPrefab = loader.asset as GameObject;
        loader.Dispose();
        if (launchPrefab == null)
        {
            Logger.LogError("LoadAssetAsync launchPrefab err : " + launchPrefabPath);
            yield break;
        }
        var go = InstantiateGameObject(launchPrefab);
        UILauncher.Instance.UIGameObject = go;
    }
}
