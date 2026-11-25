using System.Collections.Generic;
using UnityEngine;

/// <summary>Все материалы города. Создаются один раз, переиспользуются всеми зданиями (мало draw call'ов).</summary>
public class CityMaterials
{
    public const int StyleCount = 8;
    public const int Variants = 2;

    public class EmissiveEntry
    {
        public Material material;
        public Color baseEmission;
        public float nightStrength;
    }

    public Material[,] facades = new Material[StyleCount, Variants];
    public Material asphalt, pavement, grass, roof, gravel, stone, metal, trunk, water, wood;
    public Material lineWhite, lineYellow, tire, carGlass, lampHousing, pole;
    public Material[] foliage;
    public Material[] carPaints;
    public readonly List<EmissiveEntry> emissives = new List<EmissiveEntry>();
    public Material lampWarm, redBeacon;

    static ProceduralTextures.FacadeStyle Style(Color wall, Color gTop, Color gBot, float x0, float x1, float y0, float y1,
                                                float wallSmooth, float glassSmooth, float lit, bool brick = false)
    {
        return new ProceduralTextures.FacadeStyle
        {
            wall = wall, glassTop = gTop, glassBottom = gBot,
            winX0 = x0, winX1 = x1, winY0 = y0, winY1 = y1,
            wallSmooth = wallSmooth, glassSmooth = glassSmooth, litChance = lit, brick = brick
        };
    }

