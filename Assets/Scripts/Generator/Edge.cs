using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    North,
    East,
    South,
    West
}

public class Edge
{
    private string roomId; // id de la pièce vers laquelle, l'edge "pointe"
    private int keyLevelNeeded;

    public Edge(string targetRoomId)
    {
        roomId = targetRoomId;
        keyLevelNeeded = -1;
    }

    public Edge(string targetRoomId, int keyLevelToEnter)
    {
        roomId = targetRoomId;
        keyLevelNeeded = keyLevelToEnter;
    }

    public string GetRoomId()
    {
        return roomId;
    }

    public void SetKeyLevel(int keyLevel)
    {
        keyLevelNeeded = keyLevel;
    }
    

}
