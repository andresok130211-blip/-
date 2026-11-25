using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Особые кварталы: здания с настоящими помещениями внутри.</summary>
public partial class CityGenerator
{
    readonly Dictionary<Color, Material> solidCache = new Dictionary<Color, Material>();
    readonly Dictionary<int, PoiKind> landmarkMap = new Dictionary<int, PoiKind>();
    CityBatcher.MeshData lmCyl, lmDisc;
    Font labelFont;

    static int Key(int i, int j) { return i * 1000 + j; }

    void InitLandmarks()
    {
        landmarkMap.Clear();
        PoiRegistry.All.Clear();
        int c = N / 2;
        landmarkMap[Key(c - 5, c - 3)] = PoiKind.Home;
        landmarkMap[Key(c - 5, c - 1)] = PoiKind.Kindergarten;
        landmarkMap[Key(c - 4, c + 1)] = PoiKind.School;
        landmarkMap[Key(c + 2, c + 1)] = PoiKind.College;
        landmarkMap[Key(c + 2, c - 3)] = PoiKind.Hospital;
        landmarkMap[Key(c, c + 2)] = PoiKind.Police;
        landmarkMap[Key(c - 2, c - 5)] = PoiKind.Shop;
        landmarkMap[Key(c + 6, c + 5)] = PoiKind.Factory;
        landmarkMap[Key(c - 1, c + 1)] = PoiKind.Office;
        landmarkMap[Key(c + 5, c - 5)] = PoiKind.Retirement;
        landmarkMap[Key(c - 1, c - 3)] = PoiKind.Cafe;
        landmarkMap[Key(c + 1, c - 2)] = PoiKind.Supermarket;
        landmarkMap[Key(c + 1, c + 1)] = PoiKind.Bank;
        landmarkMap[Key(c + 3, c + 3)] = PoiKind.FireStation;
        landmarkMap[Key(c - 3, c - 2)] = PoiKind.PostOffice;
        lmCyl = CityBatcher.Cylinder(16);
        lmDisc = CityBatcher.Disc(16);
        labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (labelFont == null) labelFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    Material Col(Color c, float smooth = 0.3f, float metal = 0f)
    {
        Material m;
        if (!solidCache.TryGetValue(c, out m)) { m = ProceduralTextures.Solid(c, smooth, metal); solidCache[c] = m; }
        return m;
    }

    void SolidBox(Material m, Vector3 center, Vector3 size, bool collide = true, float yaw = 0f)
    {
        b.AddBox(m, center, size, yaw, 2f);
        if (collide) AddBoxCollider(center, size);
    }

    void Ground(Material m, float x0, float z0, float x1, float z1, float y, float tile = 6f) { b.AddGround(m, x0, z0, x1, z1, y, tile); }

    void Fence(float x0, float z0, float x1, float z1, float gate)
    {
        var m = Materials.pole;
        float h = 1.3f, t = 0.12f, y = Curb + h / 2, cx = (x0 + x1) / 2;
        SolidBox(m, new Vector3((x0 + cx - gate / 2) / 2, y, z0), new Vector3(cx - gate / 2 - x0, h, t));
        SolidBox(m, new Vector3((x1 + cx + gate / 2) / 2, y, z0), new Vector3(x1 - cx - gate / 2, h, t));
        SolidBox(m, new Vector3(cx, y, z1), new Vector3(x1 - x0, h, t));
        SolidBox(m, new Vector3(x0, y, (z0 + z1) / 2), new Vector3(t, h, z1 - z0));
        SolidBox(m, new Vector3(x1, y, (z0 + z1) / 2), new Vector3(t, h, z1 - z0));
    }

    void Label(string text, Vector3 pos, float size, Color color)
    {
        if (labelFont == null) return;
        var go = new GameObject("Label_" + text);
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text; tm.font = labelFont; tm.fontSize = 64; tm.characterSize = size / 16f;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = labelFont.material;
        go.transform.SetParent(colRoot.parent, true);
    }

    void Sign(string text, Color color, float cx, float y, float zFront)
    {
        float width = Mathf.Max(5f, text.Length * 0.85f);
        b.AddBox(Col(color, 0.4f), new Vector3(cx, y, zFront - 0.15f), new Vector3(width, 1.8f, 0.3f), 0f, 2f);
        Label(text, new Vector3(cx, y, zFront - 0.4f), 1.1f, Color.white);
    }

    void Entrance(float cx, float zf, float w)
    {
        b.AddBox(Materials.stone, new Vector3(cx, Curb + 0.09f, zf - 1.2f), new Vector3(w + 2f, 0.18f, 2.4f), 0f, 3f);
        b.AddBox(Col(new Color(0.25f, 0.27f, 0.3f), 0.4f, 0.6f), new Vector3(cx, Curb + 3.55f, zf - 1.3f), new Vector3(w + 2.4f, 0.18f, 2.8f), 0f, 2f);
        b.AddBox(Materials.stone, new Vector3(cx - w / 2 - 0.1f, Curb + 1.55f, zf - 0.1f), new Vector3(0.25f, 3.1f, 0.3f), 0f, 2f);
        b.AddBox(Materials.stone, new Vector3(cx + w / 2 + 0.1f, Curb + 1.55f, zf - 0.1f), new Vector3(0.25f, 3.1f, 0.3f), 0f, 2f);
    }

    Poi Register(string name, PoiKind kind, float cx, float zEntrance, Color color)
    {
        var p = new Poi { name = name, kind = kind, entrance = new Vector3(cx, Curb, zEntrance), color = color };
        PoiRegistry.All.Add(p);
        return p;
    }

    void Parking(float x0, float z0, float x1, float z1, int cars)
    {
        Ground(Materials.asphalt, x0, z0, x1, z1, Curb + 0.02f, 10f);
        for (float x = x0 + 2.5f; x < x1 - 1f; x += 2.8f) b.AddGround(Materials.lineWhite, x, z0 + 0.5f, x + 0.12f, z0 + 5.5f, Curb + 0.04f, 4f);
        for (int k = 0; k < cars; k++) props.Car(new Vector3(x0 + 3.9f + k * 5.6f, Curb, z0 + 3f), 0f, rng);
    }

    void Chimney(Vector3 basePos, float r, float h)
    {
        var tm = Matrix4x4.TRS(basePos, Quaternion.identity, new Vector3(r, h, r));
        var m = Col(new Color(0.55f, 0.28f, 0.2f), 0.1f);
        b.AddData(m, lmCyl, tm); b.AddData(m, lmDisc, tm);
        AddBoxCollider(basePos + Vector3.up * h / 2, new Vector3(r * 2, h, r * 2));
    }

    void RoofDeck(Vector3 c, float w, float d, float yTop, bool rooftop)
    {
        b.AddGround(Materials.roof, c.x - w / 2, c.z - d / 2, c.x + w / 2, c.z + d / 2, yTop + 0.03f, 8f);
        float pt = 0.4f, ph = 0.9f, y = yTop + ph / 2;
        b.AddBox(Materials.stone, new Vector3(c.x, y, c.z - d / 2 + pt / 2), new Vector3(w, ph, pt), 0f, 4f);
        b.AddBox(Materials.stone, new Vector3(c.x, y, c.z + d / 2 - pt / 2), new Vector3(w, ph, pt), 0f, 4f);
        b.AddBox(Materials.stone, new Vector3(c.x - w / 2 + pt / 2, y, c.z), new Vector3(pt, ph, d - 2 * pt), 0f, 4f);
        b.AddBox(Materials.stone, new Vector3(c.x + w / 2 - pt / 2, y, c.z), new Vector3(pt, ph, d - 2 * pt), 0f, 4f);
        if (rooftop && w > 12f)
            for (int k = 0; k < 2; k++)
            {
                float us = 2f + (float)rng.NextDouble() * 2f;
                b.AddBox(Materials.metal, new Vector3(c.x + ((float)rng.NextDouble() - 0.5f) * (w - 6f), yTop + us * 0.35f, c.z + ((float)rng.NextDouble() - 0.5f) * (d - 6f)), new Vector3(us, us * 0.7f, us * 0.8f), (float)rng.NextDouble() * 90f, 2f);
            }
    }

    // ---------- описание здания ----------
    class BSpec
    {
        public PoiKind kind; public string name, sign, lobbyTitle = "Стойка", floor = "tile";
        public Color signCol, poiCol, wall = new Color(0.9f, 0.9f, 0.86f);
        public Material facade;
        public float w, d, doorW = 4f, lobbyD = 6f;
        public int floors = 2, intFloors = 1;
        public bool rooftop;
        public RoomType[] rooms;
        public Action<RoomRect> custom;
    }

    /// <summary>Строит здание на северной стороне квартала и возвращает координаты (cx, z входа).</summary>
    Vector2 Building(BSpec s, float cx, float z0)
    {
        float zN = z0 + B - 5f, cz = zN - s.d / 2, zf = cz - s.d / 2;
        curPoi = Register(s.name, s.kind, cx, zf - 4f, s.poiCol);
        Shell(s.facade, new Vector3(cx, 0, cz), s.w, s.d, s.floors, s.intFloors, cx, s.doorW, s.floor, s.wall);
        RoofDeck(new Vector3(cx, 0, cz), s.w, s.d, Curb + s.floors * FloorH, s.rooftop);
        Entrance(cx, zf, s.doorW);
        Sign(s.sign, s.signCol, cx, Curb + 5.4f, zf);
        var box = new RoomRect(cx - s.w / 2 + 0.4f, zf + 0.4f, cx + s.w / 2 - 0.4f, zN - 0.4f);
        if (s.rooms != null) RowsLayout(box, cx, s.lobbyD, s.rooms, s.lobbyTitle);
        if (s.custom != null) s.custom(box);
        FinishInterior(box.x0, box.z0, box.x1, box.z1);
        curPoi = null;
        return new Vector2(cx, zf);
    }

    void BuildLandmark(PoiKind kind, float x0, float z0)
    {
        float cx = x0 + B / 2;
        float yardZ0 = z0 + Walk, yardZ1 = z0 + B - Walk;
        Ground(Materials.grass, x0 + Walk, yardZ0, x0 + B - Walk, yardZ1, Curb + 0.01f, 8f);
        var F2 = Materials.facades;
        BSpec s = null; Vector2 e;
        Action<float> yardTrees = zf => { props.Tree(new Vector3(cx - 30f, Curb, zf - 8f), rng); props.Tree(new Vector3(cx + 30f, Curb, zf - 12f), rng); };

        switch (kind)
        {
            case PoiKind.Home:
                s = new BSpec { kind = kind, name = "Дом", sign = "ДОМ", signCol = new Color(0.2f, 0.45f, 0.25f), poiCol = new Color(0.3f, 0.8f, 0.4f), facade = F2[7, 0], w = 17.5f, d = 14f, floors = 1, doorW = 2f, floor = "wood", wall = new Color(0.95f, 0.9f, 0.8f), lobbyD = 4.5f, lobbyTitle = null,
                    rooms = new[] { RoomType.Bedroom, RoomType.Kitchen, RoomType.Lounge, RoomType.Bath } };
                e = Building(s, cx, z0);
                SolidBox(Col(new Color(0.35f, 0.08f, 0.06f)), new Vector3(cx, Curb + FloorH + 0.9f, z0 + B - 5f - 7f), new Vector3(19f, 0.6f, 16f), false);
                Fence(x0 + Walk + 8f, yardZ0 + 6f, x0 + B - Walk - 8f, yardZ1 - 2f, 3f);
                props.Tree(new Vector3(cx - 11f, Curb, e.y + 8f), rng); props.Tree(new Vector3(cx + 12f, Curb, e.y - 8f), rng);
                props.Bench(new Vector3(cx + 7f, Curb, e.y - 6f), 180f);
                break;
            case PoiKind.Kindergarten:
            {
                s = new BSpec { kind = kind, name = "Детский сад", sign = "ДЕТСКИЙ САД", signCol = new Color(0.95f, 0.55f, 0.1f), poiCol = new Color(1f, 0.85f, 0.2f), facade = F2[7, 1], w = 35f, d = 17.5f, floors = 2, floor = "lino", wall = new Color(0.98f, 0.92f, 0.65f), lobbyD = 5f, lobbyTitle = "Воспитатель",
                    rooms = new[] { RoomType.GroupRoom, RoomType.GroupRoom, RoomType.Nursery, RoomType.Kitchen, RoomType.Dining } };
                e = Building(s, cx, z0);
                Fence(x0 + Walk + 2f, yardZ0 + 2f, x0 + B - Walk - 2f, yardZ1 - 2f, 4f);
                var cols = new[] { new Color(0.95f, 0.3f, 0.3f), new Color(0.2f, 0.6f, 0.95f), new Color(0.98f, 0.8f, 0.15f), new Color(0.3f, 0.75f, 0.35f) };
                float py = e.y - 12f;
                SolidBox(Col(new Color(0.8f, 0.7f, 0.45f), 0.05f), new Vector3(cx - 14f, Curb + 0.15f, py), new Vector3(6f, 0.3f, 6f));
                for (int k = 0; k < 4; k++) SolidBox(Col(cols[1]), new Vector3(cx + 4f + k * 0.9f, Curb + 0.4f + (3 - k) * 0.55f, py), new Vector3(0.9f, 0.8f + (3 - k) * 1.1f, 1.6f));
                SolidBox(Col(cols[0]), new Vector3(cx + 9f, Curb + 1.1f, py + 6f), new Vector3(0.15f, 2.2f, 0.15f), false);
                SolidBox(Col(cols[0]), new Vector3(cx + 13f, Curb + 1.1f, py + 6f), new Vector3(0.15f, 2.2f, 0.15f), false);
                SolidBox(Col(cols[0]), new Vector3(cx + 11f, Curb + 2.2f, py + 6f), new Vector3(4.2f, 0.15f, 0.15f), false);
                SolidBox(Col(cols[2]), new Vector3(cx + 10f, Curb + 0.7f, py + 6f), new Vector3(0.6f, 0.08f, 0.4f), false);
                SolidBox(Col(cols[3]), new Vector3(cx + 12f, Curb + 0.7f, py + 6f), new Vector3(0.6f, 0.08f, 0.4f), false);
                yardTrees(e.y);
                break;
            }
            case PoiKind.School:
            {
                s = new BSpec { kind = kind, name = "Школа", sign = "ШКОЛА", signCol = new Color(0.15f, 0.3f, 0.65f), poiCol = new Color(0.3f, 0.5f, 1f), facade = F2[5, 0], w = 56f, d = 21f, floors = 3, doorW = 5f, floor = "lino", wall = new Color(0.9f, 0.92f, 0.8f), lobbyD = 6f, lobbyTitle = "Охрана",
                    rooms = new[] { RoomType.Classroom, RoomType.Classroom, RoomType.Classroom, RoomType.Dining, RoomType.Library, RoomType.Classroom, RoomType.Classroom, RoomType.Office } };
                e = Building(s, cx, z0);
                Fence(x0 + Walk + 1f, yardZ0 + 1f, x0 + B - Walk - 1f, yardZ1 - 1f, 6f);
                float fx0 = cx - 18f, fx1 = cx + 18f, fz0 = e.y - 36f, fz1 = e.y - 8f;
                Ground(Col(new Color(0.18f, 0.5f, 0.2f), 0.05f), fx0, fz0, fx1, fz1, Curb + 0.03f);
                var ln = Materials.lineWhite; float lw = 0.15f, ly = Curb + 0.05f;
                Ground(ln, fx0, fz0, fx1, fz0 + lw, ly); Ground(ln, fx0, fz1 - lw, fx1, fz1, ly); Ground(ln, fx0, fz0, fx0 + lw, fz1, ly); Ground(ln, fx1 - lw, fz0, fx1, fz1, ly);
                Ground(ln, cx - lw / 2, fz0, cx + lw / 2, fz1, ly);
                break;
            }
            case PoiKind.College:
                s = new BSpec { kind = kind, name = "Техникум", sign = "ТЕХНИКУМ", signCol = new Color(0.45f, 0.15f, 0.55f), poiCol = new Color(0.75f, 0.4f, 0.9f), facade = F2[3, 1], w = 49f, d = 24.5f, floors = 4, doorW = 5f, floor = "tile", wall = new Color(0.85f, 0.88f, 0.94f), lobbyD = 6f, lobbyTitle = "Деканат", rooftop = true,
                    rooms = new[] { RoomType.Lecture, RoomType.Lecture, RoomType.Workshop, RoomType.Workshop, RoomType.Library, RoomType.Office, RoomType.Dining } };
                e = Building(s, cx, z0);
                Ground(Materials.stone, cx - 22f, yardZ0 + 2f, cx + 22f, e.y - 6f, Curb + 0.02f, 4f);
                for (int k = -2; k <= 2; k++) props.Bench(new Vector3(cx + k * 8f, Curb + 0.02f, e.y - 20f), 0f);
                yardTrees(e.y);
                break;
            case PoiKind.Hospital:
                s = new BSpec { kind = kind, name = "Больница", sign = "БОЛЬНИЦА", signCol = new Color(0.1f, 0.5f, 0.75f), poiCol = new Color(0.9f, 0.2f, 0.2f), facade = F2[7, 0], w = 42f, d = 24.5f, floors = 8, doorW = 6f, floor = "tile", wall = new Color(0.9f, 0.95f, 0.97f), lobbyD = 6f, lobbyTitle = "Регистратура", rooftop = true,
                    rooms = new[] { RoomType.Exam, RoomType.Exam, RoomType.Surgery, RoomType.Ward, RoomType.Ward, RoomType.Pharmacy } };
                e = Building(s, cx, z0);
                {
                    var red = Col(new Color(0.85f, 0.08f, 0.08f)); var white = Col(Color.white);
                    b.AddBox(white, new Vector3(cx, Curb + 10f, e.y - 0.1f), new Vector3(5f, 5f, 0.2f), 0f, 2f);
                    b.AddBox(red, new Vector3(cx, Curb + 10f, e.y - 0.25f), new Vector3(3.6f, 1.1f, 0.2f), 0f, 2f);
                    b.AddBox(red, new Vector3(cx, Curb + 10f, e.y - 0.25f), new Vector3(1.1f, 3.6f, 0.2f), 0f, 2f);
                }
                Parking(cx - 24f, yardZ0 + 1f, cx + 24f, e.y - 8f, 4);
                break;
            case PoiKind.Police:
                s = new BSpec { kind = kind, name = "Полиция", sign = "ПОЛИЦИЯ", signCol = new Color(0.08f, 0.2f, 0.6f), poiCol = new Color(0.3f, 0.4f, 1f), facade = F2[3, 0], w = 35f, d = 21f, floors = 3, floor = "tile", wall = new Color(0.78f, 0.82f, 0.88f), lobbyD = 6f, lobbyTitle = "Дежурный", rooftop = true,
                    rooms = new[] { RoomType.Office, RoomType.Office, RoomType.Cell, RoomType.Cell, RoomType.Break } };
                e = Building(s, cx, z0);
                Parking(cx - 20f, yardZ0 + 1f, cx + 20f, e.y - 8f, 3);
                break;
            case PoiKind.Retirement:
                s = new BSpec { kind = kind, name = "Дом престарелых", sign = "ДОМ ПРЕСТАРЕЛЫХ", signCol = new Color(0.55f, 0.35f, 0.2f), poiCol = new Color(0.8f, 0.6f, 0.4f), facade = F2[4, 1], w = 42f, d = 17.5f, floors = 3, floor = "wood", wall = new Color(0.95f, 0.9f, 0.8f), lobbyD = 5f, lobbyTitle = "Медсестра",
                    rooms = new[] { RoomType.Bedroom, RoomType.Bedroom, RoomType.Lounge, RoomType.Dining, RoomType.Bedroom, RoomType.Bedroom } };
                e = Building(s, cx, z0);
                for (int k = -2; k <= 2; k++) { props.Bench(new Vector3(cx + k * 8f, Curb + 0.02f, e.y - 16f), 0f); props.Tree(new Vector3(cx + k * 8f + 4f, Curb, e.y - 24f), rng); }
                Ground(Materials.gravel, cx - 2f, yardZ0 + 2f, cx + 2f, e.y - 1f, Curb + 0.03f, 4f);
                break;
            case PoiKind.Bank:
                s = new BSpec { kind = kind, name = "Банк", sign = "БАНК", signCol = new Color(0.05f, 0.4f, 0.3f), poiCol = new Color(0.2f, 0.8f, 0.6f), facade = F2[4, 0], w = 35f, d = 21f, floors = 3, floor = "tile", wall = new Color(0.93f, 0.9f, 0.82f), lobbyD = 7f, lobbyTitle = "Касса", rooftop = true,
                    rooms = new[] { RoomType.Office, RoomType.Vault, RoomType.Office, RoomType.Archive, RoomType.Office } };
                e = Building(s, cx, z0);
                Parking(cx - 16f, yardZ0 + 1f, cx + 16f, e.y - 8f, 2);
                break;
            case PoiKind.PostOffice:
                s = new BSpec { kind = kind, name = "Почта", sign = "ПОЧТА", signCol = new Color(0.1f, 0.35f, 0.7f), poiCol = new Color(0.4f, 0.7f, 1f), facade = F2[5, 1], w = 28f, d = 17.5f, floors = 2, floor = "tile", wall = new Color(0.92f, 0.92f, 0.85f), lobbyD = 7f, lobbyTitle = "Окно выдачи",
                    rooms = new[] { RoomType.Storage, RoomType.Office, RoomType.Storage, RoomType.Break } };
                e = Building(s, cx, z0);
                Parking(cx - 12f, yardZ0 + 1f, cx + 12f, e.y - 8f, 2);
                break;
            case PoiKind.FireStation:
                s = new BSpec { kind = kind, name = "Пожарная часть", sign = "ПОЖАРНАЯ ЧАСТЬ", signCol = new Color(0.75f, 0.1f, 0.08f), poiCol = new Color(1f, 0.3f, 0.2f), facade = F2[5, 0], w = 42f, d = 24.5f, floors = 2, doorW = 8f, floor = "concrete", wall = new Color(0.85f, 0.83f, 0.8f), lobbyD = 4f, lobbyTitle = "Диспетчер",
                    rooms = new[] { RoomType.Garage, RoomType.Garage, RoomType.Break, RoomType.Office, RoomType.Bedroom } };
                e = Building(s, cx, z0);
                Ground(Materials.asphalt, cx - 24f, yardZ0 + 1f, cx + 24f, e.y - 1f, Curb + 0.02f, 10f);
                break;
            case PoiKind.Cafe:
                s = new BSpec { kind = kind, name = "Кафе", sign = "КАФЕ", signCol = new Color(0.65f, 0.3f, 0.1f), poiCol = new Color(1f, 0.6f, 0.3f), facade = F2[7, 1], w = 28f, d = 17.5f, floors = 2, doorW = 3.5f, floor = "wood", wall = new Color(0.95f, 0.85f, 0.7f),
                    custom = r => { float kz = r.z1 - 5.5f; Part(true, kz, r.x0, r.x1, r.cx + 6f); FillRoom(RoomType.Kitchen, new RoomRect(r.x0, kz + 0.1f, r.x1, r.z1)); FillRoom(RoomType.Dining, new RoomRect(r.x0, r.z0, r.x1, kz - 0.1f)); } };
                e = Building(s, cx, z0);
                for (int k = -1; k <= 1; k++) { SolidBox(Col(Wood), new Vector3(cx + k * 8f, Curb + 0.45f, e.y - 3.5f), new Vector3(1.2f, 0.9f, 1.2f), true); props.Tree(new Vector3(cx + k * 12f, Curb, e.y - 12f), rng); }
                break;
            case PoiKind.Supermarket:
            case PoiKind.Shop:
            {
                bool mall = kind == PoiKind.Shop;
                s = new BSpec { kind = kind, name = mall ? "Торговый центр" : "Продуктовый", sign = mall ? "ТОРГОВЫЙ ЦЕНТР" : "ПРОДУКТЫ", signCol = mall ? new Color(0.85f, 0.35f, 0.1f) : new Color(0.15f, 0.55f, 0.2f), poiCol = mall ? new Color(1f, 0.6f, 0.2f) : new Color(0.4f, 0.9f, 0.3f),
                    facade = mall ? F2[0, 1] : F2[3, 1], w = mall ? 56f : 49f, d = 28f, floors = mall ? 3 : 2, doorW = 8f, floor = "tile", wall = new Color(0.95f, 0.95f, 0.92f), rooftop = true,
                    custom = r => Market(r, mall) };
                e = Building(s, cx, z0);
                Parking(cx - 30f, yardZ0 + 1f, cx + 30f, e.y - 8f, mall ? 8 : 6);
                break;
            }
            case PoiKind.Factory:
            {
                var hall = Col(new Color(0.45f, 0.5f, 0.52f), 0.4f, 0.5f);
                // фасад-сплошной металл: создаём «материал» из однотонного
                s = new BSpec { kind = kind, name = "Завод", sign = "ЗАВОД", signCol = new Color(0.8f, 0.55f, 0.05f), poiCol = new Color(1f, 0.8f, 0.2f), facade = hall, w = 63f, d = 30f, floors = 3, intFloors = 3, doorW = 6f, floor = "concrete", wall = new Color(0.78f, 0.8f, 0.8f),
                    custom = r => Factory(r) };
                e = Building(s, cx, z0);
                Chimney(new Vector3(cx - 22f, Curb, e.y + 18f), 1.6f, 32f);
                Chimney(new Vector3(cx - 14f, Curb, e.y + 18f), 1.3f, 26f);
                Ground(Materials.gravel, x0 + Walk, yardZ0, x0 + B - Walk, e.y - 1f, Curb + 0.02f, 8f);
                break;
            }
            case PoiKind.Office:
            {
                var mat = F2[0, 0];
                s = new BSpec { kind = kind, name = "Бизнес-центр", sign = "БИЗНЕС-ЦЕНТР", signCol = new Color(0.1f, 0.35f, 0.45f), poiCol = new Color(0.3f, 0.8f, 0.9f), facade = mat, w = 49f, d = 35f, floors = 4, doorW = 7f, floor = "carpet", wall = new Color(0.88f, 0.9f, 0.92f), lobbyD = 8f, lobbyTitle = "Ресепшен",
                    rooms = new[] { RoomType.Office, RoomType.Office, RoomType.Archive, RoomType.Break, RoomType.Office, RoomType.Office, RoomType.Office } };
                e = Building(s, cx, z0);
                float cz = z0 + B - 5f - 17.5f, y = Curb + 4 * FloorH;
                Tier(mat, new Vector3(cx, 0, cz), 35f, 28f, 42, y, FloorH, true);
                Antenna(new Vector3(cx, 0, cz), y + 42 * FloorH);
                Ground(Materials.stone, cx - 30f, yardZ0 + 1f, cx + 30f, e.y - 1f, Curb + 0.02f, 4f);
                for (int k = -1; k <= 1; k += 2) { props.Tree(new Vector3(cx + k * 24f, Curb, e.y - 14f), rng); props.Bench(new Vector3(cx + k * 12f, Curb + 0.02f, e.y - 14f), 0f); }
                break;
            }
        }
    }

    // ---------- открытые залы ----------
    void Market(RoomRect r, bool mall)
    {
        // подсобки вдоль северной стены
        float bz = r.z1 - 6f;
        Part(true, bz, r.x0, r.x1, r.x0 + 6f, r.cx, r.x1 - 6f);
        FillRoom(RoomType.Storage, new RoomRect(r.x0, bz + 0.1f, r.x0 + 12f, r.z1));
        FillRoom(RoomType.Office, new RoomRect(r.cx - 6f, bz + 0.1f, r.cx + 6f, r.z1));
        FillRoom(RoomType.Break, new RoomRect(r.x1 - 12f, bz + 0.1f, r.x1, r.z1));
        Part(false, r.x0 + 12f, bz, r.z1); Part(false, r.cx - 6f, bz, r.z1); Part(false, r.cx + 6f, bz, r.z1); Part(false, r.x1 - 12f, bz, r.z1);
        // кассы у входа
        for (float x = r.x0 + 4f; x < r.x1 - 3f; x += 7f)
        {
            if (Mathf.Abs(x - r.cx) < 4.5f) continue;
            F(Grey, x, r.z0 + 4.5f, 0.9f, 2.2f, 1.0f); F(Dark, x, r.z0 + 5.4f, 0.4f, 0.4f, 0.35f, 1.0f, false); Station("Касса", x, r.z0 + 3.1f);
        }
        // стеллажи
        Color[] cols = mall ? new[] { Red, Blue, Yellow, Green } : new[] { Grey, White, Cream, Grey };
        int ci = 0;
        for (float x = r.x0 + 4f; x < r.x1 - 3f; x += 5f)
        {
            if (Mathf.Abs(x - r.cx) < 2.5f) continue;
            Shelf(x, (r.z0 + 9f + bz - 2f) / 2f, 1.0f, bz - 2f - (r.z0 + 9f), 1.9f, cols[ci++ % cols.Length]);
        }
        if (!mall) { F(Red, r.x1 - 2f, r.z0 + 7f, 1.8f, 1.0f, 0.9f); F(Green, r.x1 - 4f, r.z0 + 7f, 1.8f, 1.0f, 0.9f); }
        else { for (int k = 0; k < 4; k++) { F(Wood, r.x0 + 4f + k * 3f, r.z0 + 8f, 1.4f, 0.4f, 1.6f); } Plant(r.cx - 5f, r.cz); Plant(r.cx + 5f, r.cz); }
        Station("Торговый зал", r.cx, r.cz - 2f);
    }

    void Factory(RoomRect r)
    {
        // контора в углу
        float ox = r.x1 - 12f, oz = r.z0 + 10f;
        Part(false, ox, r.z0 + 6f, oz + 0f + 8f); Part(true, oz + 8f, ox, r.x1, ox + 6f);
        FillRoom(RoomType.Office, new RoomRect(ox + 0.1f, r.z0 + 6f, r.x1, oz + 8f));
        // конвейеры и станки
        for (int k = 0; k < 3; k++)
        {
            float z = r.z0 + 16f + k * 6f;
            F(Dark, r.x0 + 22f, z, 34f, 1.2f, 0.9f); F(Yellow, r.x0 + 22f, z, 34f, 0.8f, 0.1f, 0.9f, false);
            for (float x = r.x0 + 8f; x < r.x0 + 38f; x += 6f) { F(Grey, x, z + 1.6f, 2.4f, 1.4f, 2.0f); Station("Станок", x, z - 1.2f); }
        }
        for (int k = 0; k < 6; k++) F(new Color(0.6f, 0.45f, 0.25f), r.x1 - 3f - (k % 3) * 1.4f, r.z1 - 3f - (k / 3) * 1.4f, 1.2f, 1.2f, 1.2f + (k % 2) * 1.2f);
        F(Yellow, r.x0 + 4f, r.z0 + 6f, 1.6f, 2.8f, 2.0f); // погрузчик
        Station("Склад", r.x1 - 3f, r.z1 - 6f);
    }
}