    public CityMaterials(int seed)
    {
        var styles = new[]
        {
            Style(new Color(0.10f,0.12f,0.15f), new Color(0.55f,0.72f,0.88f), new Color(0.12f,0.24f,0.38f), 0.03f,0.97f,0.10f,0.90f, 0.5f,0.92f,0.40f), // 0 стекло синее
            Style(new Color(0.09f,0.13f,0.13f), new Color(0.50f,0.78f,0.78f), new Color(0.10f,0.28f,0.30f), 0.03f,0.97f,0.10f,0.90f, 0.5f,0.92f,0.40f), // 1 стекло бирюзовое
            Style(new Color(0.08f,0.08f,0.09f), new Color(0.35f,0.40f,0.46f), new Color(0.07f,0.09f,0.12f), 0.03f,0.97f,0.10f,0.90f, 0.5f,0.90f,0.35f), // 2 стекло тёмное
            Style(new Color(0.62f,0.62f,0.60f), new Color(0.50f,0.62f,0.74f), new Color(0.15f,0.22f,0.30f), 0.10f,0.90f,0.18f,0.84f, 0.15f,0.85f,0.40f), // 3 бетон/офис
            Style(new Color(0.76f,0.69f,0.57f), new Color(0.48f,0.60f,0.72f), new Color(0.14f,0.20f,0.28f), 0.22f,0.78f,0.20f,0.82f, 0.12f,0.85f,0.45f), // 4 камень
            Style(new Color(0.52f,0.24f,0.17f), new Color(0.40f,0.52f,0.62f), new Color(0.12f,0.17f,0.24f), 0.25f,0.75f,0.22f,0.80f, 0.08f,0.80f,0.45f, true), // 5 кирпич красный
            Style(new Color(0.40f,0.28f,0.22f), new Color(0.40f,0.52f,0.62f), new Color(0.12f,0.17f,0.24f), 0.25f,0.75f,0.22f,0.80f, 0.08f,0.80f,0.45f, true), // 6 кирпич коричневый
            Style(new Color(0.84f,0.82f,0.77f), new Color(0.45f,0.58f,0.70f), new Color(0.13f,0.19f,0.27f), 0.25f,0.75f,0.20f,0.80f, 0.10f,0.80f,0.50f), // 7 штукатурка
        };

        for (int s = 0; s < StyleCount; s++)
            for (int v = 0; v < Variants; v++)
            {
                var m = ProceduralTextures.Facade(styles[s], seed + s * 31 + v * 977);
                facades[s, v] = m;
                emissives.Add(new EmissiveEntry { material = m, baseEmission = Color.white, nightStrength = 1.5f });
            }

        asphalt  = ProceduralTextures.Surface(new Color(0.10f,0.10f,0.105f), new Color(0.20f,0.20f,0.21f), 16, 0.14f, seed + 1, 0, 1f, 2.2f, 0.45f);
        pavement = ProceduralTextures.Surface(new Color(0.52f,0.52f,0.50f), new Color(0.68f,0.67f,0.64f), 8, 0.15f, seed + 2, 2, 0.65f, 2.0f, 0.25f);
        grass    = ProceduralTextures.Surface(new Color(0.12f,0.24f,0.07f), new Color(0.27f,0.42f,0.12f), 24, 0.04f, seed + 3, 0, 1f, 1.4f, 0.7f);
        roof     = ProceduralTextures.Surface(new Color(0.20f,0.20f,0.20f), new Color(0.33f,0.32f,0.30f), 16, 0.10f, seed + 4, 0, 1f, 1.5f, 0.5f);
        gravel   = ProceduralTextures.Surface(new Color(0.55f,0.50f,0.42f), new Color(0.72f,0.66f,0.55f), 16, 0.08f, seed + 5, 0, 1f, 2.0f, 0.6f);
        stone    = ProceduralTextures.Surface(new Color(0.58f,0.56f,0.53f), new Color(0.76f,0.74f,0.70f), 6, 0.25f, seed + 6, 4, 0.7f, 1.5f, 0.2f);
        trunk    = ProceduralTextures.Surface(new Color(0.18f,0.13f,0.09f), new Color(0.33f,0.24f,0.17f), 8, 0.05f, seed + 7, 0, 1f, 2.5f, 0.3f);
        wood     = ProceduralTextures.Surface(new Color(0.28f,0.18f,0.11f), new Color(0.42f,0.30f,0.19f), 6, 0.2f, seed + 8, 0, 1f, 1.0f, 0.15f);

        foliage = new[]
        {
            ProceduralTextures.Surface(new Color(0.07f,0.19f,0.05f), new Color(0.20f,0.38f,0.10f), 12, 0.08f, seed + 10, 0, 1f, 2.0f, 0.8f),
            ProceduralTextures.Surface(new Color(0.11f,0.25f,0.07f), new Color(0.30f,0.46f,0.12f), 12, 0.08f, seed + 11, 0, 1f, 2.0f, 0.8f),
            ProceduralTextures.Surface(new Color(0.09f,0.21f,0.08f), new Color(0.24f,0.40f,0.15f), 12, 0.08f, seed + 12, 0, 1f, 2.0f, 0.8f),
            ProceduralTextures.Surface(new Color(0.42f,0.23f,0.04f), new Color(0.78f,0.52f,0.10f), 12, 0.08f, seed + 13, 0, 1f, 2.0f, 0.8f), // осенняя листва
        };

        water       = ProceduralTextures.Solid(new Color(0.04f,0.11f,0.15f), 0.95f, 0f);
        metal       = ProceduralTextures.Solid(new Color(0.35f,0.37f,0.40f), 0.55f, 0.8f);
        pole        = ProceduralTextures.Solid(new Color(0.12f,0.14f,0.15f), 0.45f, 0.7f);
        lampHousing = ProceduralTextures.Solid(new Color(0.15f,0.15f,0.16f), 0.5f, 0.6f);
        lineWhite   = ProceduralTextures.Solid(new Color(0.85f,0.85f,0.80f), 0.3f, 0f);
        lineYellow  = ProceduralTextures.Solid(new Color(0.88f,0.68f,0.10f), 0.3f, 0f);
        tire        = ProceduralTextures.Solid(new Color(0.03f,0.03f,0.03f), 0.2f, 0f);
        carGlass    = ProceduralTextures.Solid(new Color(0.03f,0.04f,0.06f), 0.95f, 0.3f);

        var paints = new[]
        {
            new Color(0.9f,0.9f,0.9f), new Color(0.04f,0.04f,0.05f), new Color(0.6f,0.62f,0.65f), new Color(0.6f,0.08f,0.06f),
            new Color(0.08f,0.16f,0.40f), new Color(0.2f,0.21f,0.23f), new Color(0.72f,0.66f,0.52f), new Color(0.07f,0.28f,0.16f)
        };
        carPaints = new Material[paints.Length];
        for (int i = 0; i < paints.Length; i++) carPaints[i] = ProceduralTextures.Solid(paints[i], 0.85f, 0.55f);

        Color warm = new Color(1f, 0.78f, 0.45f);
        lampWarm = ProceduralTextures.Emissive(new Color(0.9f, 0.85f, 0.7f), Color.black);
        emissives.Add(new EmissiveEntry { material = lampWarm, baseEmission = warm, nightStrength = 3.5f });
        redBeacon = ProceduralTextures.Emissive(new Color(0.6f, 0.05f, 0.05f), Color.black);
        emissives.Add(new EmissiveEntry { material = redBeacon, baseEmission = new Color(1f, 0.1f, 0.05f), nightStrength = 4f });
    }
}
