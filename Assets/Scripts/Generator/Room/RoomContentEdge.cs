using UnityEngine;

/// <summary>
/// RoomEdge Description
/// </summary>
public class RoomContentEdge
{
    private string targetContentId;

    public RoomContentEdge(string targetId)
    {
        targetContentId = targetId;
    }

    public string GetTargetContentId()
    {
        return targetContentId;
    }

}
