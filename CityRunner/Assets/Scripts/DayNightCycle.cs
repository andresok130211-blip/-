using UnityEngine;

/// <summary>Смена дня и ночи: солнце, луна, небо, туман, окружающий свет, подсветка окон и фонарей.</summary>
public class DayNightCycle : MonoBehaviour
{
    public float hour = 17.5f;              // текущее время суток
    public float dayLengthMinutes = 12f;    // реальных минут на сутки
    public bool paused;

    float giTimer;
    Light sun, moon;
    Material sky;
    CityMaterials mats;

    public static DayNightCycle Create(CityMaterials materials)
    {
        var go = new GameObject("DayNight");
        var dn = go.AddComponent<DayNightCycle>();
        dn.mats = materials;

        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(go.transform, false);
        dn.sun = sunGo.AddComponent<Light>();
        dn.sun.type = LightType.Directional;
        dn.sun.shadows = LightShadows.Soft;
        dn.sun.shadowStrength = 0.95f;
        dn.sun.shadowBias = 0.05f;
        dn.sun.shadowNormalBias = 0.4f;

        var moonGo = new GameObject("Moon");
        moonGo.transform.SetParent(go.transform, false);
        dn.moon = moonGo.AddComponent<Light>();
        dn.moon.type = LightType.Directional;
        dn.moon.color = new Color(0.55f, 0.65f, 0.95f);
        dn.moon.shadows = LightShadows.Soft;
        dn.moon.shadowStrength = 0.6f;

        var shader = Shader.Find("Skybox/Procedural");
        if (shader != null)
        {
            dn.sky = new Material(shader);
            RenderSettings.skybox = dn.sky;
        }
        RenderSettings.sun = dn.sun;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        QualitySettings.shadowDistance = 220f;
        QualitySettings.shadowCascades = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.antiAliasing = 4;
        return dn;
    }

    void Update()
    {
        if (GameInput.TimePausePressed) paused = !paused;
        if (GameInput.TimeJumpPressed) hour = (hour + 3f) % 24f;
        if (!paused) hour = (hour + Time.deltaTime * 24f / (dayLengthMinutes * 60f)) % 24f;
        Apply();
    }

    static Color Smooth(Color a, Color b, float t) { t = Mathf.Clamp01(t); t = t * t * (3f - 2f * t); return Color.Lerp(a, b, t); }

    void Apply()
    {
        // высота солнца: +1 в полдень, -1 в полночь
        float ang = (hour - 6f) / 24f * Mathf.PI * 2f;
        float elev = Mathf.Sin(ang);
        // направление: Euler X = высота над горизонтом, Y — азимут
        float sunPitch = Mathf.Asin(Mathf.Clamp(elev, -1f, 1f)) * Mathf.Rad2Deg;
        float sunAz = 90f + Mathf.Cos(ang) * -75f;
        sun.transform.rotation = Quaternion.Euler(sunPitch, sunAz, 0f);
        moon.transform.rotation = Quaternion.Euler(-sunPitch, sunAz + 180f, 0f);

        float day = Mathf.Clamp01((elev + 0.1f) / 0.35f);         // 0 ночью, 1 днём
        float golden = Mathf.Clamp01(1f - Mathf.Abs(elev) / 0.35f); // близко к горизонту
        float night = 1f - Mathf.Clamp01((elev + 0.18f) / 0.28f);

        Color sunWhite = new Color(1f, 0.96f, 0.88f);
        Color sunOrange = new Color(1f, 0.55f, 0.25f);
        sun.color = Color.Lerp(sunWhite, sunOrange, golden * 0.9f);
        sun.intensity = Mathf.Lerp(0f, 1.25f, Mathf.Clamp01((elev + 0.05f) / 0.25f));
        sun.enabled = sun.intensity > 0.01f;
        moon.intensity = Mathf.Lerp(0f, 0.32f, night);
        moon.enabled = moon.intensity > 0.01f;

        // небо
        Color skyDay = new Color(0.36f, 0.55f, 0.85f), skyDusk = new Color(0.55f, 0.45f, 0.55f), skyNight = new Color(0.02f, 0.03f, 0.08f);
        Color ground = Smooth(skyNight, new Color(0.45f, 0.45f, 0.42f), day);
        Color horizon = Smooth(Color.Lerp(skyDusk, new Color(0.9f, 0.5f, 0.3f), golden), new Color(0.65f, 0.75f, 0.88f), Mathf.Clamp01(elev * 2f));
        horizon = Smooth(new Color(0.02f, 0.03f, 0.06f), horizon, day);
        Color zenith = Smooth(skyNight, Color.Lerp(skyDay, skyDusk, golden * 0.5f), day);

        RenderSettings.ambientSkyColor = Color.Lerp(skyNight * 1.2f, zenith * 0.85f, day);
        RenderSettings.ambientEquatorColor = Color.Lerp(skyNight * 1.5f + new Color(0.02f, 0.02f, 0.04f), horizon * 0.8f, day);
        RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.01f, 0.01f, 0.015f), ground * 0.5f, day);

        if (sky != null)
        {
            sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.6f, 1.15f, day));
            sky.SetFloat("_Exposure", Mathf.Lerp(0.25f, 1.3f, day));
            sky.SetColor("_SkyTint", Color.Lerp(new Color(0.3f, 0.3f, 0.4f), new Color(0.5f, 0.5f, 0.5f), day));
            sky.SetColor("_GroundColor", ground);
            sky.SetFloat("_SunSize", 0.04f);
        }

        // туман в цвет горизонта
        Color fog = Color.Lerp(new Color(0.02f, 0.03f, 0.06f), horizon, day);
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = Mathf.Lerp(0.0016f, 0.0011f, day);
        if (Camera.main != null) Camera.main.backgroundColor = fog;

        // окна и фонари горят ночью
        float lights = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.15f - elev) / 0.3f));
        if (mats != null)
            foreach (var e in mats.emissives)
                e.material.SetColor("_EmissionColor", e.baseEmission * e.nightStrength * lights);

        giTimer -= Time.deltaTime;
        if (giTimer <= 0f) { giTimer = 1f; DynamicGI.UpdateEnvironment(); }
    }

    public string TimeString()
    {
        int h = Mathf.FloorToInt(hour), m = Mathf.FloorToInt((hour - h) * 60f);
        return h.ToString("00") + ":" + m.ToString("00");
    }
}
