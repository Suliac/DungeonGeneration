using System;
using UnityEngine;

public abstract class IRenderer : ScriptableObject
{

    protected GameObject dungeonGameObject;
    protected Dungeon dungeon;
    protected GameObject currentRoom;

    public void Init(Dungeon currentDungeon)
    {
        dungeon = currentDungeon;
        SpecificInit();
    }

    public void Render()
    {
        if (dungeon == null)
            throw new Exception("Erreur, impossible de récupérer le donjon généré, avez vous pensé à lancer la fonction Init avant ?");

        ResetRender();
        if(!dungeonGameObject)
            throw new Exception("Erreur, impossible de récupérer dungeonGameObject");


        var rooms = dungeon.GetRooms();
        foreach (var room in rooms)
        {
            RenderRoom(room);

            RenderKeyLevel(room);

            RenderIntensity(room);

            RenderGround(room);

            RenderDoor(room, Direction.North);
            RenderDoor(room, Direction.East);
            RenderDoor(room, Direction.South);
            RenderDoor(room, Direction.West);
        }
    }
    
    abstract protected void ResetRender();

    abstract protected void RenderRoom(DungeonRoom dungeonRoom);

    abstract protected void RenderDoor(DungeonRoom dungeonRoom, Direction direction);

    abstract protected void RenderGround(DungeonRoom dungeonRoom);

    abstract protected void RenderKeyLevel(DungeonRoom dungeonRoom);

    abstract protected void RenderIntensity(DungeonRoom dungeonRoom);
    
    virtual protected void SpecificInit()
    {
        return;
    }
}
