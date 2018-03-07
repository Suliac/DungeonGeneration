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

    /// <summary>
    /// Génère toutes les salles dans le donjon avec une salle de début, de fin, de boss et des salles avec des clés
    /// </summary>
    public void Generate()
    {
        // 1 - Création de la room de départ
        PlaceFirstRoom();

        // 2 - Placement des autres salles
        PlaceAllRooms();

        // 3 - Placement de la fin  et du boss
        PlaceGoalAndEndRooms();

        // 4 - Transforme l'arbe en graph -> ajoute d'autre liaisons entre les salles (si même niveau de clé)

        // 5 - Placement des clés
    }

    /// <summary>
    /// Place la première <see cref="Room"/> à une position aléatoire, et l'ajoute au <see cref="Dungeon"/> avec le <see cref="RoomType"/> START
    /// </summary>
    private void PlaceFirstRoom()
    {
        int startX = Random.Range(0, DungeonInitMaxX);
        int startY = Random.Range(0, DungeonInitMaxY);

        Room startingRoom = new Room(new Vector2(startX, startY), 0, RoomType.START);

        AddRoomToDungeon(startingRoom);
        AddRoomToDungeonForKeyLevel(startingRoom, 0);
    }

    /// <summary>
    /// Place toutes les <see cref="Room"/> (à partir de la deuxième) dans le <see cref="Dungeon"/> en créant les <see cref="Edge"/> nécessaires
    /// </summary>
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
    /// <summary>
    /// Place la <see cref="Room"/> de fin et la <see cref="Room"/> du boss
    /// </summary>
    private void PlaceGoalAndEndRooms()
    {
        // On souhaite que notre fin de jeu se situe à un bout de la map  mais on souhaite aussi que notre salle de boss
        // ne soit accessible que par 1 entrée et donne seulement sur la salle de fin
        // On va donc assigner une feuille random de l'arbre à la salle de boss, puis ajouter à la suite dans une direction random la salle de fin

        IncreaseKeyLevel();

        // 1 - Récupération de la salle de boss
        List<Room> leafs = allRooms.Where(r => r.GetChildrens().Count == 0).ToList();
        if (leafs.Count == 0)
            throw new System.Exception("Aucune feuille disponible");

        leafs.Shuffle();
        Room bossRoom = GetRandomRoomWithFreeEdges(leafs);
        
        if (bossRoom == null)
            throw new System.Exception("Impossible de récupèrer une room de boss");

        bossRoom.SetType(RoomType.BOSS);

        // /!\ Attention pour être sur que le joueur ait récupéré toutes les clés, on met ensuite la salle de boss et de fin au plus au keylevel /!\
        bossRoom.SetKeyLevel(currentKeyLevel);
        Room bossParentRoom = bossRoom.GetParent();
        // maj des edges
        bossRoom.Link(bossParentRoom, currentKeyLevel); // il y aura forcément un blocage
        bossParentRoom.Link(bossRoom, currentKeyLevel);

        // 2 - Ajout de la salle finale à la suite de celle du boss
        Vector2 endPos = GetRandomPosAvailableForRoom(bossRoom.getPos());
        Room endRoom = new Room(endPos, currentKeyLevel, RoomType.END);

        // On link les salles enfants et parents entre elles
        bossRoom.Link(endRoom, -1); // Pas de blocage entre la salle de boss et de fin
        endRoom.Link(bossRoom, -1);

        endRoom.SetParent(bossRoom);
        bossRoom.AddChild(endRoom);

        // On ajoute la nouvelle salle au donjon
        AddRoomToDungeonForKeyLevel(endRoom, currentKeyLevel);
        AddRoomToDungeon(endRoom);
        
    }


    /////////////////////////////////////////////////////////////
    /// <summary>
    /// Récupère toutes les <see cref="Room"/> du <see cref="Dungeon"/>
    /// </summary>
    /// <returns>Toute les <see cref="Room"/> du <see cref="Dungeon"/></returns>
    public List<Room> GetRooms()
    {
        return allRooms;
    }

    /// <summary>
    /// Récupère une <see cref="Room"/> aléatoirement dans une <see cref="List{Room}"/> donnée, qui possède des <see cref="Edge"/> libres
    /// </summary>
    /// <param name="rooms">Liste de <see cref="Room"/> parmis lesquelles il faut trouver celle qui n'est pas complétement entourée</param>
    /// <returns>Une <see cref="Room"/> aléatoire qui n'est pas complétement entourée</returns>
    private Room GetRandomRoomWithFreeEdges(List<Room> rooms)
    {
        rooms.Shuffle();

        foreach (var room in rooms)
        {
            if(IsAnyFreeEdge(room))
                return room;
        }

        return null;
    }

    /// <summary>
    /// Permet de savoir si une <see cref="Room"/> possède encore des <see cref="Edge"/> libres
    /// </summary>
    /// <param name="room">La <see cref="Room"/> qu'on veut tester</param>
    /// <returns><see cref="true"/> si la <see cref="Room"/> possède des <see cref="Edge"/> libres, sinon <see cref="false"/></returns>
    private bool IsAnyFreeEdge(Room room)
    {
        var posAvailable = GetAdjacentAvailableRoomsPos(room.getPos());
        if (posAvailable.Count > 0)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Récupère une position aléatoire parmis les directions disponibles
    /// </summary>
    /// <param name="posParent">La position de la <see cref="Room"/> parent</param>
    /// <returns>Une position aléaoire disponible autour de la position du parent</returns>
    private Vector2 GetRandomPosAvailableForRoom(Vector2 posParent)
    {
        var posAvailable = GetAdjacentAvailableRoomsPos(posParent);
        if (posAvailable.Count > 0)
        {
            int randomIndex = Random.Range(0, posAvailable.Count);
            return posAvailable[randomIndex];
        }

        throw new System.Exception("Impossible de trouver une direction disponible pour la room");
    }

    /// <summary>
    /// Permet de savoir si on doit augmenter notre Key-Level
    /// </summary>
    /// <returns>Retourne <see cref="true"/> si on doit augmenter sinon <see cref="false"/></returns>
    private bool ShouldIncreaseKeyLevel()
    {
        if (!roomPerKeyLevel.ContainsKey(currentKeyLevel))
            throw new System.Exception("La liste des rooms par niveau de clé ne contient pas de clé : " + currentKeyLevel.ToString());

        return roomPerKeyLevel[currentKeyLevel].Count > DungeonMaxRoomsPerKeyLevel;
    }

    /// <summary>
    /// Ajoute 1 au Key-Level
    /// </summary>
    private void IncreaseKeyLevel()
    {
        currentKeyLevel++;
        if (!roomPerKeyLevel.ContainsKey(currentKeyLevel))
            roomPerKeyLevel.Add(currentKeyLevel, new List<Room>());
    }

    /// <summary>
    /// Ajoute la <see cref="Room"/> au <see cref="Dungeon"/>
    /// </summary>
    /// <param name="room"></param>
    private void AddRoomToDungeon(Room room)
    {
        allRooms.Add(room);
    }

    /// <summary>
    /// Ajoute la <see cref="Room"/> à la liste des <see cref="Room"/> par Key-Level
    /// </summary>
    /// <param name="room">la <see cref="Room"/> que l'on souhaite ajouter</param>
    /// <param name="keyLevel">Le Key-Level auquel on souhaite ajouter notre <see cref="Room"/></param>
    private void AddRoomToDungeonForKeyLevel(Room room, int keyLevel)
    {
        if (!roomPerKeyLevel.ContainsKey(keyLevel))
            roomPerKeyLevel.Add(keyLevel, new List<Room>());

        if (roomPerKeyLevel[keyLevel] != null)
            roomPerKeyLevel[keyLevel].Add(room);
        else
            roomPerKeyLevel[keyLevel] = new List<Room> { room };
    }

    /// <summary>
    /// Récupère les positions (Attention PAS les <see cref="Room"/>) disponibles (non remplies) autours d'une position
    /// </summary>
    /// <param name="pos">La position initiale pour laquelle on souhaite trouver les positions libres adjacentes</param>
    /// <returns>La liste des positions disponibles</returns>
    private List<Vector2> GetAdjacentAvailableRoomsPos(Vector2 pos)
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

    /// <summary>
    /// Fonction pour savoir si la position dans le donjon est libre
    /// </summary>
    /// <param name="pos">La position à laquelle on souhaite tester la présence de la <see cref="Room"/></param>
    /// <returns>Retourne <see cref="true"/> s'il n'y a pas de <see cref="Room"/> et que la position est libre, sinon retourne <see cref="false"/></returns>
    private bool IsRoomAtPosIsAvailable(Vector2 pos)
    {
        string idWanted = Room.GetIdFromPos(pos);
        if (allRooms.Any(r => r.GetId() == idWanted))
            return false;
        else
            return true;
    }
    
    /// <summary>
    /// Vérifie s'il y a un lien (<see cref="Edge"/>) avec une <see cref="Room"/> dans la <see cref="Direction"/> donnée depuis la <see cref="Room"/> parent
    /// </summary>
    /// <param name="parentRoom">La <see cref="Room"/> depuis laquelle on cherche</param>
    /// <param name="direction">La direction dans laquelle on veut tester le lien</param>
    /// <returns>Retourne le lien (<see cref="Edge"/>) si existant, sinon <see cref="null"/></returns>
    public Edge IsLinkedToDirection(Room parentRoom, Direction direction)
    {
        Room roomToDir = GetRoom(parentRoom.getPos(), direction);

        if (roomToDir == null)
            return null;

        return parentRoom.IsLinkedTo(roomToDir);
    }

    /// <summary>
    /// Récupère la <see cref="Room"/> dans le <see cref="Dungeon"/> depuis la position d'une de départ avec une <see cref="Direction"/> donnée
    /// </summary>
    /// <param name="parentPos">Position de départ</param>
    /// <param name="direction">La direction dans laquelle on veut la prochaine room</param>
    /// <returns>Retourne une <see cref="Room"/> si existante dans le donjon, sinon <see cref="null"/></returns>
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
                newPos = new Vector2(parentPos.x, parentPos.y - 1);
                break;
            case Direction.West:
                newPos = new Vector2(parentPos.x - 1, parentPos.y);
                break;
            default:
                break;
        }

        return GetRoom(Room.GetIdFromPos(newPos));
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
