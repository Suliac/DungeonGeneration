using System;
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

[Flags] public enum DirectionFlag
{
    None = 0,
    North = 1,
    East = 2,
    South = 4,
    West = 8
}

public class DungeonEdge
{
    private string roomId; // id de la pièce vers laquelle, l'edge "pointe"
    private int keyLevelNeeded;

    public DungeonEdge(string targetRoomId)
    {
        roomId = targetRoomId;
        keyLevelNeeded = -1;
    }

    public DungeonEdge(string targetRoomId, int keyLevelToEnter)
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

    public int GetKeyLevel()
    {
        return keyLevelNeeded;
    }
    

}
