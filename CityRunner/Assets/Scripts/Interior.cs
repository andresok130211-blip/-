using System.Collections.Generic;
using UnityEngine;

/// <summary>Включает свет внутри здания, только когда игрок внутри (экономия на телефоне).</summary>
public class InteriorZone : MonoBehaviour
{
    public Bounds bounds;
    public GameObject lightsRoot;
    void Update()
    {
        var p = PlayerController.Instance;
        if (p == null || lightsRoot == null) return;
        bool inside = bounds.Contains(p.transform.position + Vector3.up);
        if (lightsRoot.activeSelf != inside) lightsRoot.SetActive(inside);
    }
}

/// <summary>Прямоугольник комнаты в мировых координатах (x, z).</summary>
public struct RoomRect
{
    public float x0, z0, x1, z1;
    public RoomRect(float x0, float z0, float x1, float z1) { this.x0 = x0; this.z0 = z0; this.x1 = x1; this.z1 = z1; }
    public float cx { get { return (x0 + x1) * 0.5f; } }
    public float cz { get { return (z0 + z1) * 0.5f; } }
    public float w { get { return x1 - x0; } }
    public float d { get { return z1 - z0; } }
}

/// <summary>Полые здания: оболочка с дверным проёмом, перегородки с дверями, пол/потолок, свет.</summary>
public partial class CityGenerator
{
    const float FloorH = 3.6f;
    float fy { get { return Curb + 0.05f; } }
    float curCeil = FloorH;
    Color curWall = new Color(0.9f, 0.9f, 0.86f);
    Poi curPoi;
    readonly List<Vector2> curLights = new List<Vector2>();
    readonly Dictionary<string, Material> floorCache = new Dictionary<string, Material>();
    Material lampPanel;

    // ---------- материалы пола ----------
    Material FloorMat(string kind)
    {
        Material m;
        if (floorCache.TryGetValue(kind, out m)) return m;
        switch (kind)
        {
            case "wood":   m = ProceduralTextures.Surface(new Color(0.35f, 0.22f, 0.12f), new Color(0.5f, 0.34f, 0.2f), 6, 0.35f, 301, 8, 0.6f, 1.2f, 0.1f); break;
            case "lino":   m = ProceduralTextures.Surface(new Color(0.55f, 0.62f, 0.55f), new Color(0.68f, 0.74f, 0.66f), 6, 0.3f, 302, 0, 1f, 0.5f, 0.1f); break;
            case "carpet": m = ProceduralTextures.Surface(new Color(0.18f, 0.2f, 0.3f), new Color(0.26f, 0.28f, 0.4f), 20, 0.02f, 303, 0, 1f, 0.8f, 0.5f); break;
            case "concrete": m = ProceduralTextures.Surface(new Color(0.38f, 0.38f, 0.37f), new Color(0.5f, 0.5f, 0.48f), 8, 0.2f, 304, 4, 0.8f, 1.5f, 0.3f); break;
            default:       m = ProceduralTextures.Surface(new Color(0.72f, 0.72f, 0.7f), new Color(0.86f, 0.86f, 0.84f), 4, 0.55f, 305, 8, 0.7f, 1.0f, 0.05f); break; // плитка
        }
        floorCache[kind] = m; return m;
    }

