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

public class Room
{

    private string id; // Identifiant de la room
    private Vector2 pos;
    private List<Edge> edges; // Les liens vers les autres rooms avec des particularités, p.e. : besoin d'une clés pour passer à telle pièce
    private List<Room> childrens;
    private Room parent;

    private int keyLevel;
    private bool hasKey;
    private RoomType type;

    public Room(Vector2 roomPos, int roomKeyLevel, RoomType roomType)
    {
        pos = roomPos;
        keyLevel = roomKeyLevel;
        hasKey = false; // par défaut aucune salle n'a de clé
        type = roomType;
        id = GetIdFromPos(pos);

        edges = new List<Edge>();
        childrens = new List<Room>();
    }

    public Room(Vector2 roomPos, int roomKeyLevel)
    {
        pos = roomPos;
        keyLevel = roomKeyLevel;
        hasKey = false; // par défaut aucune salle n'a de clé
        type = RoomType.NORMAL;
        id = GetIdFromPos(pos);

        edges = new List<Edge>();
        childrens = new List<Room>();
    }

    #region Accessors
    public static string GetIdFromPos(Vector2 pos)
    {
        return pos.x+","+pos.y; // L'idée c'est d'avoir un string qui est en lien avec les coordonnées pour pouvoir tester un id non présent actuellemnt dans le dj
    }

    public void Link(Room other, int keyLevel)
    {
        Edge edge = edges.FirstOrDefault(e => e.GetRoomId() == other.id);
        if (edge != null)
            edge.SetKeyLevel(keyLevel);
        else
        {
            edge = new Edge(other.id, keyLevel);
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

    public void AddChild(Room child)
    {
        childrens.Add(child);
    }

    public void SetParent(Room room)
    {
        parent = room;
    } 

    public RoomType GetType()
    {
        return type;
    }

    public int GetKeyLevel()
    {
        return keyLevel;
    }
    #endregion

    public bool IsLinkedTo(Room other)
    {
        return edges.Any(e => e.GetRoomId() == other.GetId());
    }
}
