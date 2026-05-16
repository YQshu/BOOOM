using UnityEngine;

/// <summary>
/// 设置数据管理器（静态工具类）。
/// 统一管理 PlayerPrefs 键名与读写：音量、显示模式、分辨率。
/// </summary>
public static class SettingsManager
{
    // ── PlayerPrefs 键名 ──────────────────────────────────────

    private const string KeySfxVolume = "SfxVolume";
    private const string KeyBgmVolume = "BgmVolume";
    private const string KeyDisplayMode = "DisplayMode";
    private const string KeyResolutionWidth = "ResolutionWidth";
    private const string KeyResolutionHeight = "ResolutionHeight";

    // ── 默认值 ────────────────────────────────────────────────

    private const float DefaultVolume = 1f;
    private const int DefaultDisplayMode = 1; // 0=全屏(无边框) 1=窗口
    private const int DefaultWidth = 1920;
    private const int DefaultHeight = 1080;

    // ── 音量 ──────────────────────────────────────────────────

    public static float GetSfxVolume() => PlayerPrefs.GetFloat(KeySfxVolume, DefaultVolume);

    public static void SetSfxVolume(float v)
    {
        PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(v));
        PlayerPrefs.Save();
    }

    public static float GetBgmVolume() => PlayerPrefs.GetFloat(KeyBgmVolume, DefaultVolume);

    public static void SetBgmVolume(float v)
    {
        PlayerPrefs.SetFloat(KeyBgmVolume, Mathf.Clamp01(v));
        PlayerPrefs.Save();
    }

    // ── 显示模式 ──────────────────────────────────────────────

    /// <summary>
    /// 获取显示模式（0=全屏(无边框)，1=窗口）。
    /// </summary>
    public static int GetDisplayMode() => PlayerPrefs.GetInt(KeyDisplayMode, DefaultDisplayMode);

    /// <summary>
    /// 设置显示模式（0=全屏(无边框)，1=窗口）。
    /// </summary>
    public static void SetDisplayMode(int mode)
    {
        PlayerPrefs.SetInt(KeyDisplayMode, Mathf.Clamp(mode, 0, 1));
        PlayerPrefs.Save();
    }

    // ── 分辨率 ────────────────────────────────────────────────

    public static int GetResolutionWidth() => PlayerPrefs.GetInt(KeyResolutionWidth, DefaultWidth);
    public static int GetResolutionHeight() => PlayerPrefs.GetInt(KeyResolutionHeight, DefaultHeight);

    public static void SetResolution(int width, int height)
    {
        PlayerPrefs.SetInt(KeyResolutionWidth, width);
        PlayerPrefs.SetInt(KeyResolutionHeight, height);
        PlayerPrefs.Save();
    }

    // ── 应用设置 ──────────────────────────────────────────────

    /// <summary>
    /// 根据当前 PlayerPrefs 中的设置，应用显示模式与分辨率。
    /// 在 Editor 下跳过，避免 Screen.SetResolution 导致编辑器卡死。
    /// </summary>
    public static void Apply()
    {
#if UNITY_EDITOR
        return;
#else
            int mode = GetDisplayMode();

            if (mode == 0)
            {
                // 全屏（无边框）：使用显示器原生分辨率，避免拉伸模糊
                var native = Screen.mainWindowDisplayInfo;
                Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                // 窗口模式：使用用户选择的分辨率
                int w = GetResolutionWidth();
                int h = GetResolutionHeight();
                Screen.SetResolution(w, h, FullScreenMode.Windowed);
            }
#endif
    }

    /// <summary>
    /// 游戏启动时调用：读取所有设置并立即 Apply。
    /// </summary>
    public static void Load()
    {
        Apply();
    }
}

