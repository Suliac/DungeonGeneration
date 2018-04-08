using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour {
    [SerializeField] private int MaxRooms = 16;
    [SerializeField] private int MaxRoomPerLevel = 4;
    [SerializeField] private int InitMaxX = 1;
    [SerializeField] private int InitMaxY = 1;
    [SerializeField] private int RoomGridContentWidth = 5;
    [SerializeField] private int RoomGridContentHeight = 5;
    [SerializeField, Range(0.0f, 1.0f)] private float NewEdgeProbability = 0.3f;
    [SerializeField] private IRenderer dungeonRenderer;

    private Dungeon dungeon;
    
	// Use this for initialization
	void Start () {
        GenerateDungeon();
	}
    void Update()
    {
        if (Input.GetButtonDown("Jump"))
            GenerateDungeon();
    }

    void GenerateDungeon()
    {
        dungeon = new Dungeon(MaxRoomPerLevel, MaxRooms, RoomGridContentWidth, RoomGridContentHeight, InitMaxX, InitMaxY, NewEdgeProbability);
        dungeon.Generate();

        dungeonRenderer.Init(dungeon);
        dungeonRenderer.Render();
    }

}
