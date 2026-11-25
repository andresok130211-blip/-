using UnityEngine;

/// <summary>
/// Процедурные PBR-текстуры (альбедо, нормали, металлик/гладкость, эмиссия).
/// Всё генерируется кодом, внешние ассеты не нужны. Все текстуры бесшовные.
/// </summary>
public static class ProceduralTextures
{
    public static int Size = 512;   // на телефоне ставится 256
    public const int Cells = 4; // фасадная текстура = 4x4 оконных ячейки

    public struct FacadeStyle
    {
        public Color wall, glassTop, glassBottom;
        public float winX0, winX1, winY0, winY1; // прямоугольник окна внутри ячейки (0..1)
        public float wallSmooth, glassSmooth;
        public float litChance;                  // доля окон, горящих ночью
        public bool brick;
    }

    // ---------- бесшовный шум ----------
    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 144665;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    static float Tileable(float fx, float fy, int period, int seed)
    {
        float x = fx * period, y = fy * period;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float tx = x - x0, ty = y - y0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        int xa = x0 % period, ya = y0 % period, xb = (x0 + 1) % period, yb = (y0 + 1) % period;
        float a = Hash(xa, ya, seed), b = Hash(xb, ya, seed), c = Hash(xa, yb, seed), d = Hash(xb, yb, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    static float TFbm(float fx, float fy, int basePeriod, int seed)
    {
        float sum = 0f, amp = 0.5f, tot = 0f;
        int p = basePeriod;
        for (int o = 0; o < 4; o++)
        {
            sum += Tileable(fx, fy, p, seed + o * 17) * amp;
            tot += amp;
            amp *= 0.5f;
            p *= 2;
        }
        return sum / tot;
    }

    static Texture2D NewTex(Color32[] px, int size, bool linear)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, true, linear);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Trilinear;
        t.anisoLevel = 8;
        t.SetPixels32(px);
        t.Apply(true, true);
        return t;
    }

    static Material NewStandard()
    {
        return new Material(Shader.Find("Standard"));
    }

    // ---------- фасады ----------
    public static Material Facade(FacadeStyle s, int seed)
    {
        int size = Size;
        int cell = size / Cells;
        var rng = new System.Random(seed);

        var lit = new bool[Cells, Cells];
        var litCol = new Color[Cells, Cells];
        Color warm = new Color(1f, 0.74f, 0.4f), white = new Color(1f, 0.93f, 0.82f), cool = new Color(0.6f, 0.78f, 1f);
        for (int cx = 0; cx < Cells; cx++)
            for (int cy = 0; cy < Cells; cy++)
            {
                lit[cx, cy] = rng.NextDouble() < s.litChance;
                double r = rng.NextDouble();
                Color baseCol = r < 0.6 ? warm : (r < 0.85 ? white : cool);
                litCol[cx, cy] = baseCol * (0.5f + 0.5f * (float)rng.NextDouble());
            }

        var alb = new Color32[size * size];
        var emi = new Color32[size * size];
        var mg = new Color32[size * size];
        Color mortar = new Color(0.6f, 0.58f, 0.55f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int cx = x / cell, cy = y / cell;
                float lx = (x % cell) / (float)cell, ly = (y % cell) / (float)cell;
                float n = TFbm(x / (float)size, y / (float)size, 8, seed);
                Color c;
                Color e = Color.black;
                float smooth;
                const float f = 0.02f;
                bool win = lx >= s.winX0 && lx <= s.winX1 && ly >= s.winY0 && ly <= s.winY1;
                bool glass = lx >= s.winX0 + f && lx <= s.winX1 - f && ly >= s.winY0 + f && ly <= s.winY1 - f;
                if (glass)
                {
                    float t = Mathf.InverseLerp(s.winY0, s.winY1, ly);
                    c = Color.Lerp(s.glassBottom, s.glassTop, t) * (0.85f + 0.3f * n);
                    smooth = s.glassSmooth;
                    if (lit[cx, cy]) { e = litCol[cx, cy]; c *= 0.55f; }
                }
                else if (win)
                {
                    c = s.wall * 0.4f;
                    smooth = 0.3f;
                }
                else
                {
                    c = s.wall * (0.76f + 0.45f * n);
                    smooth = s.wallSmooth;
                    if (s.brick)
                    {
                        int row = y / 3;
                        if (y % 3 == 0 || (x + (row & 1) * 5) % 9 == 0) c = Color.Lerp(c, mortar, 0.7f);
                    }
                    if (ly < 0.05f) c *= 0.78f; // перекрытие между этажами
                }
                c.a = 1f;
                int i = y * size + x;
                alb[i] = c;
                emi[i] = e;
                mg[i] = new Color(glass ? 0.12f : 0f, 0f, 0f, smooth);
            }
        }

