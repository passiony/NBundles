using System;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine;

public class Sample : MonoBehaviour
{
    Transform canvas;
    private void Awake()
    {
        canvas = GameObject.FindObjectOfType<Canvas>().transform;
    }

    IEnumerator Start()
    {
        yield return LoadPrefab();
        yield return LoadSprite();
        yield return LoadAtlas();
    }

    //加载预制体
    IEnumerator LoadPrefab()
    {
        string path = "UI/Prefabs/UILogin/UILogin.prefab";
        var loader = AssetBundleManager.Instance.LoadAssetAsync(path, typeof(GameObject));
        yield return loader;

        GameObject.Instantiate(loader.asset, canvas);
        loader.Dispose();
    }

    IEnumerator LoadSprite()
    {
        string path = "UI/Atlas/Role/ui_role_61.png";
        var loader = AssetBundleManager.Instance.LoadAssetAsync(path, typeof(Sprite));
        yield return loader;

        var go = new GameObject("sprite");
        go.transform.position = new Vector3(-2,0,5);
        var render = go.AddComponent<SpriteRenderer>();
        render.sprite = loader.asset as Sprite;
        loader.Dispose();
    }

    IEnumerator LoadAtlas()
    {
        string path = "UI/Atlas/Emoji/EmojiOne.png";
        var loader = AssetBundleManager.Instance.LoadAssetAsync(path, typeof(Sprite),true);
        yield return loader;

        for (int i = 0; i < loader.assets.Length; i++)
        {
            var go = new GameObject("sprite");
            go.transform.position = new Vector3(i-8,-4,10);
            var render = go.AddComponent<SpriteRenderer>();
            render.sprite = loader.assets[i] as Sprite;
        }

        loader.Dispose();
    }
    

}
