using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour {
    public GameObject roomPrefab;
    public int MaxRooms = 16;
    public int MaxRoomPerLevel = 4;
    public int InitMaxX = 1;
    public int InitMaxY = 1;

    Dungeon dungeon;
    GameObject dungeonGameObject;

	// Use this for initialization
	void Start () {
        GenerateDungeon();
	}

    void GenerateDungeon()
    {
        dungeon = new Dungeon(MaxRoomPerLevel, MaxRooms, InitMaxX, InitMaxY);
        dungeon.Generate();
        RenderDungeon();
    }

    void RenderDungeon()
    {
        ResetRender();

        var rooms = dungeon.GetRooms();
        foreach (var room in rooms)
        {
            Vector2 pos = room.getPos();
            GameObject currentRoom = Instantiate(roomPrefab, new Vector3(pos.x, 0, pos.y), Quaternion.identity, dungeonGameObject.transform);

            RenderGround(room, currentRoom);

            RenderKeyLevel(room, currentRoom);            

            RenderDoor(room, currentRoom, Direction.North);
            RenderDoor(room, currentRoom, Direction.East);
            RenderDoor(room, currentRoom, Direction.South);
            RenderDoor(room, currentRoom, Direction.West);
        }

    }

    #region Debug Render Func
    private void ResetRender()
    {
        if (dungeonGameObject)
        {
            GameObject.DestroyImmediate(dungeonGameObject.gameObject);
            dungeonGameObject = null;
        }

        dungeonGameObject = new GameObject("Dungeon");
    }

    private void RenderDoor(Room room, GameObject unityRoom, Direction direction)
    {
        Transform door = null;
        Edge edge = dungeon.IsLinkedToDirection(room, direction);
        if (edge != null)
        {
            door = unityRoom.transform.Find(direction.ToString() + "Door");
            if (door != null)
            {
                Renderer doorRend = door.GetComponent<Renderer>();
                if (doorRend != null)
                    doorRend.material.color = edge.GetKeyLevel() > -1 ? Color.black : Color.grey;
            }
        }
    }

    private void RenderGround(Room room, GameObject unityRoom)
    {
        Transform ground = unityRoom.transform.Find("Ground");
        if (!ground)
            return;

        Renderer rend = ground.GetComponent<Renderer>();
        if (!rend)
            return;

        switch (room.GetType())
        {
            case RoomType.START:
                rend.material.color = Color.green;
                break;
            case RoomType.END:
                rend.material.color = Color.red;
                break;
            case RoomType.BOSS:
                rend.material.color = Color.blue;
                break;
            case RoomType.NORMAL:
                break;
            default:
                break;
        }
    }

    private void RenderKeyLevel(Room room, GameObject unityRoom)
    {
        TextMesh keyLevel = unityRoom.GetComponentInChildren<TextMesh>();
        if (!keyLevel)
            return;

        keyLevel.text = room.GetKeyLevel().ToString();
    } 
    #endregion

    // Update is called once per frame
    void Update () {
        if (Input.GetButtonDown("Jump"))
            GenerateDungeon();

	}
}
