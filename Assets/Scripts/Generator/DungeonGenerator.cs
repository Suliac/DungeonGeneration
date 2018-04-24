using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private bool loadAllInGrammarFolder;
    [SerializeField] private GrammarPattern[] patterns;

    private Dungeon dungeon;
    
	// Use this for initialization
	void Start () {
        if(loadAllInGrammarFolder)
            patterns = Resources.LoadAll<GrammarPattern>("Grammar");

        GenerateDungeon();
	}
    void Update()
    {
        if (Input.GetButtonDown("Jump"))
            GenerateDungeon();
    }

    void GenerateDungeon()
    {
        dungeon = new Dungeon(MaxRoomPerLevel, MaxRooms, RoomGridContentWidth, RoomGridContentHeight, patterns, InitMaxX, InitMaxY, NewEdgeProbability);
        dungeon.Generate();

        if (dungeonRenderer)
        {
            dungeonRenderer.Init(dungeon);
            dungeonRenderer.Render(); 
        }
    }

}