    // ---------- оболочка здания с проёмом входа (на южной стороне) ----------
    void Shell(Material facade, Vector3 c, float w, float d, int floors, int interiorFloors, float doorX, float doorW, string floor, Color wall)
    {
        float x0 = c.x - w / 2, x1 = c.x + w / 2, z0 = c.z - d / 2, z1 = c.z + d / 2;
        float H = floors * FloorH, y0 = Curb, e = 0.02f, t = 14f, dh = 3.1f;
        curCeil = interiorFloors * FloorH; curWall = wall;
        var iw = Col(wall, 0.1f);
        float dx0 = doorX - doorW / 2, dx1 = doorX + doorW / 2;

        // наружные стены
        b.AddQuad(facade, new Vector3(x1, y0, z1), new Vector3(-w, 0, 0), new Vector3(0, H, 0), Vector2.zero, new Vector2(w / t, H / t));
        b.AddQuad(facade, new Vector3(x1, y0, z0), new Vector3(0, 0, d), new Vector3(0, H, 0), Vector2.zero, new Vector2(d / t, H / t));
        b.AddQuad(facade, new Vector3(x0, y0, z1), new Vector3(0, 0, -d), new Vector3(0, H, 0), Vector2.zero, new Vector2(d / t, H / t));
        b.AddQuad(facade, new Vector3(x0, y0, z0), new Vector3(dx0 - x0, 0, 0), new Vector3(0, H, 0), Vector2.zero, new Vector2((dx0 - x0) / t, H / t));
        b.AddQuad(facade, new Vector3(dx1, y0, z0), new Vector3(x1 - dx1, 0, 0), new Vector3(0, H, 0), new Vector2((dx1 - x0) / t, 0), new Vector2((x1 - dx1) / t, H / t));
        b.AddQuad(facade, new Vector3(dx0, y0 + dh, z0), new Vector3(dx1 - dx0, 0, 0), new Vector3(0, H - dh, 0), new Vector2((dx0 - x0) / t, dh / t), new Vector2((dx1 - dx0) / t, (H - dh) / t));

        // внутренние стены
        float ch = curCeil, one = 1f;
        // юг: два куска и перемычка над проёмом
        b.AddQuad(iw, new Vector3(dx0, y0, z0 + e), new Vector3(-(dx0 - x0), 0, 0), new Vector3(0, ch, 0), Vector2.zero, Vector2.one);
        b.AddQuad(iw, new Vector3(x1, y0, z0 + e), new Vector3(-(x1 - dx1), 0, 0), new Vector3(0, ch, 0), Vector2.zero, Vector2.one);
        if (ch > dh) b.AddQuad(iw, new Vector3(dx1, y0 + dh, z0 + e), new Vector3(-(dx1 - dx0), 0, 0), new Vector3(0, ch - dh, 0), Vector2.zero, Vector2.one);
        b.AddQuad(iw, new Vector3(x0, y0, z1 - e), new Vector3(w, 0, 0), new Vector3(0, ch, 0), Vector2.zero, new Vector2(one, one));  // север
        b.AddQuad(iw, new Vector3(x1 - e, y0, z1), new Vector3(0, 0, -d), new Vector3(0, ch, 0), Vector2.zero, new Vector2(one, one)); // восток
        b.AddQuad(iw, new Vector3(x0 + e, y0, z0), new Vector3(0, 0, d), new Vector3(0, ch, 0), Vector2.zero, new Vector2(one, one));  // запад

        // пол и потолок
        b.AddGround(FloorMat(floor), x0 + e, z0 + e, x1 - e, z1 - e, fy, 4f);
        b.AddQuad(Col(new Color(0.93f, 0.93f, 0.92f), 0.05f), new Vector3(x0 + e, y0 + ch, z0 + e), new Vector3(0, 0, d - 2 * e), new Vector3(w - 2 * e, 0, 0), Vector2.zero, Vector2.one);
        b.AddGround(FloorMat(floor), dx0, z0 - 2.2f, dx1, z0, fy, 4f); // порог

        // коллизии
        float th = 0.4f;
        AddBoxCollider(new Vector3(c.x, y0 + H / 2, z1 - th / 2 + 0.1f), new Vector3(w, H, th));
        AddBoxCollider(new Vector3(x0 + th / 2 - 0.1f, y0 + H / 2, c.z), new Vector3(th, H, d));
        AddBoxCollider(new Vector3(x1 - th / 2 + 0.1f, y0 + H / 2, c.z), new Vector3(th, H, d));
        AddBoxCollider(new Vector3((x0 + dx0) / 2, y0 + H / 2, z0 + th / 2 - 0.1f), new Vector3(dx0 - x0, H, th));
        AddBoxCollider(new Vector3((dx1 + x1) / 2, y0 + H / 2, z0 + th / 2 - 0.1f), new Vector3(x1 - dx1, H, th));
        AddBoxCollider(new Vector3(doorX, y0 + (2.7f + H) / 2, z0 + th / 2 - 0.1f), new Vector3(dx1 - dx0, H - 2.7f, th));
        AddBoxCollider(new Vector3(c.x, y0 + ch + 0.15f, c.z), new Vector3(w, 0.3f, d)); // потолок

        curLights.Clear();
        if (lampPanel == null) lampPanel = ProceduralTextures.Emissive(Color.white, new Color(1.6f, 1.55f, 1.4f));

        // зона и точка входа внутрь
        if (curPoi != null)
            curPoi.interior = new Bounds(new Vector3(c.x, y0 + ch / 2, c.z), new Vector3(w, ch + 0.5f, d));
    }

