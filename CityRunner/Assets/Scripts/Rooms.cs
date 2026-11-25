using UnityEngine;

public enum RoomType { Classroom, Lecture, Office, Exam, Ward, Surgery, Cell, Kitchen, Dining, Bedroom, Lounge, Bath, Workshop, Library, GroupRoom, Nursery, Storage, Pharmacy, Break, Archive, Vault, Garage }

/// <summary>Наполнение комнат мебелью и рабочими местами.</summary>
public partial class CityGenerator
{
    static readonly Color Board = new Color(0.1f, 0.28f, 0.2f);

    // ---------- планировка: вестибюль у входа + ряд комнат вдоль коридора ----------
    void RowsLayout(RoomRect box, float doorX, float lobbyD, RoomType[] types, string lobbyTitle)
    {
        float cz = box.z0 + lobbyD;
        int n = types.Length;
        float per = box.w / n;
        var doors = new float[n];
        for (int k = 0; k < n; k++) doors[k] = box.x0 + (k + 0.5f) * per;
        Part(true, cz, box.x0, box.x1, doors);
        for (int k = 1; k < n; k++) Part(false, box.x0 + k * per, cz, box.z1);
        for (int k = 0; k < n; k++)
            FillRoom(types[k], new RoomRect(box.x0 + k * per + 0.15f, cz + 0.15f, box.x0 + (k + 1) * per - 0.15f, box.z1 - 0.1f));
        Lobby(new RoomRect(box.x0, box.z0, box.x1, cz - 0.1f), doorX, lobbyTitle);
    }

    void Lobby(RoomRect r, float doorX, string title)
    {
        float side = (doorX - r.x0) > (r.x1 - doorX) ? -1f : 1f;
        if (title != null)
        {
            F(Wood, doorX + 6f * -side, r.z1 - 1.4f, 4.2f, 0.9f, 1.1f);
            F(Cream, doorX + 6f * -side, r.z1 - 1.4f, 4.4f, 1.0f, 0.06f, 1.1f, false);
            Station(title, doorX + 6f * -side, r.z1 - 2.4f);
            for (int k = 0; k < 3; k++) F(Fabric, doorX + 6f * side + (k - 1) * 2.2f, r.z0 + 1.0f, 1.8f, 0.55f, 0.45f);
        }
        Plant(r.x0 + 1f, r.z1 - 1f); Plant(r.x1 - 1f, r.z1 - 1f); Plant(r.x0 + 1f, r.z0 + 1f); Plant(r.x1 - 1f, r.z0 + 1f);
        F(Blue, doorX, r.cz, 3f, 2f, 0.02f, 0f, false); // коврик у входа
    }

