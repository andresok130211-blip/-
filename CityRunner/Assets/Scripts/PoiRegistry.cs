using System.Collections.Generic;
using UnityEngine;

public enum PoiKind { Home, Kindergarten, School, College, Hospital, Office, Factory, Shop, Police, Retirement, Cafe, Supermarket, FireStation, PostOffice, Bank }

public class Poi
{
    public string name;
    public PoiKind kind;
    public Vector3 entrance;   // точка перед входом
    public Color color;
    public List<KeyValuePair<string, Vector3>> stations = new List<KeyValuePair<string, Vector3>>(); // рабочие места внутри (для будущих профессий)
    public Bounds interior;
}

/// <summary>Реестр важных мест города (для навигации, миникарты и будущей игровой логики).</summary>
public static class PoiRegistry
{
    public static readonly List<Poi> All = new List<Poi>();
    public static Poi Find(PoiKind k) { foreach (var p in All) if (p.kind == k) return p; return null; }
}
