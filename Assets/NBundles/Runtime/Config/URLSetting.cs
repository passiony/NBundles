
public class URLSetting
{
    public static string BASE_URL
    {
        get { return "http://127.0.0.1"; }
    }

    public static string START_UP_URL
    {
        get { return BASE_URL + "/startup"; }
    }

    public static string APP_DOWNLOAD_URL { get; set; }

    public static string RES_DOWNLOAD_URL { get; set; }
}
