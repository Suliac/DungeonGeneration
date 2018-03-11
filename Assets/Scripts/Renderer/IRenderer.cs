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
            throw new System.Exception("Erreur, impossible de récupérer le donjon généré, avez vous pensé à lancer la fonction Init avant ?");

        ResetRender();
        if(!dungeonGameObject)
            throw new System.Exception("Erreur, impossible de récupérer dungeonGameObject");


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

    abstract protected void RenderRoom(Room dungeonRoom);

    abstract protected void RenderDoor(Room dungeonRoom, Direction direction);

    abstract protected void RenderGround(Room dungeonRoom);

    abstract protected void RenderKeyLevel(Room dungeonRoom);

    abstract protected void RenderIntensity(Room dungeonRoom);

    virtual protected void SpecificInit()
    {
        return;
    }
}