        var m = NewStandard();
        m.SetTexture("_MainTex", NewTex(alb, size, false));
        m.SetColor("_Color", Color.white);
        m.SetTexture("_MetallicGlossMap", NewTex(mg, size, true));
        m.EnableKeyword("_METALLICGLOSSMAP");
        m.SetFloat("_GlossMapScale", 1f);
        m.SetTexture("_EmissionMap", NewTex(emi, size, false));
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", Color.black);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return m;
    }

    // ---------- универсальная поверхность (асфальт, бетон, трава...) ----------
    public static Material Surface(Color a, Color b, int period, float smooth, int seed,
                                   int joints = 0, float jointDark = 0.7f, float bump = 1.5f, float speckle = 0f)
    {
        int size = Size;
        var alb = new Color32[size * size];
        var hgt = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = TFbm(x / (float)size, y / (float)size, period, seed);
                Color c = Color.Lerp(a, b, n);
                float h = n;
                if (speckle > 0f)
                {
                    float sp = (Hash(x, y, seed + 99) - 0.5f) * speckle;
                    c = c * (1f + sp);
                    h += sp * 0.6f;
                }
                if (joints > 0)
                {
                    int jp = size / joints;
                    if (x % jp < 3 || y % jp < 3) { c *= jointDark; h -= 0.35f; }
                }
                c.a = 1f;
                alb[y * size + x] = c;
                hgt[y * size + x] = h;
            }
        }

        var nrm = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float hl = hgt[y * size + (x - 1 + size) % size];
                float hr = hgt[y * size + (x + 1) % size];
                float hd = hgt[((y - 1 + size) % size) * size + x];
                float hu = hgt[((y + 1) % size) * size + x];
                Vector3 nv = new Vector3((hl - hr) * bump, (hd - hu) * bump, 1f).normalized;
                byte rx = (byte)((nv.x * 0.5f + 0.5f) * 255f);
                byte gy = (byte)((nv.y * 0.5f + 0.5f) * 255f);
                byte bz = (byte)((nv.z * 0.5f + 0.5f) * 255f);
                // упаковка DXT5nm (десктоп): x в альфа-канале, y в зелёном
                nrm[y * size + x] = new Color32(255, gy, bz, rx);
            }
        }

        var m = NewStandard();
        m.SetTexture("_MainTex", NewTex(alb, size, false));
        m.SetTexture("_BumpMap", NewTex(nrm, size, true));
        m.EnableKeyword("_NORMALMAP");
        m.SetFloat("_BumpScale", 1f);
        m.SetFloat("_Glossiness", smooth);
        m.SetFloat("_Metallic", 0f);
        return m;
    }

    // ---------- однотонные материалы ----------
    public static Material Solid(Color c, float smooth, float metallic)
    {
        var m = NewStandard();
        m.SetColor("_Color", c);
        m.SetFloat("_Glossiness", smooth);
        m.SetFloat("_Metallic", metallic);
        return m;
    }

    public static Material Emissive(Color c, Color emission)
    {
        var m = Solid(c, 0.4f, 0f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return m;
    }
}