    /// <summary>Расставить светильники сеткой и создать зону с точечными лампами.</summary>
    void FinishInterior(float x0, float z0, float x1, float z1)
    {
        var root = new GameObject("InteriorLights");
        root.transform.SetParent(colRoot.parent, false);
        var holder = new GameObject("Lights");
        holder.transform.SetParent(root.transform, false);
        int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 9f)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 9f));
        for (int i = 0; i < nx; i++)
            for (int k = 0; k < nz; k++)
            {
                float lx = x0 + (i + 0.5f) * (x1 - x0) / nx, lz = z0 + (k + 0.5f) * (z1 - z0) / nz;
                b.AddBox(lampPanel, new Vector3(lx, Curb + curCeil - 0.03f, lz), new Vector3(1.6f, 0.06f, 0.8f), 0f, 1f);
                var lg = new GameObject("L");
                lg.transform.SetParent(holder.transform, false);
                lg.transform.position = new Vector3(lx, Curb + curCeil - 0.6f, lz);
                var L = lg.AddComponent<Light>();
                L.type = LightType.Point; L.range = 10f; L.intensity = 1.0f; L.color = new Color(1f, 0.96f, 0.88f); L.shadows = LightShadows.None;
            }
        var zone = root.AddComponent<InteriorZone>();
        zone.bounds = new Bounds(new Vector3((x0 + x1) / 2, Curb + curCeil / 2, (z0 + z1) / 2), new Vector3(x1 - x0, curCeil + 0.5f, z1 - z0));
        zone.lightsRoot = holder;
        holder.SetActive(false);
    }

    // ---------- перегородки ----------
    void Seg(bool alongX, float c, float a, float bb, float ya, float yb)
    {
        if (bb - a < 0.05f) return;
        var m = Col(curWall, 0.1f);
        float y0 = Curb + ya, h = yb - ya, th = 0.1f;
        if (alongX)
        {
            b.AddQuad(m, new Vector3(bb, y0, c + th), new Vector3(-(bb - a), 0, 0), new Vector3(0, h, 0), Vector2.zero, Vector2.one);
            b.AddQuad(m, new Vector3(a, y0, c - th), new Vector3(bb - a, 0, 0), new Vector3(0, h, 0), Vector2.zero, Vector2.one);
            b.AddQuad(m, new Vector3(bb, y0, c - th), new Vector3(0, 0, 2 * th), new Vector3(0, h, 0), Vector2.zero, Vector2.one);
            b.AddQuad(m, new Vector3(a, y0, c + th), new Vector3(0, 0, -2 * th), new Vector3(0, h, 0), Vector2.zero, Vector2.one);
            AddBoxCollider(new Vector3((a + bb) / 2, y0 + h / 2, c), new Vector3(bb - a, h, 0.2f));
        }
        else
        {
            b.AddQuad(m, new Vector3(c + th, y0, a), new Vector3(0, 0, bb - a), new Vector3(0, h, 0), Vector2.zero, Vector2.one);
            b.AddQuad(m, new Vector3(c - th, y0, bb), new Vector3(0, 0, -(bb - a)), new Vector3(0, h, 0), Vector2.zero, Vector2.one);
            b.AddQuad(m, new Vector3(c + th, y0, bb), new Vector3(-2 * th, 0, 0), new Vector3(0, h, 0), Vector2.zero, Vector2.one);
            b.AddQuad(m, new Vector3(c - th, y0, a), new Vector3(2 * th, 0, 0), new Vector3(0, h, 0), Vector2.zero, Vector2.one);
            AddBoxCollider(new Vector3(c, y0 + h / 2, (a + bb) / 2), new Vector3(0.2f, h, bb - a));
        }
    }

    /// <summary>Стена вдоль оси с дверными проёмами шириной 1.8 м (центры проёмов — doors).</summary>
    void Part(bool alongX, float c, float a, float bb, params float[] doors)
    {
        System.Array.Sort(doors);
        float pos = a;
        foreach (var dc in doors)
        {
            float s = dc - 0.9f, en = dc + 0.9f;
            Seg(alongX, c, pos, s, 0f, curCeil);
            Seg(alongX, c, s, en, 2.6f, curCeil);
            pos = en;
        }
        Seg(alongX, c, pos, bb, 0f, curCeil);
    }

    // ---------- мебель ----------
    static readonly Color Wood = new Color(0.45f, 0.3f, 0.18f), Dark = new Color(0.12f, 0.12f, 0.13f), White = new Color(0.92f, 0.92f, 0.9f),
        Grey = new Color(0.5f, 0.52f, 0.55f), Blue = new Color(0.15f, 0.3f, 0.6f), Red = new Color(0.7f, 0.12f, 0.1f), Green = new Color(0.15f, 0.45f, 0.2f),
        Yellow = new Color(0.9f, 0.75f, 0.2f), Fabric = new Color(0.35f, 0.25f, 0.4f), Cream = new Color(0.85f, 0.8f, 0.65f);

    /// <summary>Параллелепипед мебели: центр x,z; размеры sx,sz,sy; elev — высота низа над полом.</summary>
    void F(Color col, float x, float z, float sx, float sz, float sy, float elev = 0f, bool collide = true)
    {
        SolidBox(Col(col, 0.25f), new Vector3(x, fy + elev + sy / 2, z), new Vector3(sx, sy, sz), collide);
    }

    void Station(string name, float x, float z)
    {
        if (curPoi != null) curPoi.stations.Add(new KeyValuePair<string, Vector3>(name, new Vector3(x, Curb, z)));
    }

    void DeskSet(float x, float z, bool north = false, string station = null)
    {
        float s = north ? -1f : 1f;
        F(Wood, x, z, 1.4f, 0.7f, 0.75f);
        F(Dark, x, z + 0.1f * s, 0.5f, 0.05f, 0.35f, 0.75f, false);          // монитор
        F(Grey, x, z - 0.1f * s, 0.2f, 0.2f, 0.02f, 0.75f, false);
        F(Dark, x, z - 0.9f * s, 0.5f, 0.5f, 0.5f, 0f, false);                // кресло
        if (station != null) Station(station, x, z - 1.1f * s);
    }

    void Plant(float x, float z)
    {
        F(new Color(0.4f, 0.25f, 0.15f), x, z, 0.4f, 0.4f, 0.4f, 0f, false);
        F(Green, x, z, 0.55f, 0.55f, 0.9f, 0.4f, false);
    }

    void Shelf(float x, float z, float sx, float sz, float sy, Color col)
    {
        F(col, x, z, sx, sz, sy);
        for (int k = 0; k < 4; k++) F(new Color(0.7f - k * 0.1f, 0.3f + k * 0.12f, 0.25f), x, z, sx * 0.9f, sz * 1.02f, 0.25f, 0.3f + k * 0.45f, false);
    }

    void Bed(float x, float z, bool vertical, Color sheet)
    {
        float sx = vertical ? 1.0f : 2.0f, sz = vertical ? 2.0f : 1.0f;
        F(Grey, x, z, sx, sz, 0.45f);
        F(sheet, x, z, sx * 0.95f, sz * 0.95f, 0.15f, 0.45f, false);
        F(White, vertical ? x : x - 0.8f, vertical ? z + 0.8f : z, vertical ? 0.6f : 0.4f, vertical ? 0.4f : 0.6f, 0.12f, 0.6f, false);
    }

    void Sofa(float x, float z, float len, Color c, bool alongX = true)
    {
        float sx = alongX ? len : 0.95f, sz = alongX ? 0.95f : len;
        F(c, x, z, sx, sz, 0.45f);
        F(c * 0.85f, alongX ? x : x - 0.4f, alongX ? z + 0.4f : z, alongX ? len : 0.15f, alongX ? 0.15f : len, 0.55f, 0.45f, false);
    }

    void Table(float x, float z, float sx, float sz, Color c, float h = 0.75f)
    {
        F(c, x, z, sx, sz, 0.06f, h - 0.06f);
        F(Dark, x - sx / 2 + 0.1f, z - sz / 2 + 0.1f, 0.06f, 0.06f, h - 0.06f, 0f, false);
        F(Dark, x + sx / 2 - 0.1f, z + sz / 2 - 0.1f, 0.06f, 0.06f, h - 0.06f, 0f, false);
        F(Dark, x - sx / 2 + 0.1f, z + sz / 2 - 0.1f, 0.06f, 0.06f, h - 0.06f, 0f, false);
        F(Dark, x + sx / 2 - 0.1f, z - sz / 2 + 0.1f, 0.06f, 0.06f, h - 0.06f, 0f, false);
    }

    void Chair(float x, float z, Color c) { F(c, x, z, 0.42f, 0.42f, 0.45f, 0f, false); F(c, x, z, 0.42f, 0.06f, 0.45f, 0.45f, false); }
}
