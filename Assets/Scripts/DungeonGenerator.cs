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
        if(dungeonGameObject)
        {
            GameObject.DestroyImmediate(dungeonGameObject.gameObject);
            dungeonGameObject = null;
        }

        dungeonGameObject = new GameObject("Dungeon");

        var rooms = dungeon.GetRooms();
        foreach (var room in rooms)
        {
            Vector2 pos = room.getPos();

            GameObject currentRoom = Instantiate(roomPrefab, new Vector3(pos.x, 0, pos.y), Quaternion.identity, dungeonGameObject.transform);
            Transform ground = currentRoom.transform.Find("Ground");
            if (!ground)
                continue;

            Renderer rend = ground.GetComponent<Renderer>();
            if (!rend)
                continue;

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

            TextMesh keyLevel = currentRoom.GetComponentInChildren<TextMesh>();
            if (!keyLevel)
                continue;

            keyLevel.text = room.GetKeyLevel().ToString();

            Transform door = null;
            if(dungeon.IsLinkedToDirection(room, Direction.North))
            {
                door = currentRoom.transform.Find("NorthDoor");
                if(door != null)
                {
                    Renderer doorRend = door.GetComponent<Renderer>();
                    if(doorRend != null)
                        doorRend.material.color = Color.black;
                }
            }

            door = null;
            if (dungeon.IsLinkedToDirection(room, Direction.East))
            {
                door = currentRoom.transform.Find("EastDoor");
                if (door != null)
                {
                    Renderer doorRend = door.GetComponent<Renderer>();
                    if (doorRend != null)
                        doorRend.material.color = Color.black;
                }
            }

            door = null;
            if (dungeon.IsLinkedToDirection(room, Direction.South))
            {
                door = currentRoom.transform.Find("SouthDoor");
                if (door != null)
                {
                    Renderer doorRend = door.GetComponent<Renderer>();
                    if (doorRend != null)
                        doorRend.material.color = Color.black;
                }
            }

            door = null;
            if (dungeon.IsLinkedToDirection(room, Direction.West))
            {
                door = currentRoom.transform.Find("WestDoor");
                if (door != null)
                {
                    Renderer doorRend = door.GetComponent<Renderer>();
                    if (doorRend != null)
                        doorRend.material.color = Color.black;
                }
            }
        }

    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetButtonDown("Jump"))
            GenerateDungeon();

	}
}
