using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkHttp : MonoSingleton<NetworkHttp>
{
    private IEnumerator coSendRequest(UnityWebRequest request, Action<string> callBack)
    {
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError || !string.IsNullOrEmpty(request.error))
        {
            Logger.LogError("UnityWebRequest.error: " + request.error + " ; url: " + request.url);
            callBack?.Invoke(null);
        }
        else
        {
            string text = request.downloadHandler?.text;
            Logger.Log("UnityWebRequest.text：" + text);
            callBack?.Invoke(text);
        }

        request.Dispose();
    }

    public UnityWebRequest Post(string url, Dictionary<string, string> header, byte[] args, Action<string> callBack,
        int timeOut = 0)
    {
        UnityWebRequest request = UnityWebRequest.Post(url, "POST");
        if (args != null)
        {
            request.uploadHandler = new UploadHandlerRaw(args);
        }

        request.timeout = timeOut > 0 ? timeOut : 10;

        if (header != null)
        {
            foreach (var keyValuePair in header)
            {
                request.SetRequestHeader(keyValuePair.Key, keyValuePair.Value);
            }
        }

        StartCoroutine(coSendRequest(request, callBack));
        return request;
    }

    public UnityWebRequest Get(string url, Dictionary<string, string> header, Action<string> callBack, int timeOut = 0)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = timeOut > 0 ? timeOut : 10;

        if (header != null)
        {
            foreach (var keyValuePair in header)
            {
                request.SetRequestHeader(keyValuePair.Key, keyValuePair.Value);
            }
        }

        StartCoroutine(coSendRequest(request, callBack));
        return request;
    }

    
    public static void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
}
