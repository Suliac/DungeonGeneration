using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Dungeon
{
    private List<Room> allRooms;
    private Dictionary<int, List<Room>> roomPerKeyLevel;
    private int currentKeyLevel;

    private int DungeonMaxRoomsPerKeyLevel = 4;
    private int DungeonMaxRooms = 16; // NB MAX_ROOM < MAX_X*MAX_Y pour ne pas avoir que des donjons finaux carrés (qui remplisse tout l'espace dispo)
    private int DungeonInitMaxX = 1;
    private int DungeonInitMaxY = 1;

    public Dungeon(int maxRoomsPerLevel, int maxRooms, int firstRoomMaxX = 1, int firstRoomMaxY = 1)
    {
        allRooms = new List<Room>();
        roomPerKeyLevel = new Dictionary<int, List<Room>>();
        currentKeyLevel = 0;

        DungeonMaxRoomsPerKeyLevel = maxRoomsPerLevel;
        DungeonMaxRooms = maxRooms;
        DungeonInitMaxX = firstRoomMaxX;
        DungeonInitMaxY = firstRoomMaxY;
    }

    public void Generate()
    {
        // 1 - Création de la room de départ
        PlaceFirstRoom();

        // 2 - Placement des autres salles
        PlaceAllRooms();

        // 3 - Placement de la fin  et du boss

        // 4 - Transforme l'arbe en graph -> ajoute d'autre liaisons entre les salles (si même niveau de clé)

        // 5 - Placement des clés
    }

    private void PlaceFirstRoom()
    {
        int startX = Random.Range(0, DungeonInitMaxX);
        int startY = Random.Range(0, DungeonInitMaxY);

        Room startingRoom = new Room(new Vector2(startX, startY), 0, RoomType.START);

        AddRoomToDungeon(startingRoom);
        AddRoomToDungeonForKeyLevel(startingRoom, 0);
    }

    private void PlaceAllRooms()
    {
        currentKeyLevel = 0;

        // Tant qu'on a pas le nombre max de pièces
        while (allRooms.Count < DungeonMaxRooms)
        {
            bool needToLockDoorWithChildren = false;

            // On récupère une room avec des côté dispo à notre "niveau" de clé

            Room parentRoom = GetRandomRoomWithFreeEdges(roomPerKeyLevel[currentKeyLevel]);
            if (parentRoom == null)
            {
                // Si aucune possibilité de trouver un parent libre dans le niveau de clé actuel
                // on récupère un parent au hasard dispo dans toute la map et on incrémente le niveau de clé

                parentRoom = GetRandomRoomWithFreeEdges(allRooms); // Si aucune room dispo

                IncreaseKeyLevel();
                needToLockDoorWithChildren = true; // La prochaine room sera d'un autre keylevel
            }

            if (parentRoom == null)
                throw new System.Exception("Impossible de récupérer une room parent");

            // Une fois notre prochain parent récupéré, on vérifie si on veut passer au prochain niveau de clé (si le nombre de piece par niveau de clé est rempli)
            if (!needToLockDoorWithChildren && ShouldIncreaseKeyLevel())
            {
                IncreaseKeyLevel();
                needToLockDoorWithChildren = true; // La prochaine room sera d'un autre keylevel
            }

            // on récupère une direction random parmis les dispo et on créer la room enfant
            Vector2 childPos = GetRandomPosAvailableForRoom(parentRoom.getPos());
            Room childRoom = new Room(childPos, currentKeyLevel);

            // On link les salles enfants et parents entre elles
            parentRoom.Link(childRoom, needToLockDoorWithChildren ? currentKeyLevel : -1); // Si le niveau de clé a changé, le lien devient conditionnel
            childRoom.Link(parentRoom, needToLockDoorWithChildren ? currentKeyLevel : -1);

            childRoom.SetParent(parentRoom);
            parentRoom.AddChild(childRoom);

            // On ajoute la nouvelle salle au donjon
            AddRoomToDungeonForKeyLevel(childRoom, currentKeyLevel);
            AddRoomToDungeon(childRoom);
        }
    }


    /////////////////////////////////////////////////////////////
    public List<Room> GetRooms()
    {
        return allRooms;
    }

    private Room GetRandomRoomWithFreeEdges(List<Room> rooms)
    {
        rooms.Shuffle();

        foreach (var room in rooms)
        {
            var posAvailable = GetAdjacentAvailableRoomsPos(room.getPos(), currentKeyLevel);
            if (posAvailable.Count > 0)
                return room;
        }

        return null;
    }

    private Vector2 GetRandomPosAvailableForRoom(Vector2 posParent)
    {
        var posAvailable = GetAdjacentAvailableRoomsPos(posParent, currentKeyLevel);
        if (posAvailable.Count > 0)
        {
            int randomIndex = Random.Range(0, posAvailable.Count);
            return posAvailable[randomIndex];
        }

        throw new System.Exception("Impossible de trouver une direction disponible pour la room");
    }

    private bool ShouldIncreaseKeyLevel()
    {
        if (!roomPerKeyLevel.ContainsKey(currentKeyLevel))
            throw new System.Exception("La liste des rooms par niveau de clé ne contient pas de clé : " + currentKeyLevel.ToString());

        return roomPerKeyLevel[currentKeyLevel].Count > DungeonMaxRoomsPerKeyLevel;
    }

    private void IncreaseKeyLevel()
    {
        currentKeyLevel++;
        if (!roomPerKeyLevel.ContainsKey(currentKeyLevel))
            roomPerKeyLevel.Add(currentKeyLevel, new List<Room>());
    }

    private void AddRoomToDungeon(Room room)
    {
        allRooms.Add(room);
    }

    private void AddRoomToDungeonForKeyLevel(Room room, int keyLevel)
    {
        if (!roomPerKeyLevel.ContainsKey(keyLevel))
            roomPerKeyLevel.Add(keyLevel, new List<Room>());

        if (roomPerKeyLevel[keyLevel] != null)
            roomPerKeyLevel[keyLevel].Add(room);
        else
            roomPerKeyLevel[keyLevel] = new List<Room> { room };
    }

    private List<Vector2> GetAdjacentAvailableRoomsPos(Vector2 pos, int keyLevel)
    {
        List<Vector2> allPosAvailable = new List<Vector2>();

        // Test au nord
        Vector2 northPos = new Vector2(pos.x, pos.y + 1);
        if (IsRoomAtPosIsAvailable(northPos))
            allPosAvailable.Add(northPos);

        // Test au sud
        Vector2 southPos = new Vector2(pos.x, pos.y - 1);
        if (IsRoomAtPosIsAvailable(southPos))
            allPosAvailable.Add(southPos);

        // Test au est
        Vector2 eastPos = new Vector2(pos.x + 1, pos.y);
        if (IsRoomAtPosIsAvailable(eastPos))
            allPosAvailable.Add(eastPos);

        // Test au ouest
        Vector2 westPos = new Vector2(pos.x - 1, pos.y);
        if (IsRoomAtPosIsAvailable(westPos))
            allPosAvailable.Add(westPos);

        return allPosAvailable;
    }

    private bool IsRoomAtPosIsAvailable(Vector2 pos)
    {
        string idWanted = Room.GetIdFromPos(pos);
        if (allRooms.Any(r => r.GetId() == idWanted))
            return false;
        else
            return true;
    }

    /// <summary>
    /// Récupère la <see cref="Room"/> dans le donjon depuis la position d'une de départ avec une <see cref="Direction"/> donnée
    /// </summary>
    /// <param name="parentPos">Position de départ</param>
    /// <param name="direction">La direction dans laquelle on veut la prochaine room</param>
    /// <returns>Retourne une <see cref="Room"/> si existante dans le <see cref="Dungeon"/>, sinon <see cref="null"/></returns>
    public Room GetRoom(Vector2 parentPos, Direction direction)
    {
        Vector2 newPos = Vector2.zero;

        switch (direction)
        {
            case Direction.North:
                newPos = new Vector2(parentPos.x, parentPos.y + 1);
                break;
            case Direction.East:
                newPos = new Vector2(parentPos.x + 1, parentPos.y);
                break;
            case Direction.South:
                newPos = new Vector2(parentPos.x, parentPos.y + 1);
                break;
            case Direction.West:
                newPos = new Vector2(parentPos.x - 1, parentPos.y);
                break;
            default:
                break;
        }

        return GetRoom(Room.GetIdFromPos(newPos));
    }

    public bool IsLinkedToDirection(Room parentRoom, Direction direction)
    {
        Room roomToDir = roomToDir = GetRoom(parentRoom.getPos(), direction);

        if (roomToDir == null)
            return false;

        return parentRoom.IsLinkedTo(roomToDir);
    }

    /// <summary>
    /// Récupère une <see cref="Room"/> par son id (concaténation de ses coordonnées en <see cref="string"/>
    /// </summary>
    /// <param name="id">Identifiant de la <see cref="Room"/> recherchée</param>
    /// <returns>Retourne une <see cref="Room"/> si existante dans le <see cref="Dungeon"/>, sinon <see cref="null"/></returns>
    public Room GetRoom(string id)
    {
        return allRooms.FirstOrDefault(r => r.GetId() == id);
    }
}
