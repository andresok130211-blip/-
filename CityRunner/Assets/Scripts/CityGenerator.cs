using System.Collections.Generic;
using UnityEngine;

/// <summary>Процедурная генерация большого города: улицы, кварталы, небоскрёбы, парки, площадь, пропсы.</summary>
public partial class CityGenerator
{
    public const int N = 20;            // кварталов по стороне (~1.9 км)
    public const float B = 80f;         // размер квартала
    public const float R = 18f;         // ширина улицы
    public const float Pitch = B + R;
    public const float Curb = 0.15f;
    public const float Walk = 4f;       // ширина тротуара

    public float WorldSize => N * Pitch;
    public Vector3 SpawnPoint { get; private set; }
    public CityMaterials Materials { get; private set; }

    public static bool Mobile;
    CityBatcher b;
    CityProps props;
    Transform colRoot;
    System.Random rng;
    readonly Vector3 center = new Vector3(N * Pitch * 0.5f - R * 0.5f, 0, N * Pitch * 0.5f - R * 0.5f);

    public GameObject Generate(int seed, Transform parent)
    {
        rng = new System.Random(seed);
        Materials = new CityMaterials(seed);
        b = new CityBatcher(160f);
        var root = new GameObject("City");
        root.transform.SetParent(parent, false);
        colRoot = new GameObject("Colliders").transform;
        colRoot.SetParent(root.transform, false);
        props = new CityProps(b, Materials, colRoot);
        InitLandmarks();

        BuildGround();
        BuildRoadMarkings();
        IntersectionDetails();
        for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
                BuildBlock(i, j);

        // спавн на перекрёстке в центре
        var home = PoiRegistry.Find(PoiKind.Home);
        SpawnPoint = home != null ? home.entrance + new Vector3(0, 1.2f, -1f) : new Vector3(center.x, 1.2f, center.z);

        var meshRoot = b.Build("CityMeshes");
        meshRoot.transform.SetParent(root.transform, false);
        return root;
    }

    // ---------------- земля ----------------
    void BuildGround()
    {
        float ext = 400f;
        float lo = -R - ext, hi = N * Pitch + ext;
        b.AddGround(Materials.grass, lo, lo, hi, hi, -0.03f, 8f);
        b.AddGround(Materials.asphalt, -R, -R, N * Pitch, N * Pitch, 0f, 10f);

        AddBoxCollider(new Vector3((lo + hi) / 2, -1f, (lo + hi) / 2), new Vector3(hi - lo, 2f, hi - lo));
        // невидимые стены по краю мира
        float wallH = 20f, t = 2f;
        AddBoxCollider(new Vector3((lo + hi) / 2, wallH / 2, lo - t / 2), new Vector3(hi - lo, wallH, t));
        AddBoxCollider(new Vector3((lo + hi) / 2, wallH / 2, hi + t / 2), new Vector3(hi - lo, wallH, t));
        AddBoxCollider(new Vector3(lo - t / 2, wallH / 2, (lo + hi) / 2), new Vector3(t, wallH, hi - lo));
        AddBoxCollider(new Vector3(hi + t / 2, wallH / 2, (lo + hi) / 2), new Vector3(t, wallH, hi - lo));
    }

    void AddBoxCollider(Vector3 c, Vector3 size)
    {
        var go = new GameObject("col");
        go.transform.SetParent(colRoot, false);
        go.transform.position = c;
        go.AddComponent<BoxCollider>().size = size;
        go.isStatic = true;
    }

    // ---------------- разметка ----------------
    void BuildRoadMarkings()
    {
        // дорога r проходит между блоками r-1 и r; центр = r*Pitch - R/2
        for (int r = 0; r <= N; r++)
        {
            float c = r * Pitch - R * 0.5f;
            for (int k = 0; k < N; k++)
            {
                float s0 = k * Pitch + 6f, s1 = k * Pitch + B - 6f;
                for (float s = s0; s < s1; s += 9f)
                {
                    float e = Mathf.Min(s + 4f, s1);
                    b.AddGround(Materials.lineYellow, c - 0.1f, s, c + 0.1f, e, 0.02f, 4f);   // вертикальная дорога
                    b.AddGround(Materials.lineYellow, s, c - 0.1f, e, c + 0.1f, 0.02f, 4f);   // горизонтальная
                }
            }
        }
        // пешеходные переходы на перекрёстках
        for (int r = 0; r <= N; r++)
            for (int q = 0; q <= N; q++)
            {
                float cx = r * Pitch - R * 0.5f, cz = q * Pitch - R * 0.5f;
                float d = R * 0.5f + 2.5f;
                props.Crosswalk(new Vector3(cx, 0, cz + d), false, 3f, R - 3f);
                props.Crosswalk(new Vector3(cx, 0, cz - d), false, 3f, R - 3f);
                props.Crosswalk(new Vector3(cx + d, 0, cz), true, 3f, R - 3f);
                props.Crosswalk(new Vector3(cx - d, 0, cz), true, 3f, R - 3f);
            }
    }

