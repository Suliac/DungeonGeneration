﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DebugRenderer", menuName = "DungeonGenerator/Renderer/Debug", order = 1)]
public class DebugRenderer : IRenderer
{
    [SerializeField] private GameObject roomPrefab;

    [SerializeField] private GameObject toDefineContentPrefab;
    [SerializeField] private GameObject emptyContentPrefab;
    [SerializeField] private GameObject blockContentPrefab;
    [SerializeField] private GameObject enemyContentPrefab;
    [SerializeField] private GameObject bonusContentPrefab;

    [SerializeField] private string intensityObjectName = "Intensity";
    [SerializeField] private string keyLevelObjectName = "KeyLevel";
    [SerializeField] private string contentsObjectName = "Contents";
    [SerializeField] private int roomWidth = 1;
    [SerializeField] public bool displayRoomsDetails = true;

    [SerializeField] private Color startRoomColor = Color.green;
    [SerializeField] private Color endRoomColor = Color.magenta;
    [SerializeField] private Color bossRoomColor = Color.blue;
    [SerializeField] private Color keyRoomColor = Color.yellow;
    [SerializeField] private Color normalRoomColorLowIntensity = Color.white;
    [SerializeField] private Color normalRoomColorHighIntensity = Color.red;

    private float wallWidth = 0.1f;
    private TextMesh keyLevel;
    private TextMesh intensity;
    private Transform roomContents;

    protected override void ResetRender()
    {
        if (dungeonGameObject)
        {
            GameObject.DestroyImmediate(dungeonGameObject.gameObject);
            dungeonGameObject = null;
        }

        dungeonGameObject = new GameObject("Dungeon");
    }

    protected override void RenderDoor(DungeonRoom dungeonRoom, Direction direction)
    {
        Transform door = null;
        DungeonEdge edge = dungeon.IsLinkedToDirection(dungeonRoom, direction);
        if (edge != null)
        {
            door = currentRoom.transform.Find(direction.ToString() + "Door");
            if (door != null)
            {
                Renderer doorRend = door.GetComponent<Renderer>();
                if (doorRend != null)
                    doorRend.material.color = edge.GetKeyLevel() > -1 ? edge.GetKeyLevel() == 1000 ? Color.red : Color.black : Color.grey;
            }
        }
    }

    protected override void RenderGround(DungeonRoom dungeonRoom)
    {
        List<Renderer> renderers = new List<Renderer>();

        Transform ground = currentRoom.transform.Find("Ground");
        if (!ground)
            return;

        Renderer rend = ground.GetComponent<Renderer>();
        if (rend)
            renderers.Add(rend);


        // On donne la couleur aux murs
        Transform walls = currentRoom.transform.Find("Walls");
        if (walls)
        {
            Renderer[] rends = walls.GetComponentsInChildren<Renderer>();
            if (rends != null)
                renderers.AddRange(rends);
        }

        // On donne la couleur aux portes (changé ensuite si besoin)
        for (int i = 0; i < 4; i++)
        {
            Transform door = currentRoom.transform.Find(((Direction)i).ToString() + "Door");
            if(door)
            {
                Renderer doorRend = door.GetComponent<Renderer>();
                if (doorRend)
                    renderers.Add(doorRend);
            }
        }

        Color colorToApply = startRoomColor;
        switch (dungeonRoom.GetRoomType())
        {
            case RoomType.START:
                colorToApply = startRoomColor;
                break;
            case RoomType.END:
                colorToApply = endRoomColor;
                break;
            case RoomType.BOSS:
                colorToApply = bossRoomColor;
                break;
            case RoomType.NORMAL:
                colorToApply = Color.Lerp(normalRoomColorLowIntensity, normalRoomColorHighIntensity, dungeonRoom.GetIntensity());
                break;
            case RoomType.KEY:
                colorToApply = keyRoomColor;
                break;
            default:
                break;
        }

        if (renderers != null)
            foreach (var render in renderers)
                render.material.color = colorToApply;
    }

    protected override void RenderKeyLevel(DungeonRoom dungeonRoom)
    {
        if (!keyLevel)
            return;

        keyLevel.text = dungeonRoom.GetKeyLevel().ToString();
    }

    protected override void RenderRoom(DungeonRoom dungeonRoom)
    {
        Vector2 pos = dungeonRoom.getPos();
        currentRoom = Instantiate(roomPrefab, new Vector3(pos.x * roomWidth, 0, pos.y * roomWidth), Quaternion.identity, dungeonGameObject.transform);

        // Maj des texts de débug
        TextMesh[] texts = currentRoom.GetComponentsInChildren<TextMesh>();
        if (texts != null)
        {
            intensity = texts.FirstOrDefault(t => t.name == intensityObjectName);
            keyLevel = texts.FirstOrDefault(t => t.name == keyLevelObjectName);
        }

        if (displayRoomsDetails)
        {
            roomContents = currentRoom.transform.Find(contentsObjectName);

            var roomContentHolder = dungeonRoom.GetContent();
            var contents = roomContentHolder.GetAllContents();

            foreach (var content in contents)
            {
                GameObject toInstantiate = null;

                switch (content.GetContentType())
                {
                    case ContentType.ToDefine:
                        toInstantiate = toDefineContentPrefab;
                        break;
                    case ContentType.Empty:
                        toInstantiate = emptyContentPrefab;
                        break;
                    case ContentType.Block:
                        toInstantiate = blockContentPrefab;
                        break;
                    case ContentType.Enemy:
                        toInstantiate = enemyContentPrefab;
                        break;
                    case ContentType.Bonus:
                        toInstantiate = bonusContentPrefab;
                        break;
                    default:
                        break;
                }

                float contentWidth = 0.16f;
                float contentHeight = 0.16f;
                Vector2 contentPos = content.GetPos();

                if (toInstantiate)
                {
                    Instantiate(toInstantiate,
                        new Vector3(wallWidth + pos.x * roomWidth + contentPos.x * contentWidth, 0.2f, wallWidth + pos.y * roomWidth + contentPos.y * contentHeight),
                        Quaternion.identity,
                        roomContents ? roomContents : currentRoom.transform);
                }

            }
        }
    }

    protected override void RenderIntensity(DungeonRoom dungeonRoom)
    {
        if (!intensity)
            return;

        intensity.text = dungeonRoom.GetIntensity().ToString("n2");
    }

}
