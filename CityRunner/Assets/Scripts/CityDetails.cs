using UnityEngine;

/// <summary>Детали города: светофоры, остановки, урны, гидранты, люки, разметка кромок, частные дома, фасадные детали.</summary>
public partial class CityGenerator
{
    Material tlRed, tlGreen;

    void InitDetailMats()
    {
        if (tlRed != null) return;
        tlRed = ProceduralTextures.Emissive(new Color(0.4f, 0.03f, 0.03f), new Color(2.4f, 0.15f, 0.1f));
        tlGreen = ProceduralTextures.Emissive(new Color(0.03f, 0.35f, 0.1f), new Color(0.1f, 2.0f, 0.5f));
    }

    // ---------- перекрёстки ----------
    void IntersectionDetails()
    {
        InitDetailMats();
        for (int r = 0; r <= N; r++)
            for (int q = 0; q <= N; q++)
            {
                float cx = r * Pitch - R * 0.5f, cz = q * Pitch - R * 0.5f;
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        int bi = r + (sx > 0 ? 0 : -1), bj = q + (sz > 0 ? 0 : -1);
                        if (bi < 0 || bi >= N || bj < 0 || bj >= N) continue;
                        if (Mobile && sx != sz) continue; // на телефоне — только по диагонали
                        TrafficLight(new Vector3(cx + sx * (R * 0.5f + 0.8f), Curb, cz + sz * (R * 0.5f + 0.8f)), sx, sz);
                    }
                // люки у перекрёстков
                var hole = Col(new Color(0.08f, 0.08f, 0.09f), 0.3f, 0.6f);
                b.AddData(hole, lmDisc, Matrix4x4.TRS(new Vector3(cx + 3.2f, 0f, cz - 2.4f), Quaternion.identity, new Vector3(0.45f, 0.03f, 0.45f)));
            }
    }

    void TrafficLight(Vector3 p, int sx, int sz)
    {
        b.AddData(Materials.pole, lmCyl, Matrix4x4.TRS(p, Quaternion.identity, new Vector3(0.09f, 4.4f, 0.09f)));
        var dark = Col(new Color(0.06f, 0.06f, 0.07f), 0.4f);
        float y = Curb + 3.9f;
        // голова, смотрящая вдоль x
        b.AddBox(dark, new Vector3(p.x + sx * 0.2f, y, p.z), new Vector3(0.25f, 0.95f, 0.4f), 0f, 1f);
        b.AddBox(tlRed, new Vector3(p.x + sx * 0.34f, y + 0.3f, p.z), new Vector3(0.06f, 0.2f, 0.2f), 0f, 1f);
        b.AddBox(dark, new Vector3(p.x + sx * 0.34f, y, p.z), new Vector3(0.06f, 0.2f, 0.2f), 0f, 1f);
        b.AddBox(dark, new Vector3(p.x + sx * 0.34f, y - 0.3f, p.z), new Vector3(0.06f, 0.2f, 0.2f), 0f, 1f);
        // голова, смотрящая вдоль z
        b.AddBox(dark, new Vector3(p.x, y, p.z + sz * 0.2f), new Vector3(0.4f, 0.95f, 0.25f), 0f, 1f);
        b.AddBox(dark, new Vector3(p.x, y + 0.3f, p.z + sz * 0.34f), new Vector3(0.2f, 0.2f, 0.06f), 0f, 1f);
        b.AddBox(dark, new Vector3(p.x, y, p.z + sz * 0.34f), new Vector3(0.2f, 0.2f, 0.06f), 0f, 1f);
        b.AddBox(tlGreen, new Vector3(p.x, y - 0.3f, p.z + sz * 0.34f), new Vector3(0.2f, 0.2f, 0.06f), 0f, 1f);
        AddBoxCollider(p + Vector3.up * 1.5f, new Vector3(0.3f, 3f, 0.3f));
    }

    // ---------- детали по периметру квартала ----------
    void StreetDetails(float x0, float z0, float x1, float z1, BlockType type)
    {
        // осевые белые кромки дороги
        var ln = Materials.lineWhite; float ly = 0.02f, lw = 0.12f, o = 0.7f, st = 5.5f;
        b.AddGround(ln, x0 + st, z0 - o - lw, x1 - st, z0 - o, ly, 4f);
        b.AddGround(ln, x0 + st, z1 + o, x1 - st, z1 + o + lw, ly, 4f);
        b.AddGround(ln, x0 - o - lw, z0 + st, x0 - o, z1 - st, ly, 4f);
        b.AddGround(ln, x1 + o, z0 + st, x1 + o + lw, z1 - st, ly, 4f);

        // люки на дороге
        var hole = Col(new Color(0.08f, 0.08f, 0.09f), 0.3f, 0.6f);
        b.AddData(hole, lmDisc, Matrix4x4.TRS(new Vector3(x0 + B / 2 + 5f, 0f, z0 - R * 0.5f + 3.5f), Quaternion.identity, new Vector3(0.45f, 0.03f, 0.45f)));
        b.AddData(hole, lmDisc, Matrix4x4.TRS(new Vector3(x0 - R * 0.5f - 3.5f, 0f, z0 + B / 2 - 5f), Quaternion.identity, new Vector3(0.45f, 0.03f, 0.45f)));

        if (type == BlockType.Park) return;

        // урны, гидранты, почтовые ящики
        var bin = Col(new Color(0.15f, 0.35f, 0.2f), 0.4f);
        SolidBox(bin, new Vector3(x0 + 9f, Curb + 0.45f, z0 + 1.6f), new Vector3(0.5f, 0.9f, 0.5f));
        SolidBox(bin, new Vector3(x1 - 9f, Curb + 0.45f, z1 - 1.6f), new Vector3(0.5f, 0.9f, 0.5f));
        var hyd = Col(new Color(0.75f, 0.1f, 0.08f), 0.4f);
        SolidBox(hyd, new Vector3(x0 + 20f, Curb + 0.35f, z0 + 0.7f), new Vector3(0.3f, 0.7f, 0.3f));
        SolidBox(hyd, new Vector3(x0 + 0.7f, Curb + 0.35f, z1 - 20f), new Vector3(0.3f, 0.7f, 0.3f));
        SolidBox(Col(new Color(0.1f, 0.25f, 0.6f), 0.4f), new Vector3(x1 - 0.9f, Curb + 0.6f, z0 + 30f), new Vector3(0.4f, 1.2f, 0.5f));

        // остановка
        if (type == BlockType.Buildings && rng.NextDouble() < 0.18)
        {
            float bx = x0 + B / 2f + 14f, bz = z0 + 3.2f;
            var glass = Materials.carGlass;
            SolidBox(Materials.pole, new Vector3(bx - 2f, Curb + 1.3f, bz + 0.6f), new Vector3(0.1f, 2.6f, 0.1f));
            SolidBox(Materials.pole, new Vector3(bx + 2f, Curb + 1.3f, bz + 0.6f), new Vector3(0.1f, 2.6f, 0.1f));
            SolidBox(Materials.pole, new Vector3(bx, Curb + 2.66f, bz), new Vector3(4.4f, 0.12f, 2.0f), false);
            SolidBox(glass, new Vector3(bx, Curb + 1.3f, bz + 0.65f), new Vector3(4f, 2.2f, 0.06f));
            props.Bench(new Vector3(bx, Curb, bz + 0.2f), 180f);
            SolidBox(Col(new Color(0.1f, 0.35f, 0.8f), 0.3f), new Vector3(bx + 3.2f, Curb + 3.0f, bz + 0.8f), new Vector3(0.55f, 0.55f, 0.06f), false);
            SolidBox(Materials.pole, new Vector3(bx + 3.2f, Curb + 1.5f, bz + 0.8f), new Vector3(0.07f, 3f, 0.07f));
        }
    }

    // ---------- фасадные детали обычных зданий ----------
    void FacadeDetails(Vector3 c, float fw, float fd, int floors, float baseY, int style)
    {
        float zf = c.z - fd / 2f, top = baseY + floors * FloorH;
        // карниз
        var st = Materials.stone;
        b.AddBox(st, new Vector3(c.x, top - 0.2f, c.z - fd / 2f - 0.12f), new Vector3(fw + 0.5f, 0.4f, 0.3f), 0f, 4f);
        b.AddBox(st, new Vector3(c.x, top - 0.2f, c.z + fd / 2f + 0.12f), new Vector3(fw + 0.5f, 0.4f, 0.3f), 0f, 4f);
        b.AddBox(st, new Vector3(c.x - fw / 2f - 0.12f, top - 0.2f, c.z), new Vector3(0.3f, 0.4f, fd), 0f, 4f);
        b.AddBox(st, new Vector3(c.x + fw / 2f + 0.12f, top - 0.2f, c.z), new Vector3(0.3f, 0.4f, fd), 0f, 4f);

        if (floors >= 3 && style != 0 && style != 1 && style != 2)
        {
            // витрина магазина с навесом
            var awn = new[] { new Color(0.75f, 0.12f, 0.1f), new Color(0.12f, 0.35f, 0.65f), new Color(0.15f, 0.5f, 0.25f), new Color(0.85f, 0.6f, 0.1f) };
            b.AddBox(Col(new Color(0.05f, 0.08f, 0.1f), 0.95f, 0.4f), new Vector3(c.x, baseY + 1.7f, zf - 0.05f), new Vector3(fw - 1.4f, 3.0f, 0.1f), 0f, 2f);
            b.AddBox(Col(awn[rng.Next(awn.Length)], 0.2f), new Vector3(c.x, baseY + 3.5f, zf - 0.7f), new Vector3(fw * 0.7f, 0.12f, 1.4f), 0f, 2f);
            AddBoxCollider(new Vector3(c.x, baseY + 1.7f, zf - 0.05f), new Vector3(fw - 1.4f, 3.0f, 0.1f));
        }
        if (!Mobile && floors >= 3 && floors <= 9 && style >= 3 && rng.NextDouble() < 0.5)
        {
            var rail = Materials.pole;
            for (int f = 1; f < floors; f++)
                for (float x = c.x - fw / 2f + 1.75f; x < c.x + fw / 2f - 1f; x += 7f)
                {
                    float by = baseY + f * FloorH + 0.05f;
                    b.AddBox(st, new Vector3(x, by, zf - 0.5f), new Vector3(2.6f, 0.12f, 1.0f), 0f, 2f);
                    b.AddBox(rail, new Vector3(x, by + 0.5f, zf - 1.0f), new Vector3(2.6f, 0.9f, 0.05f), 0f, 1f);
                    b.AddBox(rail, new Vector3(x - 1.3f, by + 0.5f, zf - 0.5f), new Vector3(0.05f, 0.9f, 1f), 0f, 1f);
                    b.AddBox(rail, new Vector3(x + 1.3f, by + 0.5f, zf - 0.5f), new Vector3(0.05f, 0.9f, 1f), 0f, 1f);
                }
        }
    }

    // ---------- частные дома ----------
    void BuildHouses(float x0, float z0)
    {
        float lot = (B - 2 * Walk) / 3f;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++) House(x0 + Walk + i * lot, z0 + Walk + j * lot, lot);
    }

    void GableRoof(Material m, Material wall, float cx, float cz, float w, float d, float yb, float h, float ov = 0.5f)
    {
        float xa = cx - w / 2 - ov, xb = cx + w / 2 + ov, half = d / 2 + ov;
        b.AddQuad(m, new Vector3(xa, yb, cz - half), new Vector3(xb - xa, 0, 0), new Vector3(0, h, half), Vector2.zero, new Vector2((xb - xa) / 3f, half / 3f));
        b.AddQuad(m, new Vector3(xb, yb, cz + half), new Vector3(-(xb - xa), 0, 0), new Vector3(0, h, -half), Vector2.zero, new Vector2((xb - xa) / 3f, half / 3f));
        float hh = h * (half - d / 2) / half;   // высота кровли над стеной (свес)
        b.AddTri(wall, new Vector3(cx - w / 2, yb, cz - d / 2), new Vector3(cx - w / 2, yb, cz + d / 2), new Vector3(cx - w / 2, yb + h - hh * 0f, cz), Vector3.left, 14f);
        b.AddTri(wall, new Vector3(cx + w / 2, yb, cz - d / 2), new Vector3(cx + w / 2, yb, cz + d / 2), new Vector3(cx + w / 2, yb + h - hh * 0f, cz), Vector3.right, 14f);
    }

    void House(float lx, float lz, float lot)
    {
        float w = 10.5f + 3.5f * rng.Next(0, 2), d = 8f + 2f * rng.Next(0, 2);
        int floors = rng.Next(1, 3);
        float hx = lx + lot / 2f, hz = lz + 8.5f + d / 2f;
        int style = rng.Next(4, 8);
        var mat = Materials.facades[style, rng.Next(2)];
        float H = floors * FloorH;
        SolidBox(mat, new Vector3(hx, Curb + H / 2, hz), new Vector3(w, H, d), true);
        var roofCols = new[] { new Color(0.45f, 0.14f, 0.1f), new Color(0.22f, 0.24f, 0.28f), new Color(0.4f, 0.28f, 0.2f) };
        GableRoof(Col(roofCols[rng.Next(roofCols.Length)], 0.2f), mat, hx, hz, w, d, Curb + H, 2.8f);
        float zf = hz - d / 2f;
        b.AddBox(Col(new Color(0.25f, 0.14f, 0.08f), 0.3f), new Vector3(hx, Curb + 1.05f, zf - 0.05f), new Vector3(1.1f, 2.1f, 0.1f), 0f, 1f);
        b.AddBox(Materials.stone, new Vector3(hx, Curb + 0.1f, zf - 0.6f), new Vector3(2f, 0.2f, 1.2f), 0f, 2f);
        b.AddBox(Col(new Color(0.3f, 0.3f, 0.3f), 0.3f), new Vector3(hx, Curb + 2.6f, zf - 0.7f), new Vector3(2.4f, 0.12f, 1.4f), 0f, 2f);
        SolidBox(Col(new Color(0.5f, 0.3f, 0.22f), 0.1f), new Vector3(hx + w / 3f, Curb + H + 2.6f, hz + d / 4f), new Vector3(0.8f, 2.6f, 0.8f), false);
        Ground(Materials.pavement, hx - 0.9f, lz + 1f, hx + 0.9f, zf - 1.2f, Curb + 0.02f, 4f);
        Fence(lx + 1f, lz + 1f, lx + lot - 1f, lz + lot - 1f, 2.2f);
        if (rng.NextDouble() < 0.4) props.Car(new Vector3(lx + lot - 4f, Curb, lz + 5f), 0f, rng);
        props.Tree(new Vector3(lx + 4f, Curb, lz + 4f + (float)rng.NextDouble() * 3f), rng);
        if (rng.NextDouble() < 0.5) props.Tree(new Vector3(lx + lot - 4f, Curb, lz + lot - 3f), rng);
        var hedge = Col(new Color(0.12f, 0.3f, 0.1f), 0.1f);
        SolidBox(hedge, new Vector3(hx - w / 2f + 1.5f, Curb + 0.4f, zf - 0.8f), new Vector3(2.5f, 0.8f, 0.8f), false);
        SolidBox(hedge, new Vector3(hx + w / 2f - 1.5f, Curb + 0.4f, zf - 0.8f), new Vector3(2.5f, 0.8f, 0.8f), false);
    }
}
