
using UnityEngine;
using UnityEngine.UI;

public class UILauncher : Singleton<UILauncher>
{
    private GameObject go;
    private Text statusText;
    private Text progressText;
    private Slider slider;

    public GameObject UIGameObject
    {
        get
        {
            return go;
        }
        set
        {
            if (value != go)
            {
                go = value;
                InitGo(go);
            }
        }
    }

    public void InitGo(GameObject go)
    {
        statusText = go.transform.Find("ContentRoot/LoadingDesc").GetComponent<Text>();
        progressText = go.transform.Find("ContentRoot/SliderBar/LoadingTxt").GetComponent<Text>();
        slider = go.transform.Find("ContentRoot/SliderBar").GetComponent<Slider>();
        slider.gameObject.SetActive(false);

        var aspect = (float)Screen.width / Screen.height;
//        if (aspect >= 1.77f)
//        {
//            var bg = go.transform.Find("BgRoot/BG").transform;
//            bg.localPosition = new Vector3(0,660,0);
//        }
    }
    
    public void SetSatus(string status)
    {
        statusText.text = status;
    }

    public void SetValue(float progress, bool active = true)
    {
        slider.gameObject.SetActive(active);
        slider.normalizedValue = progress;
    }

    public void SetProgressText(string text)
    {
        progressText.text = text;
    }
    
    public void HidLauncher()
    {
        SetProgressText("");
        SetValue(0, false);
        go.SetActive(false);
    }

    public override void Dispose()
    {
        if (go != null)
        {
            Object.Destroy(go);
        }
    }
}