    // ---------------- кварталы ----------------
    enum BlockType { Buildings, Park, Plaza, Landmark, Houses }

    void BuildBlock(int i, int j)
    {
        float x0 = i * Pitch, z0 = j * Pitch, x1 = x0 + B, z1 = z0 + B;
        var c = new Vector3(x0 + B / 2, 0, z0 + B / 2);
        float dist = Vector3.Distance(c, center) / (N * Pitch * 0.5f); // 0 в центре, ~1 на краю

        BlockType type = BlockType.Buildings;
        bool isCenter = (i == N / 2 || i == N / 2 - 1) && (j == N / 2 || j == N / 2 - 1);
        if (i == N / 2 - 1 && j == N / 2 - 1) type = BlockType.Plaza;
        PoiKind lm;
        bool isLandmark = landmarkMap.TryGetValue(Key(i, j), out lm);
        if (isLandmark) type = BlockType.Landmark;
        else if (!isCenter && rng.NextDouble() < 0.12) type = BlockType.Park;
        else if (dist > 0.72f && rng.NextDouble() < 0.6) type = BlockType.Houses;

        Material top = type == BlockType.Park ? Materials.grass : (type == BlockType.Plaza ? Materials.stone : Materials.pavement);
        b.AddGround(top, x0, z0, x1, z1, Curb, type == BlockType.Park ? 8f : 8f);
        b.AddCurbs(Materials.stone, x0, z0, x1, z1, Curb, 4f);
        AddBoxCollider(new Vector3(c.x, Curb / 2, c.z), new Vector3(B, Curb, B));

        StreetFurniture(x0, z0, x1, z1, type);
        StreetDetails(x0, z0, x1, z1, type);

        if (type == BlockType.Buildings) BuildLots(x0, z0, dist);
        else if (type == BlockType.Houses) BuildHouses(x0, z0);
        else if (type == BlockType.Landmark) BuildLandmark(lm, x0, z0);
        else if (type == BlockType.Park) BuildPark(x0, z0, x1, z1);
        else BuildPlaza(c);
    }

    void StreetFurniture(float x0, float z0, float x1, float z1, BlockType type)
    {
        float inset = 1.2f;
        // фонари и деревья вдоль всех 4 сторон квартала, парковка вдоль бордюра
        for (float s = 12f; s <= B - 10f; s += 24f)
        {
            props.StreetLamp(new Vector3(x0 + s, Curb, z0 + inset), 180f);
            props.StreetLamp(new Vector3(x0 + s + 12f, Curb, z1 - inset), 0f);
            props.StreetLamp(new Vector3(x0 + inset, Curb, z0 + s + 12f), 270f);
            props.StreetLamp(new Vector3(x1 - inset, Curb, z0 + s), 90f);
        }
        for (float s = 6f; s <= B - 6f; s += (Mobile ? 24f : 12f))
        {
            if (type == BlockType.Park) break;
            props.Tree(new Vector3(x0 + s, Curb, z0 + 2.4f), rng);
            props.Tree(new Vector3(x0 + s, Curb, z1 - 2.4f), rng);
            props.Tree(new Vector3(x0 + 2.4f, Curb, z0 + s), rng);
            props.Tree(new Vector3(x1 - 2.4f, Curb, z0 + s), rng);
        }
        // припаркованные машины
        for (float s = 12f; s <= B - 12f; s += 7.5f)
        {
            if (rng.NextDouble() < 0.35) props.Car(new Vector3(x0 + s, 0, z0 - 1.5f), 90f, rng);
            if (rng.NextDouble() < 0.35) props.Car(new Vector3(x0 + s, 0, z1 + 1.5f), 90f, rng);
            if (rng.NextDouble() < 0.35) props.Car(new Vector3(x0 - 1.5f, 0, z0 + s), 0f, rng);
            if (rng.NextDouble() < 0.35) props.Car(new Vector3(x1 + 1.5f, 0, z0 + s), 0f, rng);
        }
    }

