using UnityEngine;
using System;
using System.Collections.Generic;
using AssetBundles;
using LitJson;
using UnityEngine.EventSystems;

/// <summary>
/// added by passion @ 2017.12.25
/// 功能：通用静态方法
/// </summary>
//[LuaCallCSharp]
public static class GameUtility
{
    /// <summary>
    /// 获取当前平台
    /// </summary>
    /// <returns></returns>
    public static string GetPlatform()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                return "Android";
            case RuntimePlatform.IPhonePlayer:
                return "iOS";
            case RuntimePlatform.WebGLPlayer:
                return "WebGL";
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                return "Windows";
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                return "OSX";
            default:
                return "null";
        }
    }

    public static string GetLangStr()
    {
        var lang =  Application.systemLanguage;
        switch (lang)
        {
            case SystemLanguage.English:
                return "en";
            case SystemLanguage.ChineseSimplified:
                return "zh_cn";
            case SystemLanguage.ChineseTraditional:
                return "zh_tc";
            case SystemLanguage.Japanese:
                return "ja";
            case SystemLanguage.Korean:
                return "ko";
            case SystemLanguage.Russian:
                return "ru";
            case SystemLanguage.French:
                return "fr";
        }
        return "en";
    }
    
    /// <summary>
    /// 是否是移动端
    /// </summary>
    /// <returns></returns>
    public static bool IsMobile()
    {
        return Application.isMobilePlatform;
    }

    /// <summary>
    /// 是否是AssetBundle->Editor模式
    /// </summary>
    /// <returns></returns>
    public static bool IsEditorMode()
    {
#if UNITY_EDITOR
        if (AssetBundleConfig.IsEditorMode)
        {
            return true;
        }
#endif
        return false;
    }
    
    /// <summary>
    /// 获取当前网络
    /// </summary>
    /// <returns>0：无网，1：移动网，2：wifi</returns>
    public static int GetInternet()
    {
        return (int) Application.internetReachability;
    }

    public static SystemLanguage GetSystemLanguage()
    {
        return  Application.systemLanguage;
    }

    public static int ToInt(this Enum em)
    {
        return em.GetHashCode();
    }

    /// <summary>
    /// 手机永不休眠
    /// </summary>
    public static void NeverSleep()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    /// <summary>
    /// 判断手指是否点击UI，用于UI遮挡
    /// </summary>
    public static bool IsPointerOverGameObject()
    {
#if UNITY_EDITOR
        return EventSystem.current.IsPointerOverGameObject();
#elif UNITY_ANDROID || UNITY_IOS
        return Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
    }

    /// <summary>
    /// 获取手机点击的UI，用于判断指定UI遮挡
    /// </summary>
    public static bool GetPointerOverGameObject(out GameObject pressObject)
    {
        pressObject = null;

        if (IsPointerOverGameObject())
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(
#if UNITY_EDITOR
                Input.mousePosition.x, Input.mousePosition.y
#elif UNITY_ANDROID || UNITY_IOS
           Input.touchCount > 0 ? Input.GetTouch(0).position.x : 0, Input.touchCount > 0 ? Input.GetTouch(0).position.y : 0
#endif
            );

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            if (results.Count > 0)
            {
                pressObject = results[0].gameObject;
                return true;
            }
        }

        return false;
    }
    
    public static string TryGetString(this JsonData jsonData, string key)
    {
        if (jsonData.ContainsKey(key))
        {
            return jsonData[key].ToString().Trim();
        }

        Debug.LogWarning("JsonData con't find key:" + key);
        return "";
    }
    
    public static int TryGetInt(this JsonData jsonData, string key)
    {
        if (jsonData.ContainsKey(key))
        {
            return int.Parse(jsonData[key].ToString().Trim());
        }
        Debug.LogWarning("JsonData con't find key:" + key);
        return 0;
    }
    
}