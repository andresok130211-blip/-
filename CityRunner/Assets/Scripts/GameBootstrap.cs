using System.Collections;
using UnityEngine;

/// <summary>
/// Точка входа. Создаётся автоматически при запуске любой сцены — сцену настраивать не нужно.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    public const int Seed = 20261124;

    CityGenerator city;
    PlayerController player;
    DayNightCycle dayNight;
    Camera cam, miniCam;
    RenderTexture miniRT;
    float miniTimer;
    bool ready;
    bool showHelp = true;
    float fps;
    int poiIndex;
    GUIStyle style, shadow, big;
    Texture2D dot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindObjectOfType<GameBootstrap>() != null) return;
        new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
    }

    IEnumerator Start()
    {
        bool mobile = Application.isMobilePlatform;
        CityGenerator.Mobile = mobile;
        ProceduralTextures.Size = mobile ? 256 : 512;
        Application.targetFrameRate = mobile ? 60 : 120;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (mobile) Screen.orientation = ScreenOrientation.LandscapeLeft;

        // убрать камеры/свет сцены по умолчанию
        foreach (var c in FindObjectsOfType<Camera>()) Destroy(c.gameObject);
        foreach (var l in FindObjectsOfType<Light>()) Destroy(l.gameObject);

        yield return null; // кадр с экраном загрузки

        city = new CityGenerator();
        city.Generate(Seed, transform);

        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        cam = camGo.AddComponent<Camera>();
        cam.allowHDR = !mobile;
        cam.nearClipPlane = 0.15f;
        cam.farClipPlane = mobile ? 650f : 1800f;
        cam.fieldOfView = 65f;
        cam.clearFlags = CameraClearFlags.Skybox;
        camGo.AddComponent<AudioListener>();

        player = PlayerController.Create(city.SpawnPoint);
        player.cameraTransform = camGo.transform;
        var tp = camGo.AddComponent<ThirdPersonCamera>();
        tp.target = player.transform;

        dayNight = DayNightCycle.Create(city.Materials);
        if (mobile)
        {
            QualitySettings.shadowDistance = 90f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            RenderSettings.fogDensity = 0.004f;
        }
        TouchControls.Create();

        // миникарта: вид сверху, перерисовывается раз в 0.3 c
        miniRT = new RenderTexture(256, 256, 16);
        var mc = new GameObject("MiniMapCamera");
        mc.transform.SetParent(transform, false);
        miniCam = mc.AddComponent<Camera>();
        miniCam.orthographic = true;
        miniCam.orthographicSize = 170f;
        miniCam.targetTexture = miniRT;
        miniCam.clearFlags = CameraClearFlags.SolidColor;
        miniCam.backgroundColor = new Color(0.1f, 0.12f, 0.1f);
        miniCam.farClipPlane = 800f;
        miniCam.enabled = false;
        dot = Texture2D.whiteTexture;
        ready = true;
    }

    void Update()
    {
        fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);
        if (!ready) return;
        if (GameInput.HelpPressed) showHelp = !showHelp;
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.N)) NextPoi();
#endif

        miniTimer -= Time.unscaledDeltaTime;
        if (miniTimer <= 0f && miniCam != null && player != null)
        {
            miniTimer = 0.3f;
            miniCam.transform.position = player.transform.position + Vector3.up * 400f;
            miniCam.transform.rotation = Quaternion.Euler(90f, cam.transform.eulerAngles.y, 0f);
            miniCam.Render();
        }
    }

    void NextPoi() { if (PoiRegistry.All.Count > 0) poiIndex = (poiIndex + 1) % PoiRegistry.All.Count; }

    void EnsureStyles()
    {
        if (style != null) return;
        int fs = Mathf.Max(14, Screen.height / 38);
        style = new GUIStyle(GUI.skin.label) { fontSize = fs, richText = true };
        style.normal.textColor = Color.white;
        shadow = new GUIStyle(style); shadow.normal.textColor = new Color(0, 0, 0, 0.85f);
        big = new GUIStyle(GUI.skin.label) { fontSize = fs * 2, alignment = TextAnchor.MiddleCenter };
        big.normal.textColor = Color.white;
    }

    void Text(Rect r, string t)
    {
        GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), t, shadow);
        GUI.Label(r, t, style);
    }

    void OnGUI()
    {
        EnsureStyles();
        if (!ready)
        {
            GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Строю город…", big);
            return;
        }

        float pad = Screen.height * 0.02f;
        string t = "Время " + dayNight.TimeString() + "   FPS " + Mathf.RoundToInt(fps);
        if (showHelp && !Application.isMobilePlatform)
            t += "\nWASD — ходьба  Shift — бег  Пробел — прыжок  Мышь — камера\nT — +3 часа  Y — пауза  Tab — следующее место  F1 — скрыть";
        Text(new Rect(pad, pad, Screen.width * 0.7f, Screen.height * 0.3f), t);

        // миникарта
        float ms = Screen.height * 0.3f;
        var mr = new Rect(Screen.width - ms - pad, pad, ms, ms);
        GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(new Rect(mr.x - 3, mr.y - 3, ms + 6, ms + 6), dot);
        GUI.color = Color.white; GUI.DrawTexture(mr, miniRT);
        float d = ms * 0.045f;
        GUI.color = Color.red; GUI.DrawTexture(new Rect(mr.center.x - d / 2, mr.center.y - d / 2, d, d), dot);
        float scale = ms / (miniCam.orthographicSize * 2f);
        foreach (var p in PoiRegistry.All)
        {
            Vector3 rel = Quaternion.Euler(0, -cam.transform.eulerAngles.y, 0) * (p.entrance - player.transform.position);
            Vector2 pos = new Vector2(mr.center.x + rel.x * scale, mr.center.y - rel.z * scale);
            if (!mr.Contains(pos)) continue;
            GUI.color = p.color; GUI.DrawTexture(new Rect(pos.x - d / 2, pos.y - d / 2, d, d), dot);
        }
        GUI.color = Color.white;

        // цель
        if (PoiRegistry.All.Count > 0)
        {
            var target = PoiRegistry.All[poiIndex];
            float dist = Vector3.Distance(player.transform.position, target.entrance);
            Vector3 dirW = target.entrance - player.transform.position; dirW.y = 0;
            float rel = Vector3.SignedAngle(Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up), dirW, Vector3.up);
            string arrow = Mathf.Abs(rel) < 25f ? "↑" : (rel > 0 ? (rel > 135f ? "↓" : "→") : (rel < -135f ? "↓" : "←"));
            float by = Screen.height - Screen.height * 0.09f - pad;
            var br = new Rect(Screen.width * 0.5f - Screen.width * 0.2f, by, Screen.width * 0.4f, Screen.height * 0.09f);
            GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(br, dot); GUI.color = Color.white;
            var c = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(br, arrow + "  " + target.name + "  " + Mathf.RoundToInt(dist) + " м", c);
            if (GUI.Button(new Rect(br.xMax + pad, br.y, br.height * 1.6f, br.height), "Следующее", style)) NextPoi();
        }
    }
}
