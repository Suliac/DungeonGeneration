using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ContentType
{
    Anything = 0,
    ToDefine = 1,
    Empty = 2,
    Block = 3,
    Enemy = 4,
    Bonus = 5
}

/// <summary>
/// RoomContent Description
/// </summary>
public class RoomContent
{
    private string id;
    private ContentType myContent;
    private Vector2 position;
    private List<RoomContentEdge> edges;

    public RoomContent(Vector2 pos)
    {
        myContent = ContentType.ToDefine;
        position = pos;

        id = GetIdFromPos(pos);
        edges = new List<RoomContentEdge>();
    }

    public static string GetIdFromPos(Vector2 pos)
    {
        return pos.x + "," + pos.y; // L'idée c'est d'avoir un string qui est en lien avec les coordonnées pour pouvoir tester un id non présent actuellemnt dans le dj
    }

    public string GetId()
    {
        return id;
    }

    public void SetContentType(ContentType value)
    {
        myContent = value;
    }

    public ContentType GetContentType()
    {
        return myContent;
    }

    public Vector2 GetPos()
    {
        return position;
    }

    public void Link(RoomContent other)
    {
        RoomContentEdge edge = edges.FirstOrDefault(e => e.GetTargetContentId() == other.id);
        if (edge == null)
        {
            edge = new RoomContentEdge(other.id);
            edges.Add(edge);
        }
    }
}