    // ---------------- здания ----------------
    void BuildLots(float x0, float z0, float dist)
    {
        float inner = B - 2 * Walk;          // 72
        float lot = inner / 2f;              // 36
        for (int a = 0; a < 2; a++)
            for (int c = 0; c < 2; c++)
            {
                float lx = x0 + Walk + a * lot, lz = z0 + Walk + c * lot;
                var lotCenter = new Vector3(lx + lot / 2, 0, lz + lot / 2);
                float d = Vector3.Distance(lotCenter, center) / (N * Pitch * 0.5f);
                BuildBuilding(lotCenter, lot, d);
            }
    }

    void BuildBuilding(Vector3 lotCenter, float lot, float d)
    {
        int floors;
        if (d < 0.28f) floors = rng.Next(28, 60);
        else if (d < 0.55f) floors = rng.Next(10, 28);
        else floors = rng.Next(4, 10);

        int style;
        if (floors >= 24) style = rng.Next(0, 3);          // стеклянные башни
        else if (floors >= 10) style = rng.Next(0, 5);
        else style = rng.Next(3, 8);                       // кирпич, штукатурка

        float fw = Mathf.Floor((lot - 3f - (float)rng.NextDouble() * 4f) / 3.5f) * 3.5f;
        float fd = Mathf.Floor((lot - 3f - (float)rng.NextDouble() * 4f) / 3.5f) * 3.5f;
        fw = Mathf.Max(fw, 14f); fd = Mathf.Max(fd, 14f);
        float floorH = 3.6f;
        int variant = rng.Next(CityMaterials.Variants);
        var mat = Materials.facades[style, variant];

        float baseY = Curb;
        if (floors > 20)
        {
            // подиум + башня с уступом
            int podium = rng.Next(3, 6);
            Tier(mat, lotCenter, fw, fd, podium, baseY, floorH, false);
            float tw = Mathf.Floor(fw * 0.7f / 3.5f) * 3.5f, td = Mathf.Floor(fd * 0.7f / 3.5f) * 3.5f;
            int towerFloors = floors - podium;
            float y = baseY + podium * floorH;
            Tier(mat, lotCenter, tw, td, towerFloors, y, floorH, true);
            if (floors > 40)
            {
                // верхний уступ
                float uw = Mathf.Floor(tw * 0.7f / 3.5f) * 3.5f, ud = Mathf.Floor(td * 0.7f / 3.5f) * 3.5f;
                float y2 = y + towerFloors * floorH;
                Tier(mat, lotCenter, uw, ud, 4, y2, floorH, true);
                Antenna(lotCenter, y2 + 4 * floorH);
            }
            else if (floors > 30) Antenna(lotCenter, y + towerFloors * floorH);
        }
        else
        {
            Tier(mat, lotCenter, fw, fd, floors, baseY, floorH, floors > 8);
            FacadeDetails(lotCenter, fw, fd, floors, baseY, style);
        }
    }

    void Tier(Material mat, Vector3 c, float w, float d, int floors, float y, float floorH, bool rooftop)
    {
        float h = floors * floorH;
        var center3 = new Vector3(c.x, y + h * 0.5f, c.z);
        b.AddBox(mat, center3, new Vector3(w, h, d), 0f, 14f);
        // крыша
        b.AddGround(Materials.roof, c.x - w / 2, c.z - d / 2, c.x + w / 2, c.z + d / 2, y + h + 0.03f, 8f);
        // парапет
        float pt = 0.4f, ph = 0.9f;
        b.AddBox(Materials.stone, new Vector3(c.x, y + h + ph / 2, c.z - d / 2 + pt / 2), new Vector3(w, ph, pt), 0f, 4f);
        b.AddBox(Materials.stone, new Vector3(c.x, y + h + ph / 2, c.z + d / 2 - pt / 2), new Vector3(w, ph, pt), 0f, 4f);
        b.AddBox(Materials.stone, new Vector3(c.x - w / 2 + pt / 2, y + h + ph / 2, c.z), new Vector3(pt, ph, d - 2 * pt), 0f, 4f);
        b.AddBox(Materials.stone, new Vector3(c.x + w / 2 - pt / 2, y + h + ph / 2, c.z), new Vector3(pt, ph, d - 2 * pt), 0f, 4f);
        if (rooftop)
        {
            // техника на крыше
            int units = rng.Next(1, 4);
            for (int k = 0; k < units; k++)
            {
                float ux = c.x + ((float)rng.NextDouble() - 0.5f) * (w - 6f);
                float uz = c.z + ((float)rng.NextDouble() - 0.5f) * (d - 6f);
                float us = 2f + (float)rng.NextDouble() * 3f;
                b.AddBox(Materials.metal, new Vector3(ux, y + h + us * 0.35f, uz), new Vector3(us, us * 0.7f, us * 0.8f), (float)rng.NextDouble() * 90f, 2f);
            }
        }
        AddBoxCollider(center3, new Vector3(w, h, d));
    }

