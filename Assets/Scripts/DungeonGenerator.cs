using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour {
    public int MaxRooms = 16;
    public int MaxRoomPerLevel = 4;
    public int InitMaxX = 1;
    public int InitMaxY = 1;

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
        dungeon = new Dungeon(MaxRoomPerLevel, MaxRooms, InitMaxX, InitMaxY);
        dungeon.Generate();

        dungeonRenderer.Init(dungeon);
        dungeonRenderer.Render();
    }

}
