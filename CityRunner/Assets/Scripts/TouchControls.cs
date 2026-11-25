using UnityEngine;

#if ENABLE_LEGACY_INPUT_MANAGER

/// <summary>
/// Экранное управление для телефона: плавающий джойстик слева, свайп справа — камера,
/// кнопки «прыжок» и «бег». Работает через legacy Input.touches (по умолчанию в Unity 2022.3).
/// </summary>
public class TouchControls : MonoBehaviour
{
    public static TouchControls Instance;
    public static bool Active => Instance != null && Instance.enabled && Instance.show;

    public Vector2 move;
    public Vector2 look;      // дельта за кадр в «пикселях»
    public bool jump;         // нажатие в этом кадре
    public bool sprint;       // переключатель бега

    bool show;
    int moveFinger = -1, lookFinger = -1;
    Vector2 moveOrigin, moveCurrent;
    Texture2D circle;
    Rect jumpRect, runRect;

    public static void Create()
    {
        if (Instance != null) return;
        var go = new GameObject("TouchControls");
        Instance = go.AddComponent<TouchControls>();
    }

    void Awake()
    {
        show = Application.isMobilePlatform || Input.touchSupported;
        circle = MakeCircle(64);
    }

    static Texture2D MakeCircle(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color32[s * s];
        float r = s * 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                byte a = (byte)(255f * Mathf.Clamp01((1f - d) * r * 0.5f));
                px[y * s + x] = new Color32(255, 255, 255, a);
            }
        t.SetPixels32(px); t.Apply();
        return t;
    }

    void Layout()
    {
        float u = Mathf.Min(Screen.width, Screen.height) * 0.16f;
        jumpRect = new Rect(Screen.width - u * 1.6f, Screen.height - u * 1.9f, u, u);              // экранные координаты, y сверху
        runRect = new Rect(Screen.width - u * 2.9f, Screen.height - u * 1.2f, u * 0.85f, u * 0.85f);
    }

    void Update()
    {
        jump = false;
        look = Vector2.zero;
        if (!show) return;
        Layout();
        float W = Screen.width;

        foreach (var t in Input.touches)
        {
            Vector2 p = new Vector2(t.position.x, Screen.height - t.position.y); // y сверху
            if (t.phase == TouchPhase.Began)
            {
                if (jumpRect.Contains(p)) { jump = true; continue; }
                if (runRect.Contains(p)) { sprint = !sprint; continue; }
                if (p.x < W * 0.42f && moveFinger < 0) { moveFinger = t.fingerId; moveOrigin = moveCurrent = p; }
                else if (p.x >= W * 0.42f && lookFinger < 0) lookFinger = t.fingerId;
            }
            else if (t.fingerId == moveFinger)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { moveFinger = -1; move = Vector2.zero; }
                else
                {
                    moveCurrent = p;
                    Vector2 d = (moveCurrent - moveOrigin) / (Screen.dpi > 0 ? Screen.dpi * 0.35f : 120f);
                    move = Vector2.ClampMagnitude(new Vector2(d.x, -d.y), 1f);
                    if (move.magnitude < 0.12f) move = Vector2.zero;
                }
            }
            else if (t.fingerId == lookFinger)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) lookFinger = -1;
                else look += new Vector2(t.deltaPosition.x, t.deltaPosition.y) * 0.06f;
            }
        }
    }

    void OnGUI()
    {
        if (!show) return;
        var old = GUI.color;
        if (moveFinger >= 0)
        {
            float r = Screen.height * 0.12f;
            GUI.color = new Color(1, 1, 1, 0.18f);
            GUI.DrawTexture(new Rect(moveOrigin.x - r, moveOrigin.y - r, r * 2, r * 2), circle);
            Vector2 knob = moveOrigin + Vector2.ClampMagnitude(moveCurrent - moveOrigin, r);
            GUI.color = new Color(1, 1, 1, 0.45f);
            GUI.DrawTexture(new Rect(knob.x - r * 0.45f, knob.y - r * 0.45f, r * 0.9f, r * 0.9f), circle);
        }
        GUI.color = new Color(1, 1, 1, 0.3f);
        GUI.DrawTexture(jumpRect, circle);
        GUI.color = sprint ? new Color(1f, 0.8f, 0.2f, 0.6f) : new Color(1, 1, 1, 0.3f);
        GUI.DrawTexture(runRect, circle);
        GUI.color = Color.white;
        var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(jumpRect.width * 0.2f) };
        st.normal.textColor = Color.white;
        GUI.Label(jumpRect, "ПРЫЖОК", st);
        GUI.Label(runRect, "БЕГ", st);
        GUI.color = old;
    }
}
#else
public class TouchControls : MonoBehaviour
{
    public static bool Active => false;
    public static TouchControls Instance;
    public Vector2 move, look;
    public bool jump, sprint;
    public static void Create() { }
}
#endif
