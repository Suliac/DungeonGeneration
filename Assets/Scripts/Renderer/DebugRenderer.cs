using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DebugRenderer", menuName = "DungeonGenerator/Renderer/Debug", order = 1)]
public class DebugRenderer : IRenderer
{
    public GameObject roomPrefab;
    public string IntensityObjectName = "Intensity";
    public string KeyLevelObjectName = "KeyLevel";

    private TextMesh keyLevel;
    private TextMesh intensity;
    
    protected override void ResetRender()
    {
        if (dungeonGameObject)
        {
            GameObject.DestroyImmediate(dungeonGameObject.gameObject);
            dungeonGameObject = null;
        }

        dungeonGameObject = new GameObject("Dungeon");
    }

    protected override void RenderDoor(Room dungeonRoom, Direction direction)
    {
        Transform door = null;
        Edge edge = dungeon.IsLinkedToDirection(dungeonRoom, direction);
        if (edge != null)
        {
            door = currentRoom.transform.Find(direction.ToString() + "Door");
            if (door != null)
            {
                Renderer doorRend = door.GetComponent<Renderer>();
                if (doorRend != null)
                    doorRend.material.color = edge.GetKeyLevel() > -1 ? Color.black : Color.grey;
            }
        }
    }

    protected override void RenderGround(Room dungeonRoom)
    {
        Transform ground = currentRoom.transform.Find("Ground");
        if (!ground)
            return;

        Renderer rend = ground.GetComponent<Renderer>();
        if (!rend)
            return;



        switch (dungeonRoom.GetRoomType())
        {
            case RoomType.START:
                rend.material.color = Color.green;
                break;
            case RoomType.END:
                rend.material.color = Color.magenta;
                break;
            case RoomType.BOSS:
                rend.material.color = Color.red;
                break;
            case RoomType.NORMAL:
                rend.material.color = new Color(dungeonRoom.GetIntensity(), 0.0f, 0.0f, 1.0f);
                break;
            default:
                break;
        }
    }

    protected override void RenderKeyLevel(Room dungeonRoom)
    {
        if (!keyLevel)
            return;

        keyLevel.text = dungeonRoom.GetKeyLevel().ToString();
    }

    protected override void RenderRoom(Room dungeonRoom)
    {
        Vector2 pos = dungeonRoom.getPos();
        currentRoom = Instantiate(roomPrefab, new Vector3(pos.x, 0, pos.y), Quaternion.identity, dungeonGameObject.transform);

        // Maj des texts de débug
        TextMesh[] texts = currentRoom.GetComponentsInChildren<TextMesh>();
        if (texts != null)
        {
            intensity = texts.FirstOrDefault(t => t.name == IntensityObjectName);
            keyLevel = texts.FirstOrDefault(t => t.name == KeyLevelObjectName);
        }
    }

    protected override void RenderIntensity(Room dungeonRoom)
    {
        if (!intensity)
            return;

        intensity.text = dungeonRoom.GetIntensity().ToString("n2");
    }

}
