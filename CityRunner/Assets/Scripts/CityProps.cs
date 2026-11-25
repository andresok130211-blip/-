using System.Collections.Generic;
using UnityEngine;

/// <summary>Мелкие объекты города: деревья, фонари, скамейки, машины, разметка.</summary>
public class CityProps
{
    readonly CityBatcher b;
    readonly CityMaterials m;
    readonly Transform colliderRoot;
    readonly CityBatcher.MeshData cyl6 = CityBatcher.Cylinder(8);
    readonly CityBatcher.MeshData cyl12 = CityBatcher.Cylinder(12);
    readonly CityBatcher.MeshData disc12 = CityBatcher.Disc(12);
    readonly CityBatcher.MeshData[] blobs;

    public CityProps(CityBatcher batcher, CityMaterials mats, Transform colliders)
    {
        b = batcher; m = mats; colliderRoot = colliders;
        blobs = new[] { CityBatcher.Blob(1, 0.35f), CityBatcher.Blob(2, 0.35f), CityBatcher.Blob(3, 0.35f) };
    }

    void Cyl(Material mat, Vector3 basePos, float radius, float height)
    {
        b.AddData(mat, cyl12, Matrix4x4.TRS(basePos, Quaternion.identity, new Vector3(radius, height, radius)));
        b.AddData(mat, disc12, Matrix4x4.TRS(basePos, Quaternion.identity, new Vector3(radius, height, radius)));
    }

    void AddCollider(Vector3 center, Vector3 size, float yaw = 0f)
    {
        var go = new GameObject("col");
        go.transform.SetParent(colliderRoot, false);
        go.transform.position = center;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        go.AddComponent<BoxCollider>().size = size;
        go.isStatic = true;
    }

    public void Tree(Vector3 pos, System.Random rng)
    {
        float h = 4f + (float)rng.NextDouble() * 4f;
        float r = 1.8f + (float)rng.NextDouble() * 1.6f;
        float trunkR = 0.14f + 0.03f * h * 0.1f;
        b.AddData(m.trunk, cyl6, Matrix4x4.TRS(pos, Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), new Vector3(trunkR, h * 0.7f, trunkR)));
        var foliage = m.foliage[rng.NextDouble() < 0.12 ? 3 : rng.Next(3)];
        int blobCount = CityGenerator.Mobile ? 1 : 3;
        for (int i = 0; i < blobCount; i++)
        {
            Vector3 off = i == 0 ? Vector3.zero : new Vector3((float)(rng.NextDouble() - 0.5) * r, (float)rng.NextDouble() * r * 0.5f, (float)(rng.NextDouble() - 0.5) * r);
            float s = r * (i == 0 ? 1f : 0.7f);
            var blob = blobs[rng.Next(blobs.Length)];
            b.AddData(foliage, blob, Matrix4x4.TRS(pos + new Vector3(0, h * 0.75f + r * 0.4f, 0) + off,
                Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), new Vector3(s, s * 0.85f, s)));
        }
        AddCollider(pos + Vector3.up * 1.5f, new Vector3(trunkR * 2f, 3f, trunkR * 2f));
    }

    public void StreetLamp(Vector3 pos, float yaw)
    {
        float h = 7f;
        b.AddData(m.pole, cyl6, Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(0.09f, h, 0.09f)));
        Quaternion rot = Quaternion.Euler(0, yaw, 0);
        Vector3 arm = rot * Vector3.forward;
        b.AddBox(m.pole, pos + Vector3.up * h + arm * 0.9f, new Vector3(0.1f, 0.1f, 1.8f), yaw);
        b.AddBox(m.lampHousing, pos + Vector3.up * (h - 0.05f) + arm * 1.7f, new Vector3(0.45f, 0.14f, 0.9f), yaw);
        b.AddBox(m.lampWarm, pos + Vector3.up * (h - 0.14f) + arm * 1.7f, new Vector3(0.38f, 0.03f, 0.8f), yaw);
        AddCollider(pos + Vector3.up * 1.5f, new Vector3(0.3f, 3f, 0.3f));
    }

    public void Bench(Vector3 pos, float yaw)
    {
        Quaternion rot = Quaternion.Euler(0, yaw, 0);
        Vector3 back = rot * Vector3.back;
        b.AddBox(m.wood, pos + Vector3.up * 0.45f, new Vector3(1.8f, 0.07f, 0.5f), yaw, 1f);
        b.AddBox(m.wood, pos + Vector3.up * 0.8f + back * 0.22f, new Vector3(1.8f, 0.45f, 0.06f), yaw, 1f);
        b.AddBox(m.metal, pos + rot * new Vector3(0.8f, 0.22f, 0), new Vector3(0.06f, 0.45f, 0.45f), yaw, 1f);
        b.AddBox(m.metal, pos + rot * new Vector3(-0.8f, 0.22f, 0), new Vector3(0.06f, 0.45f, 0.45f), yaw, 1f);
        AddCollider(pos + Vector3.up * 0.5f, new Vector3(1.8f, 1f, 0.6f), yaw);
    }

    public void Car(Vector3 pos, float yaw, System.Random rng)
    {
        var paint = m.carPaints[rng.Next(m.carPaints.Length)];
        float len = 4.3f + (float)rng.NextDouble() * 0.6f;
        Quaternion rot = Quaternion.Euler(0, yaw, 0);
        b.AddBox(paint, pos + Vector3.up * 0.7f, new Vector3(1.8f, 0.7f, len), yaw, 2f);
        b.AddBox(m.carGlass, pos + rot * new Vector3(0, 1.28f, -0.15f), new Vector3(1.6f, 0.55f, len * 0.55f), yaw, 2f);
        b.AddBox(paint, pos + rot * new Vector3(0, 1.58f, -0.15f), new Vector3(1.55f, 0.04f, len * 0.5f), yaw, 2f);
        b.AddBox(m.tire, pos + Vector3.up * 0.3f, new Vector3(1.7f, 0.3f, len - 0.6f), yaw, 2f);
        AddCollider(pos + Vector3.up * 0.9f, new Vector3(1.8f, 1.8f, len), yaw);
    }

    /// <summary>Пешеходный переход: полосы вдоль направления движения.</summary>
    public void Crosswalk(Vector3 center, bool alongX, float length, float width)
    {
        int stripes = Mathf.FloorToInt(width / 1.0f);
        for (int i = 0; i < stripes; i += 2)
        {
            float o = -width * 0.5f + i + 0.5f;
            if (alongX) b.AddGround(m.lineWhite, center.x - length * 0.5f, center.z + o, center.x + length * 0.5f, center.z + o + 0.5f, 0.02f, 4f);
            else b.AddGround(m.lineWhite, center.x + o, center.z - length * 0.5f, center.x + o + 0.5f, center.z + length * 0.5f, 0.02f, 4f);
        }
    }
}
