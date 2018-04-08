using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoomType
{
    START,
    END,
    BOSS,
    NORMAL
}


public class DungeonRoom
{

    private string id; // Identifiant de la room
    private Vector2 pos;
    private List<DungeonEdge> edges; // Les liens vers les autres rooms avec des particularités, p.e. : besoin d'une clés pour passer à telle pièce
    private List<DungeonRoom> childrens;
    private DungeonRoom parent;
    private RoomFilled contentOfTheRoom;

    private float intensity;

    private int keyLevel;
    private bool hasKey;
    private RoomType type;

    public DungeonRoom(Vector2 roomPos, int roomKeyLevel, RoomType roomType)
    {
        pos = roomPos;
        keyLevel = roomKeyLevel;
        hasKey = false; // par défaut aucune salle n'a de clé
        type = roomType;
        id = GetIdFromPos(pos);

        edges = new List<DungeonEdge>();
        childrens = new List<DungeonRoom>();
    }

    public DungeonRoom(Vector2 roomPos, int roomKeyLevel)
    {
        pos = roomPos;
        keyLevel = roomKeyLevel;
        hasKey = false; // par défaut aucune salle n'a de clé
        type = RoomType.NORMAL;
        id = GetIdFromPos(pos);

        edges = new List<DungeonEdge>();
        childrens = new List<DungeonRoom>();
    }

    #region Accessors
    public static string GetIdFromPos(Vector2 pos)
    {
        return pos.x + "," + pos.y; // L'idée c'est d'avoir un string qui est en lien avec les coordonnées pour pouvoir tester un id non présent actuellemnt dans le dj
    }
    public static Vector2 GetPosFromId(string id)
    {
        string[] coord = id.Split(',');
        if (coord.Length != 2)
            throw new System.Exception("Mauvais ID");

        return new Vector2(int.Parse(coord[0]), int.Parse(coord[1])); // L'idée c'est d'avoir un string qui est en lien avec les coordonnées pour pouvoir tester un id non présent actuellemnt dans le dj
    }

    public void Link(DungeonRoom other, int keyLevel)
    {
        DungeonEdge edge = edges.FirstOrDefault(e => e.GetRoomId() == other.id);
        if (edge != null)
            edge.SetKeyLevel(keyLevel);
        else
        {
            edge = new DungeonEdge(other.id, keyLevel);
            edges.Add(edge);
        }
    }

    public string GetId()
    {
        return id;
    }

    public Vector2 getPos()
    {
        return pos;
    }

    public void AddChild(DungeonRoom child)
    {
        childrens.Add(child);
    }

    public List<DungeonRoom> GetChildrens()
    {
        return childrens;
    }

    public void SetParent(DungeonRoom room)
    {
        parent = room;
    }

    public DungeonRoom GetParent()
    {
        return parent;
    }

    public RoomType GetRoomType()
    {
        return type;
    }

    public void SetType(RoomType value)
    {
        type = value;
    }

    public int GetKeyLevel()
    {
        return keyLevel;
    }

    public void SetKeyLevel(int value)
    {
        keyLevel = value;
    }

    public DungeonEdge IsLinkedTo(DungeonRoom other)
    {
        return edges.FirstOrDefault(e => e.GetRoomId() == other.GetId());
    }

    public void SetIntensity(float value)
    {
        intensity = value;
    }

    public float GetIntensity()
    {
        return intensity;
    }

    public bool GetHasKey()
    {
        return hasKey;
    }

    public void SetHasKey(bool value)
    {
        hasKey = value;
    }

    public bool IsBossOrEnd()
    {
        return type == RoomType.BOSS || type == RoomType.END;
    }
    
    public RoomFilled GetContent()
    {
        return contentOfTheRoom;
    }
    #endregion

    public void GenerateContent(int width, int height)
    {
        DirectionFlag doorDirections = DirectionFlag.None;
        foreach (var edge in edges)
        {
            Vector2 adjPosition = GetPosFromId(edge.GetRoomId());

            if (pos.x == adjPosition.x && pos.y + 1 == adjPosition.y)
                doorDirections |= DirectionFlag.North;

            if (pos.x + 1 == adjPosition.x && pos.y == adjPosition.y)
                doorDirections |= DirectionFlag.East;

            if (pos.x == adjPosition.x && pos.y - 1 == adjPosition.y)
                doorDirections |= DirectionFlag.South;

            if (pos.x - 1 == adjPosition.x && pos.y == adjPosition.y)
                doorDirections |= DirectionFlag.West;
        }

        contentOfTheRoom = new RoomFilled(width, height, doorDirections);
        contentOfTheRoom.Generate();
    }

}