    void FillRoom(RoomType t, RoomRect r)
    {
        switch (t)
        {
            case RoomType.Classroom: case RoomType.Lecture: Classroom(r); break;
            case RoomType.Office:
                for (float x = r.x0 + 1.4f; x < r.x1 - 1f; x += 2.6f) { DeskSet(x, r.z1 - 1.2f, false, "Рабочее место"); }
                Shelf(r.x1 - 0.5f, r.cz, 0.5f, 3f, 2f, Grey);
                Table(r.cx, r.cz - 0.5f, 2.2f, 1.1f, Wood); Chair(r.cx - 0.6f, r.cz - 1.3f, Fabric); Chair(r.cx + 0.6f, r.cz - 1.3f, Fabric);
                break;
            case RoomType.Exam:
                DeskSet(r.x0 + 1.6f, r.z1 - 1.2f, false, "Кабинет врача");
                Bed(r.x1 - 1.0f, r.cz + 0.6f, true, new Color(0.7f, 0.85f, 0.95f));
                F(White, r.x0 + 0.5f, r.cz, 0.5f, 1.8f, 1.9f); F(Grey, r.x0 + 1.4f, r.cz - 0.5f, 0.6f, 0.5f, 0.9f);
                break;
            case RoomType.Ward:
                for (float x = r.x0 + 1.2f; x < r.x1 - 0.8f; x += 2.4f) { Bed(x, r.z1 - 1.3f, true, new Color(0.7f, 0.85f, 0.95f)); F(White, x + 1.0f, r.z1 - 0.5f, 0.4f, 0.4f, 0.6f); }
                Chair(r.cx, r.cz, Blue); Station("Палата", r.cx, r.cz - 1f);
                break;
            case RoomType.Surgery:
                F(White, r.cx, r.cz + 0.5f, 2.2f, 0.9f, 0.9f); F(new Color(0.4f, 0.75f, 0.7f), r.cx, r.cz + 0.5f, 2.0f, 0.8f, 0.1f, 0.9f, false);
                F(new Color(1f, 1f, 0.9f), r.cx, r.cz + 0.5f, 1.2f, 1.2f, 0.12f, 2.6f, false);
                F(Grey, r.x1 - 0.8f, r.cz + 1f, 0.9f, 0.6f, 1f); F(Grey, r.x0 + 0.5f, r.z1 - 0.8f, 0.6f, 3f, 1.0f);
                Station("Операционная", r.cx, r.cz - 0.8f);
                break;
            case RoomType.Cell:
                Bed(r.x0 + 1.2f, r.z1 - 1.3f, true, Grey); Bed(r.x0 + 2.5f, r.z1 - 1.3f, true, Grey);
                F(White, r.x1 - 0.5f, r.z1 - 0.5f, 0.5f, 0.6f, 0.45f); F(White, r.x1 - 0.5f, r.z1 - 1.5f, 0.5f, 0.4f, 0.9f, 0f, false);
                break;
            case RoomType.Kitchen:
                F(Grey, r.cx, r.z1 - 0.4f, r.w - 1f, 0.7f, 0.9f); F(White, r.x0 + 0.6f, r.z1 - 1.5f, 0.7f, 0.7f, 1.8f); F(Dark, r.cx + 1f, r.z1 - 0.4f, 0.7f, 0.65f, 0.06f, 0.9f, false);
                Table(r.cx, r.cz - 0.4f, 1.8f, 1.0f, Wood); Chair(r.cx - 0.6f, r.cz - 1.3f, Wood); Chair(r.cx + 0.6f, r.cz - 1.3f, Wood);
                Station("Кухня", r.cx, r.z1 - 1.6f);
                break;
            case RoomType.Dining:
                for (float z = r.z0 + 3.5f; z < r.z1 - 1.5f; z += 2.4f)
                    for (float x = r.x0 + 1.6f; x < r.x1 - 1.2f; x += 3.0f) { Table(x, z, 1.6f, 0.9f, Cream); Chair(x - 0.5f, z - 0.7f, Wood); Chair(x + 0.5f, z - 0.7f, Wood); Chair(x - 0.5f, z + 0.7f, Wood); Chair(x + 0.5f, z + 0.7f, Wood); }
                F(Grey, r.cx, r.z1 - 0.5f, Mathf.Min(5f, r.w - 1f), 0.8f, 1.0f); Station("Раздача", r.cx, r.z1 - 1.5f);
                break;
            case RoomType.Bedroom:
                Bed(r.x1 - 1.3f, r.z1 - 1.6f, true, Fabric); F(Wood, r.x1 - 2.4f, r.z1 - 0.5f, 0.5f, 0.5f, 0.5f);
                F(Wood, r.x0 + 0.5f, r.cz + 0.5f, 0.6f, 1.8f, 2.1f);
                F(Green, r.cx, r.cz, 2.2f, 1.6f, 0.02f, 0f, false);
                break;
            case RoomType.Lounge:
                Sofa(r.cx, r.z1 - 1.0f, 2.4f, Fabric); Sofa(r.x0 + 1.0f, r.cz, 2.0f, Fabric, false);
                Table(r.cx, r.cz + 0.3f, 1.3f, 0.7f, Wood, 0.45f);
                F(Dark, r.cx, r.z0 + 0.3f, 1.4f, 0.08f, 0.8f, 0.9f, false); F(Wood, r.cx, r.z0 + 0.4f, 1.6f, 0.4f, 0.6f);
                Station("Гостиная", r.cx, r.cz - 0.6f);
                break;
            case RoomType.Bath:
                F(White, r.x1 - 0.5f, r.z1 - 1.2f, 0.75f, 1.7f, 0.6f); F(White, r.x0 + 0.4f, r.z1 - 0.5f, 0.45f, 0.6f, 0.4f); F(White, r.x0 + 1.4f, r.z1 - 0.3f, 0.6f, 0.4f, 0.85f);
                break;
            case RoomType.Workshop:
                for (float x = r.x0 + 1.5f; x < r.x1 - 1f; x += 3.2f) { F(Wood, x, r.z1 - 1.2f, 2.0f, 0.8f, 0.9f); F(Grey, x + 0.6f, r.z1 - 1.2f, 0.5f, 0.5f, 0.4f, 0.9f, false); Station("Верстак", x, r.z1 - 2.2f); }
                F(new Color(0.8f, 0.5f, 0.1f), r.x0 + 1.2f, r.cz, 1.4f, 1.2f, 1.6f); F(Grey, r.x1 - 1.4f, r.cz, 1.6f, 1.3f, 1.3f);
                break;
            case RoomType.Library:
                for (float x = r.x0 + 1.2f; x < r.x1 - 1f; x += 3f) Shelf(x, r.z1 - 1.3f, 2.4f, 0.5f, 2.1f, Wood);
                Table(r.cx, r.cz - 0.5f, 2.4f, 1.1f, Wood); Chair(r.cx - 0.7f, r.cz - 1.4f, Fabric); Chair(r.cx + 0.7f, r.cz - 1.4f, Fabric);
                Station("Библиотека", r.cx, r.z1 - 2.4f);
                break;
            case RoomType.GroupRoom:
                F(new Color(0.9f, 0.4f, 0.4f), r.cx, r.cz, r.w * 0.6f, r.d * 0.5f, 0.02f, 0f, false);
                for (int k = 0; k < 3; k++) { Table(r.x0 + 1.4f + k * 2.0f, r.cz + 1f, 1.0f, 0.8f, Yellow, 0.5f); Chair(r.x0 + 1.4f + k * 2.0f, r.cz + 0.2f, Red); }
                F(Blue, r.x1 - 0.5f, r.z1 - 0.5f, 0.6f, 0.6f, 0.6f); F(Yellow, r.x1 - 1.3f, r.z1 - 0.5f, 0.6f, 0.6f, 0.4f); F(Red, r.x1 - 2.1f, r.z1 - 0.5f, 0.6f, 0.6f, 0.8f);
                Shelf(r.x0 + 0.4f, r.z1 - 1.5f, 0.4f, 2.5f, 1.2f, Wood);
                Station("Группа", r.cx, r.cz - 0.8f);
                break;
            case RoomType.Nursery:
                for (float x = r.x0 + 0.9f; x < r.x1 - 0.6f; x += 1.5f) { Bed(x, r.z1 - 1.1f, true, new Color(0.9f, 0.7f, 0.8f)); }
                for (float x = r.x0 + 0.9f; x < r.x1 - 0.6f; x += 1.5f) { Bed(x, r.cz - 0.3f, true, new Color(0.7f, 0.85f, 0.95f)); }
                Station("Спальня", r.cx, r.cz - 1.8f);
                break;
            case RoomType.Storage:
                for (int k = 0; k < 6; k++) F(new Color(0.55f, 0.4f, 0.22f), r.x0 + 1f + (k % 3) * 1.1f, r.z1 - 1f - (k / 3) * 1.1f, 1.0f, 1.0f, 1.0f + (k % 2) * 0.8f);
                break;
            case RoomType.Pharmacy:
                F(Wood, r.cx, r.cz - 1f, Mathf.Min(4f, r.w - 1f), 0.8f, 1.1f); Shelf(r.cx, r.z1 - 0.5f, Mathf.Min(4.5f, r.w - 1f), 0.5f, 2.1f, White); Station("Аптека", r.cx, r.cz - 0.2f);
                break;
            case RoomType.Break:
                Table(r.cx, r.cz, 1.6f, 0.9f, Cream); Chair(r.cx - 0.5f, r.cz - 0.7f, Red); Chair(r.cx + 0.5f, r.cz - 0.7f, Red);
                F(Red, r.x1 - 0.6f, r.z1 - 0.6f, 0.8f, 0.8f, 1.8f); F(Grey, r.x0 + 0.6f, r.z1 - 0.6f, 0.9f, 0.6f, 0.9f);
                break;
            case RoomType.Archive:
                for (float x = r.x0 + 1.2f; x < r.x1 - 0.8f; x += 2.2f) Shelf(x, r.cz + 0.5f, 0.6f, 4f, 2.1f, Grey);
                break;
            case RoomType.Vault:
                F(new Color(0.3f, 0.3f, 0.32f), r.cx, r.z1 - 1.0f, 2.4f, 1.4f, 2.0f); F(Yellow, r.cx, r.z1 - 1.0f, 1.0f, 1.5f, 0.15f, 1.0f, false);
                Station("Хранилище", r.cx, r.z1 - 2.5f);
                break;
            case RoomType.Garage:
                F(Red, r.cx, r.z1 - 4.5f, 3.5f, 8f, 3.0f); F(White, r.cx, r.z1 - 4.5f, 3.6f, 3.0f, 0.5f, 1.2f, false); Station("Пожарная машина", r.cx, r.cz - 2f);
                break;
        }
    }

    void Classroom(RoomRect r)
    {
        F(Board, r.cx, r.z1 - 0.12f, Mathf.Min(5f, r.w - 1f), 0.08f, 1.2f, 1.0f, false);
        F(Wood, r.cx, r.z1 - 1.6f, 1.6f, 0.8f, 0.75f); Chair(r.cx, r.z1 - 2.3f, Dark);
        Station("Стол учителя", r.cx, r.z1 - 2.5f);
        for (float z = r.z1 - 3.6f; z > r.z0 + 2.6f; z -= 1.4f)
            for (float x = r.x0 + 1.2f; x < r.x1 - 0.8f; x += 1.5f) { F(Wood, x, z, 1.0f, 0.5f, 0.75f); Chair(x, z - 0.55f, Blue); }
        Station("Парта", r.x0 + 1.2f, r.z1 - 4.2f);
    }
}
