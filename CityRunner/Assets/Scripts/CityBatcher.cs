using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Накапливает геометрию по материалам и ячейкам сетки, затем строит небольшое число крупных мешей:
/// мало draw call'ов, при этом работает frustum culling по ячейкам.
/// </summary>
public class CityBatcher
{
    public class MeshData
    {
        public Vector3[] v, n;
        public Vector2[] uv;
        public int[] t;
    }

    class Buffer
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();
    }

    readonly Dictionary<Material, Dictionary<long, Buffer>> data = new Dictionary<Material, Dictionary<long, Buffer>>();
    readonly float cellSize;

    public CityBatcher(float cellSize = 160f) { this.cellSize = cellSize; }

    Buffer Get(Material mat, Vector3 p)
    {
        Dictionary<long, Buffer> cells;
        if (!data.TryGetValue(mat, out cells)) { cells = new Dictionary<long, Buffer>(); data[mat] = cells; }
        int cx = Mathf.FloorToInt(p.x / cellSize), cz = Mathf.FloorToInt(p.z / cellSize);
        long key = ((long)cx << 32) ^ (uint)cz;
        Buffer b;
        if (!cells.TryGetValue(key, out b)) { b = new Buffer(); cells[key] = b; }
        return b;
    }

    // ---------- примитивы ----------

    /// <summary>Четырёхугольник. Нормаль = Cross(v, u) (смотрит наружу, если порядок выбран правильно).</summary>
    public void AddQuad(Material mat, Vector3 origin, Vector3 u, Vector3 v, Vector2 uv0, Vector2 uvSize)
    {
        var b = Get(mat, origin + (u + v) * 0.5f);
        int i = b.v.Count;
        Vector3 nrm = Vector3.Cross(v, u).normalized;
        b.v.Add(origin); b.v.Add(origin + v); b.v.Add(origin + u + v); b.v.Add(origin + u);
        for (int k = 0; k < 4; k++) b.n.Add(nrm);
        b.uv.Add(uv0);
        b.uv.Add(uv0 + new Vector2(0f, uvSize.y));
        b.uv.Add(uv0 + uvSize);
        b.uv.Add(uv0 + new Vector2(uvSize.x, 0f));
        b.t.Add(i); b.t.Add(i + 1); b.t.Add(i + 2);
        b.t.Add(i); b.t.Add(i + 2); b.t.Add(i + 3);
    }


    /// <summary>Треугольник; hint — желаемое направление нормали (наружу).</summary>
    public void AddTri(Material mat, Vector3 a, Vector3 bb, Vector3 c, Vector3 hint, float tile = 4f)
    {
        if (Vector3.Dot(Vector3.Cross(bb - a, c - a), hint) < 0f) { var t = bb; bb = c; c = t; }
        var buf = Get(mat, (a + bb + c) / 3f);
        int i = buf.v.Count;
        Vector3 n = Vector3.Cross(bb - a, c - a).normalized;
        buf.v.Add(a); buf.v.Add(bb); buf.v.Add(c);
        for (int k = 0; k < 3; k++) buf.n.Add(n);
        buf.uv.Add(new Vector2((a.x + a.z) / tile, a.y / tile));
        buf.uv.Add(new Vector2((bb.x + bb.z) / tile, bb.y / tile));
        buf.uv.Add(new Vector2((c.x + c.z) / tile, c.y / tile));
        buf.t.Add(i); buf.t.Add(i + 1); buf.t.Add(i + 2);
    }

    /// <summary>Горизонтальная плоскость, смотрит вверх, UV в метрах (tile = размер текстуры в метрах).</summary>
    public void AddGround(Material mat, float x0, float z0, float x1, float z1, float y, float tile)
    {
        AddQuad(mat, new Vector3(x0, y, z0), new Vector3(x1 - x0, 0, 0), new Vector3(0, 0, z1 - z0),
                new Vector2(x0 / tile, z0 / tile), new Vector2((x1 - x0) / tile, (z1 - z0) / tile));
    }

    /// <summary>Вертикальные грани бордюра по периметру прямоугольника (наружу).</summary>
    public void AddCurbs(Material mat, float x0, float z0, float x1, float z1, float h, float tile)
    {
        float w = x1 - x0, d = z1 - z0;
        var uvs = new Vector2(1f, h / tile);
        AddQuad(mat, new Vector3(x1, 0, z1), new Vector3(-w, 0, 0), new Vector3(0, h, 0), Vector2.zero, new Vector2(w / tile, h / tile)); // +z
        AddQuad(mat, new Vector3(x0, 0, z0), new Vector3(w, 0, 0), new Vector3(0, h, 0), Vector2.zero, new Vector2(w / tile, h / tile));   // -z
        AddQuad(mat, new Vector3(x1, 0, z0), new Vector3(0, 0, d), new Vector3(0, h, 0), Vector2.zero, new Vector2(d / tile, h / tile));   // +x
        AddQuad(mat, new Vector3(x0, 0, z1), new Vector3(0, 0, -d), new Vector3(0, h, 0), Vector2.zero, new Vector2(d / tile, h / tile));  // -x
    }

    /// <summary>Параллелепипед (без днища), повёрнут вокруг Y. UV в метрах.</summary>
    public void AddBox(Material mat, Vector3 center, Vector3 size, float yaw, float tile = 2f)
    {
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 hx = rot * new Vector3(size.x * 0.5f, 0, 0);
        Vector3 hy = new Vector3(0, size.y * 0.5f, 0);
        Vector3 hz = rot * new Vector3(0, 0, size.z * 0.5f);
        AddQuad(mat, center + hy - hx - hz, hx * 2f, hz * 2f, Vector2.zero, new Vector2(size.x / tile, size.z / tile)); // верх
        AddQuad(mat, center - hy + hx + hz, -hx * 2f, hy * 2f, Vector2.zero, new Vector2(size.x / tile, size.y / tile)); // +z
        AddQuad(mat, center - hy - hx - hz, hx * 2f, hy * 2f, Vector2.zero, new Vector2(size.x / tile, size.y / tile));  // -z
        AddQuad(mat, center - hy + hx - hz, hz * 2f, hy * 2f, Vector2.zero, new Vector2(size.z / tile, size.y / tile));  // +x
        AddQuad(mat, center - hy - hx + hz, -hz * 2f, hy * 2f, Vector2.zero, new Vector2(size.z / tile, size.y / tile)); // -x
    }

    /// <summary>Добавить готовую геометрию с матрицей трансформации.</summary>
    public void AddData(Material mat, MeshData d, Matrix4x4 m)
    {
        var b = Get(mat, m.MultiplyPoint3x4(Vector3.zero));
        int baseI = b.v.Count;
        Matrix4x4 nm = m.inverse.transpose;
        for (int i = 0; i < d.v.Length; i++)
        {
            b.v.Add(m.MultiplyPoint3x4(d.v[i]));
            b.n.Add(nm.MultiplyVector(d.n[i]).normalized);
            b.uv.Add(d.uv[i]);
        }
        for (int i = 0; i < d.t.Length; i++) b.t.Add(baseI + d.t[i]);
    }

    // ---------- шаблоны геометрии ----------

    /// <summary>Призма с радиусом 1 и высотой 1 (от y=0 до y=1), без крышек. Для столбов и стволов.</summary>
    public static MeshData Cylinder(int sides)
    {
        var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        for (int i = 0; i <= sides; i++)
        {
            float a = i / (float)sides * Mathf.PI * 2f;
            float x = Mathf.Cos(a), z = Mathf.Sin(a);
            verts.Add(new Vector3(x, 0, z)); verts.Add(new Vector3(x, 1, z));
            norms.Add(new Vector3(x, 0, z)); norms.Add(new Vector3(x, 0, z));
            uvs.Add(new Vector2(i / (float)sides, 0)); uvs.Add(new Vector2(i / (float)sides, 1));
        }
        for (int i = 0; i < sides; i++)
        {
            int b0 = 2 * i, t0 = 2 * i + 1, b1 = 2 * i + 2, t1 = 2 * i + 3;
            tris.Add(b0); tris.Add(t0); tris.Add(b1);
            tris.Add(t0); tris.Add(t1); tris.Add(b1);
        }
        return new MeshData { v = verts.ToArray(), n = norms.ToArray(), uv = uvs.ToArray(), t = tris.ToArray() };
    }

    /// <summary>Крышка цилиндра (диск, смотрит вверх) радиуса 1 на высоте y=1.</summary>
    public static MeshData Disc(int sides)
    {
        var verts = new List<Vector3> { new Vector3(0, 1, 0) };
        var norms = new List<Vector3> { Vector3.up };
        var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
        var tris = new List<int>();
        for (int i = 0; i < sides; i++)
        {
            float a = i / (float)sides * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a), 1, Mathf.Sin(a)));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
        }
        for (int i = 0; i < sides; i++)
        {
            int a = 1 + i, b = 1 + (i + 1) % sides;
            // нормаль наружу (вверх): Cross(b-a, c-a) должен быть +y
            tris.Add(0); tris.Add(b); tris.Add(a);
        }
        return new MeshData { v = verts.ToArray(), n = norms.ToArray(), uv = uvs.ToArray(), t = tris.ToArray() };
    }

    /// <summary>Неровная низкополигональная «сфера» радиуса ~1 для крон деревьев.</summary>
    public static MeshData Blob(int seed, float roughness)
    {
        float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
        var verts = new List<Vector3>
        {
            new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
            new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
            new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1)
        };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
        var faces = new List<int[]>
        {
            new[]{0,11,5}, new[]{0,5,1}, new[]{0,1,7}, new[]{0,7,10}, new[]{0,10,11},
            new[]{1,5,9}, new[]{5,11,4}, new[]{11,10,2}, new[]{10,7,6}, new[]{7,1,8},
            new[]{3,9,4}, new[]{3,4,2}, new[]{3,2,6}, new[]{3,6,8}, new[]{3,8,9},
            new[]{4,9,5}, new[]{2,4,11}, new[]{6,2,10}, new[]{8,6,7}, new[]{9,8,1}
        };
        var cache = new Dictionary<long, int>();
        System.Func<int, int, int> mid = (a, b) =>
        {
            long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
            int idx;
            if (cache.TryGetValue(key, out idx)) return idx;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
            cache[key] = verts.Count - 1;
            return verts.Count - 1;
        };
        var next = new List<int[]>();
        foreach (var f in faces)
        {
            int ab = mid(f[0], f[1]), bc = mid(f[1], f[2]), ca = mid(f[2], f[0]);
            next.Add(new[] { f[0], ab, ca }); next.Add(new[] { f[1], bc, ab });
            next.Add(new[] { f[2], ca, bc }); next.Add(new[] { ab, bc, ca });
        }
        var rng = new System.Random(seed);
        for (int i = 0; i < verts.Count; i++)
            verts[i] = verts[i] * (1f - roughness * 0.5f + roughness * (float)rng.NextDouble());

        var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        foreach (var v in verts)
        {
            norms.Add(v.normalized);
            uvs.Add(new Vector2(Mathf.Atan2(v.z, v.x) / (2f * Mathf.PI) + 0.5f, v.y * 0.5f + 0.5f));
        }
        foreach (var f in next)
        {
            Vector3 a = verts[f[0]], b = verts[f[1]], c = verts[f[2]];
            bool outward = Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) > 0f;
            if (outward) { tris.Add(f[0]); tris.Add(f[1]); tris.Add(f[2]); }
            else { tris.Add(f[0]); tris.Add(f[2]); tris.Add(f[1]); }
        }
        return new MeshData { v = verts.ToArray(), n = norms.ToArray(), uv = uvs.ToArray(), t = tris.ToArray() };
    }

    // ---------- сборка ----------
    public GameObject Build(string name)
    {
        var root = new GameObject(name);
        foreach (var kv in data)
        {
            foreach (var cell in kv.Value)
            {
                var b = cell.Value;
                var mesh = new Mesh { name = name + "_mesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(b.v);
                mesh.SetNormals(b.n);
                mesh.SetUVs(0, b.uv);
                mesh.SetTriangles(b.t, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                mesh.UploadMeshData(true);

                var go = new GameObject("chunk");
                go.transform.SetParent(root.transform, false);
                go.isStatic = true;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = kv.Key;
            }
        }
        data.Clear();
        return root;
    }
}