    void Antenna(Vector3 c, float topY)
    {
        float h = 25f;
        b.AddBox(Materials.metal, new Vector3(c.x, topY + h / 2, c.z), new Vector3(0.5f, h, 0.5f), 0f, 2f);
        b.AddBox(Materials.redBeacon, new Vector3(c.x, topY + h + 0.4f, c.z), new Vector3(0.8f, 0.8f, 0.8f), 0f, 1f);
    }

    // ---------------- парк ----------------
    void BuildPark(float x0, float z0, float x1, float z1)
    {
        float cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
        // дорожки крестом
        b.AddGround(Materials.gravel, cx - 2f, z0 + 4f, cx + 2f, z1 - 4f, Curb + 0.02f, 4f);
        b.AddGround(Materials.gravel, x0 + 4f, cz - 2f, x1 - 4f, cz + 2f, Curb + 0.02f, 4f);
        // пруд в одном из квадрантов
        float px0 = cx + 8f, pz0 = cz + 8f, px1 = x1 - 8f, pz1 = z1 - 8f;
        b.AddGround(Materials.water, px0, pz0, px1, pz1, Curb + 0.04f, 8f);
        AddBoxCollider(new Vector3((px0 + px1) / 2, 0.8f, (pz0 + pz1) / 2), new Vector3(px1 - px0, 1.6f, pz1 - pz0));
        b.AddCurbs(Materials.stone, px0, pz0, px1, pz1, 0.45f, 3f);
        // деревья
        int trees = Mobile ? 25 : 70;
        for (int t = 0; t < trees; t++)
        {
            float tx = x0 + 6f + (float)rng.NextDouble() * (B - 12f);
            float tz = z0 + 6f + (float)rng.NextDouble() * (B - 12f);
            if (Mathf.Abs(tx - cx) < 4f || Mathf.Abs(tz - cz) < 4f) continue;
            if (tx > px0 - 3f && tx < px1 + 3f && tz > pz0 - 3f && tz < pz1 + 3f) continue;
            props.Tree(new Vector3(tx, Curb, tz), rng);
        }
        // скамейки вдоль дорожек
        props.Bench(new Vector3(cx + 3.2f, Curb, cz - 14f), 270f);
        props.Bench(new Vector3(cx - 3.2f, Curb, cz + 14f), 90f);
        props.Bench(new Vector3(cx - 14f, Curb, cz + 3.2f), 180f);
        props.Bench(new Vector3(cx + 14f, Curb, cz - 3.2f), 0f);
    }

    // ---------------- центральная площадь ----------------
    void BuildPlaza(Vector3 c)
    {
        // ступенчатый постамент и обелиск
        b.AddBox(Materials.stone, new Vector3(c.x, Curb + 0.3f, c.z), new Vector3(14f, 0.6f, 14f), 0f, 3f);
        b.AddBox(Materials.stone, new Vector3(c.x, Curb + 0.9f, c.z), new Vector3(10f, 0.6f, 10f), 0f, 3f);
        b.AddBox(Materials.stone, new Vector3(c.x, Curb + 9f, c.z), new Vector3(2.4f, 16f, 2.4f), 0f, 3f);
        AddBoxCollider(new Vector3(c.x, 5f, c.z), new Vector3(14f, 10f, 14f));
        // скамейки и деревья по кругу
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI / 4f;
            props.Bench(new Vector3(c.x + Mathf.Cos(a) * 14f, Curb, c.z + Mathf.Sin(a) * 14f), -a * Mathf.Rad2Deg + 90f);
            props.Tree(new Vector3(c.x + Mathf.Cos(a + 0.4f) * 24f, Curb, c.z + Mathf.Sin(a + 0.4f) * 24f), rng);
        }
    }
}
