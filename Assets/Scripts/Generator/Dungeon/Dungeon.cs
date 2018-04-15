using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Dungeon
{
    private List<DungeonRoom> allRooms;
    private Dictionary<int, List<DungeonRoom>> roomPerKeyLevel;
    private int currentKeyLevel;
    private DungeonRoom startingRoom;

    private int DungeonMaxRoomsPerKeyLevel = 4;
    private int DungeonMaxRooms = 16; // NB MAX_ROOM < MAX_X*MAX_Y pour ne pas avoir que des donjons finaux carrés (qui remplisse tout l'espace dispo)
    private int DungeonInitMaxX = 1;
    private int DungeonInitMaxY = 1;
    private float DungeonGraphLinkProbability = 0.3f;    
    private int DungeonRoomContentGridWidth = 5;
    private int DungeonRoomContentGridHeight = 5;
    private GrammarPattern[] dungeonPatterns;

    public Dungeon(int maxRoomsPerLevel, int maxRooms, int roomContentWidth, int roomContentHeight, GrammarPattern[] patternsToApply, int firstRoomMaxX = 1, int firstRoomMaxY = 1, float newEdgeProbability = 0.3f)
    {
        allRooms = new List<DungeonRoom>();
        roomPerKeyLevel = new Dictionary<int, List<DungeonRoom>>();
        currentKeyLevel = 0;
        startingRoom = null;

        DungeonMaxRoomsPerKeyLevel = maxRoomsPerLevel;
        DungeonMaxRooms = maxRooms;
        DungeonInitMaxX = firstRoomMaxX;
        DungeonInitMaxY = firstRoomMaxY;
        DungeonGraphLinkProbability = newEdgeProbability;
        DungeonRoomContentGridHeight = roomContentHeight;
        DungeonRoomContentGridWidth = roomContentWidth;
        dungeonPatterns = patternsToApply;
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
        Graphify();

        // 5 - Gestion de l'intensité d'une room inspiré de la thèse : https://smartech.gatech.edu/bitstream/handle/1853/16823/ashmore-thesis.pdf (en simplifiant)
        SetAllRoomsIntensity();

        // 6 - Placement des clés -> on met les clés là où l'intensité est la plus forte à une niveau
        SetAllKeys();

        // 7 - Génération du contenu des salles
        GenerateRoomContent();
    }

    #region Core Func
    /// <summary>
    /// Place la première <see cref="DungeonRoom"/> à une position aléatoire, et l'ajoute au <see cref="Dungeon"/> avec le <see cref="RoomType"/> START
    /// </summary>
    private void PlaceFirstRoom()
    {
        int startX = Random.Range(0, DungeonInitMaxX);
        int startY = Random.Range(0, DungeonInitMaxY);

        startingRoom = new DungeonRoom(new Vector2(startX, startY), 0, RoomType.START);

        AddRoomToDungeon(startingRoom);
        AddRoomToDungeonForKeyLevel(startingRoom, 0);
    }

    /// <summary>
    /// Place toutes les <see cref="DungeonRoom"/> (à partir de la deuxième) dans le <see cref="Dungeon"/> en créant les <see cref="DungeonEdge"/> nécessaires
    /// </summary>
    private void PlaceAllRooms()
    {
        currentKeyLevel = 0;

        // Tant qu'on a pas le nombre max de pièces
        while (allRooms.Count < DungeonMaxRooms)
        {
            bool needToLockDoorWithChildren = false;

            // On récupère une room avec des côté dispo à notre "niveau" de clé

            DungeonRoom parentRoom = GetRandomRoomWithFreeEdges(roomPerKeyLevel[currentKeyLevel]);
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
            DungeonRoom childRoom = new DungeonRoom(childPos, currentKeyLevel);

            // On link les salles enfants et parents entre elles
            LinkParentAndChild(parentRoom, childRoom, needToLockDoorWithChildren ? currentKeyLevel : -1);

            // On ajoute la nouvelle salle au donjon
            AddRoomToDungeonForKeyLevel(childRoom, currentKeyLevel);
            AddRoomToDungeon(childRoom);
        }
    }

    /// <summary>
    /// Place la <see cref="DungeonRoom"/> de fin et la <see cref="DungeonRoom"/> du boss
    /// </summary>
    private void PlaceGoalAndEndRooms()
    {
        // On souhaite que notre fin de jeu se situe à un bout de la map  mais on souhaite aussi que notre salle de boss
        // ne soit accessible que par 1 entrée et donne seulement sur la salle de fin
        // On va donc assigner une feuille random de l'arbre à la salle de boss, puis ajouter à la suite dans une direction random la salle de fin

        IncreaseKeyLevel();

        // 1 - Récupération de la salle de boss
        List<DungeonRoom> leafs = roomPerKeyLevel[currentKeyLevel-1].Where(r => r.GetChildrens().Count == 0).ToList();
        if (leafs.Count == 0)
            throw new System.Exception("Aucune feuille disponible");

        leafs.Shuffle();
        DungeonRoom bossRoom = GetRandomRoomWithFreeEdges(leafs);

        if (bossRoom == null)
            throw new System.Exception("Impossible de récupèrer une room de boss");

        bossRoom.SetType(RoomType.BOSS);

        // /!\ Attention pour être sur que le joueur ait récupéré toutes les clés, on met ensuite la salle de boss et de fin au plus haut keylevel /!\
        ChangeKeyLevel(bossRoom, currentKeyLevel);
        DungeonRoom bossParentRoom = bossRoom.GetParent();

        // maj des edges
        bossRoom.Link(bossParentRoom, currentKeyLevel); // il y aura forcément un blocage
        bossParentRoom.Link(bossRoom, currentKeyLevel);

        // 2 - Ajout de la salle finale à la suite de celle du boss
        Vector2 endPos = GetRandomPosAvailableForRoom(bossRoom.getPos());
        DungeonRoom endRoom = new DungeonRoom(endPos, currentKeyLevel, RoomType.END);

        // On link les salles enfants et parents entre elles
        bossRoom.Link(endRoom, -1); // Pas de blocage entre la salle de boss et de fin
        endRoom.Link(bossRoom, -1);

        endRoom.SetParent(bossRoom);
        bossRoom.AddChild(endRoom);

        // On ajoute la nouvelle salle au donjon
        AddRoomToDungeonForKeyLevel(endRoom, currentKeyLevel);
        AddRoomToDungeon(endRoom);

    }

    /// <summary>
    /// Parcours l'arbre des <see cref="DungeonRoom"/> récusrivement pour évaluer le niveau d'intensité des pièces
    /// </summary>
    private void SetAllRoomsIntensity()
    {
        // parcours récursif de l'arbre pour mettre une première version de l'intensité
        float maxDungeonIntensity = SetRoomIntensityByKeyLevel();

        // Gestion de l'intensité de la salle de boss et de fin
        maxDungeonIntensity = SetBossAndEndRoomIntensity(maxDungeonIntensity);

        // On passe nos intensités entre 0.0f et 1.0f
        NormalizeRoomsIntensity(maxDungeonIntensity);
    }

    /// <summary>
    /// Récupère une <see cref="DungeonRoom"/> par niveau pour lui mettre une clé, en évitant de mettre la clé devant la porte si possible
    /// </summary>
    private void SetAllKeys()
    {
        for (int i = 0; i < currentKeyLevel; i++) // Pas touche aux boss/fin
        {
            var rooms = GetRoomsWithTheMostIntensityForLevel(i).ToArray();
            if (rooms.Count() <= 0)
                throw new System.Exception("Impossible de récupérer une place pour la clé pour un niveau");


            if (rooms.Count() > 1)
            {
                bool keyPlaceFound = false;
                foreach (var room in rooms)
                {
                    // si la piece n'est pas collé à une porte vérouillée, on y met une clé
                    if (!room.GetChildrens().Any(child => child.GetKeyLevel() != room.GetKeyLevel()) && !keyPlaceFound)
                    {
                        room.SetHasKey(true);
                        keyPlaceFound = true;
                    }
                }
            }
            else // si == 1
                rooms[0].SetHasKey(true);
        }
    }

    /// <summary>
    /// Ajoute des liens entre des salles de même niveau / avec un écart d'1 niveau sans toucher à la fin & boss
    /// </summary>
    private void Graphify()
    {
        foreach (var room in allRooms)
        {
            if (room.IsBossOrEnd()) // On veut garder qu'1 passage vers le boss
                continue;

            List<DungeonRoom> adjacentsRooms = GetAdjacentRooms(room.GetId()).Where(r => !r.IsBossOrEnd()).ToList(); // On veut garder qu'1 passage vers le boss
            foreach (var adjRoom in adjacentsRooms)
            {
                if (room.IsLinkedTo(adjRoom) == null) // Pas besoin de refaire de liens pour des salles déjà connectées
                {
                    // On ne veut ajouter des portes que si les salles sont de même key level
                    int currentKL = room.GetKeyLevel();
                    int adjKL = adjRoom.GetKeyLevel();
                    int diffLevelRoot = (currentKL - adjKL) * (currentKL - adjKL);

                    if ((diffLevelRoot == 0) && Random.Range(0.0f, 1.0f) <= DungeonGraphLinkProbability)
                        LinkParentAndChild(room, adjRoom, -1, true);
                }
            }
        }
    }

    private void GenerateRoomContent()
    {
        foreach (var room in allRooms)
            room.GenerateContent(DungeonRoomContentGridWidth, DungeonRoomContentGridHeight, dungeonPatterns);
    }

    #endregion

    #region Utility Func

    #region Room getters
    /// <summary>
    /// Récupère toutes les <see cref="DungeonRoom"/> du <see cref="Dungeon"/>
    /// </summary>
    /// <returns>Toute les <see cref="DungeonRoom"/> du <see cref="Dungeon"/></returns>
    public List<DungeonRoom> GetRooms()
    {
        return allRooms;
    }

    /// <summary>
    /// Récupère la <see cref="DungeonRoom"/> dans le <see cref="Dungeon"/> depuis la position d'une de départ avec une <see cref="Direction"/> donnée
    /// </summary>
    /// <param name="parentPos">Position de départ</param>
    /// <param name="direction">La direction dans laquelle on veut la prochaine room</param>
    /// <returns>Retourne une <see cref="DungeonRoom"/> si existante dans le donjon, sinon <see cref="null"/></returns>
    public DungeonRoom GetRoom(Vector2 parentPos, Direction direction)
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

        return GetRoom(DungeonRoom.GetIdFromPos(newPos));
    }

    /// <summary>
    /// Récupère une <see cref="DungeonRoom"/> par son id (concaténation de ses coordonnées en <see cref="string"/>
    /// </summary>
    /// <param name="id">Identifiant de la <see cref="DungeonRoom"/> recherchée</param>
    /// <returns>Retourne une <see cref="DungeonRoom"/> si existante dans le <see cref="Dungeon"/>, sinon <see cref="null"/></returns>
    public DungeonRoom GetRoom(string id)
    {
        return allRooms.FirstOrDefault(r => r.GetId() == id);
    }

    /// <summary>
    /// Récupère la première <see cref="DungeonRoom"/> d'un niveau : celle qui a un parent dont le niveau de clé est différent (ou qui n'a pas de parent)
    /// </summary>
    /// <param name="keyLevel">Niveau de clé pour lequel on souhaite récupérer la premiere <see cref="DungeonRoom"/></param>
    /// <returns>la première <see cref="DungeonRoom"/> d'un niveau ou <see cref="null"/> si niveau inexistant</returns>
    private DungeonRoom GetRoom(int keyLevel)
    {
        if (!roomPerKeyLevel.ContainsKey(keyLevel))
            return null;

        return roomPerKeyLevel[keyLevel].FirstOrDefault(room => room.GetParent() == null || room.GetParent().GetKeyLevel() != room.GetKeyLevel());
    }

    /// <summary>
    /// Récupère une <see cref="DungeonRoom"/> aléatoirement dans une <see cref="List{Room}"/> donnée, qui possède des <see cref="DungeonEdge"/> libres
    /// </summary>
    /// <param name="rooms">Liste de <see cref="DungeonRoom"/> parmis lesquelles il faut trouver celle qui n'est pas complétement entourée</param>
    /// <returns>Une <see cref="DungeonRoom"/> aléatoire qui n'est pas complétement entourée</returns>
    private DungeonRoom GetRandomRoomWithFreeEdges(List<DungeonRoom> rooms)
    {
        rooms.Shuffle();

        foreach (var room in rooms)
        {
            if (IsAnyFreeEdge(room))
                return room;
        }

        return null;
    }

    private IEnumerable<DungeonRoom> GetRoomsWithTheMostIntensityForLevel(int keyLevel)
    {
        float maxIntensityLevel = roomPerKeyLevel[keyLevel].Max(r => r.GetIntensity());
        return roomPerKeyLevel[keyLevel].Where(r => r.GetIntensity() == maxIntensityLevel);
    }
    #endregion

    /// <summary>
    /// Permet de savoir si une <see cref="DungeonRoom"/> possède encore des <see cref="DungeonEdge"/> libres
    /// </summary>
    /// <param name="room">La <see cref="DungeonRoom"/> qu'on veut tester</param>
    /// <returns><see cref="true"/> si la <see cref="DungeonRoom"/> possède des <see cref="DungeonEdge"/> libres, sinon <see cref="false"/></returns>
    private bool IsAnyFreeEdge(DungeonRoom room)
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
    /// <param name="posParent">La position de la <see cref="DungeonRoom"/> parent</param>
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
            roomPerKeyLevel.Add(currentKeyLevel, new List<DungeonRoom>());
    }

    /// <summary>
    /// Change une <see cref="DungeonRoom"/> d'un KeyLevel à un autre
    /// </summary>
    /// <param name="roomToUpdate">La pièce à update</param>
    /// <param name="newKeyLevel">Le nouveau niveau de la pièece</param>
    private void ChangeKeyLevel(DungeonRoom roomToUpdate, int newKeyLevel)
    {
        int oldKeyLevel = roomToUpdate.GetKeyLevel();
        if (oldKeyLevel != newKeyLevel)
        {
            if (!roomPerKeyLevel.ContainsKey(oldKeyLevel))
                throw new System.Exception("Erreur avec le vieux niveau de clé");

            DungeonRoom roomToDelete = roomPerKeyLevel[oldKeyLevel].FirstOrDefault(r => r.GetRoomType() == RoomType.BOSS);
            roomPerKeyLevel[oldKeyLevel].Remove(roomToDelete);

            roomToUpdate.SetKeyLevel(currentKeyLevel);
            if (!roomPerKeyLevel.ContainsKey(currentKeyLevel))
                throw new System.Exception("Erreur avec le nouveau niveau de clé");

            roomPerKeyLevel[newKeyLevel].Add(roomToUpdate);
        }
    }

    /// <summary>
    /// Ajoute la <see cref="DungeonRoom"/> au <see cref="Dungeon"/>
    /// </summary>
    /// <param name="room"></param>
    private void AddRoomToDungeon(DungeonRoom room)
    {
        allRooms.Add(room);
    }

    /// <summary>
    /// Ajoute la <see cref="DungeonRoom"/> à la liste des <see cref="DungeonRoom"/> par Key-Level
    /// </summary>
    /// <param name="room">la <see cref="DungeonRoom"/> que l'on souhaite ajouter</param>
    /// <param name="keyLevel">Le Key-Level auquel on souhaite ajouter notre <see cref="DungeonRoom"/></param>
    private void AddRoomToDungeonForKeyLevel(DungeonRoom room, int keyLevel)
    {
        if (!roomPerKeyLevel.ContainsKey(keyLevel))
            roomPerKeyLevel.Add(keyLevel, new List<DungeonRoom>());

        if (roomPerKeyLevel[keyLevel] != null)
            roomPerKeyLevel[keyLevel].Add(room);
        else
            roomPerKeyLevel[keyLevel] = new List<DungeonRoom> { room };
    }

    /// <summary>
    /// Récupère les positions (Attention PAS les <see cref="DungeonRoom"/>) disponibles (non remplies) autours d'une position
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

    private List<DungeonRoom> GetAdjacentRooms(string roomId)
    {
        List<DungeonRoom> adjacent = new List<DungeonRoom>();

        DungeonRoom currentRoom = GetRoom(roomId);
        if (currentRoom != null)
        {
            Vector2 roomPos = currentRoom.getPos();
            DungeonRoom north = GetRoom(roomPos, Direction.North);
            DungeonRoom east = GetRoom(roomPos, Direction.East);
            DungeonRoom south = GetRoom(roomPos, Direction.South);
            DungeonRoom west = GetRoom(roomPos, Direction.West);

            if (north != null)
                adjacent.Add(north);
            if (east != null)
                adjacent.Add(east);
            if (south != null)
                adjacent.Add(south);
            if (west != null)
                adjacent.Add(west);

        }

        return adjacent;
    }

    /// <summary>
    /// Fonction pour savoir si la position dans le donjon est libre
    /// </summary>
    /// <param name="pos">La position à laquelle on souhaite tester la présence de la <see cref="DungeonRoom"/></param>
    /// <returns>Retourne <see cref="true"/> s'il n'y a pas de <see cref="DungeonRoom"/> et que la position est libre, sinon retourne <see cref="false"/></returns>
    private bool IsRoomAtPosIsAvailable(Vector2 pos)
    {
        string idWanted = DungeonRoom.GetIdFromPos(pos);
        if (allRooms.Any(r => r.GetId() == idWanted))
            return false;
        else
            return true;
    }

    /// <summary>
    /// Vérifie s'il y a un lien (<see cref="DungeonEdge"/>) avec une <see cref="DungeonRoom"/> dans la <see cref="Direction"/> donnée depuis la <see cref="DungeonRoom"/> parent
    /// </summary>
    /// <param name="parentRoom">La <see cref="DungeonRoom"/> depuis laquelle on cherche</param>
    /// <param name="direction">La direction dans laquelle on veut tester le lien</param>
    /// <returns>Retourne le lien (<see cref="DungeonEdge"/>) si existant, sinon <see cref="null"/></returns>
    public DungeonEdge IsLinkedToDirection(DungeonRoom parentRoom, Direction direction)
    {
        DungeonRoom roomToDir = GetRoom(parentRoom.getPos(), direction);

        if (roomToDir == null)
            return null;

        return parentRoom.IsLinkedTo(roomToDir);
    }

    /// <summary>
    /// On précise l'intensité de la salle à partir de la dernière intensité (où de l'intensité max du dernier niveau si nouveau level) et on fait la même chose pour ses enfants
    /// Attention, seules les <see cref="DungeonRoom"/> d'un même niveau de clés sont ajoutés
    /// </summary>
    /// <param name="currentRoom">La pièce que l'on est ent rain de modifier</param>
    /// <param name="lastIntensity">L'intensité de la pièce parente</param>
    private float SetRoomIntensity(string currentRoomId, float lastIntensity)
    {
        // NB : On a un probleme quand la feuille d'un niveau n'est pas la dernière pièce d'un étage
        // Si on ne fait pas attention, nous pourrions passer à l'étage suivant avec comme base d'intensité un valeur
        // qui n'est pas liée à toute les pièces de l'étage, il faut donc r'envoyer une valeur si on arrive au bout 
        // d'une branche mais que toutes les pièces de l'étages n'ont pas été parcourues

        // De plus maintenant que le calcul d'intensité est fait sur le graph et polus sur un arbre il faut faire
        // attention aux dépendances cycliques

        DungeonRoom currentRoom = GetRoom(currentRoomId);
        if (currentRoom == null)
            throw new System.Exception("Impossible de trouver la room à partir de son ID");

        List<DungeonRoom> childrens = currentRoom.GetChildrens();
        int currentKeyLevel = currentRoom.GetKeyLevel();

        List<DungeonRoom> childrensAlreadyComputed = childrens.Where(c => c.GetIntensity() > 0 && c.GetRoomType() != RoomType.START).ToList();
        // Il faut prendre en compte les dépendances cycliques, si un enfant a déjà été calculé, il ne sera pas recalculé et la pièece acutelle
        // doit avoir une intensité en fonction de la plus petite aux alentours
        if (childrensAlreadyComputed != null && childrensAlreadyComputed.Count > 0) 
        {
            float minChildrenIntensity = childrensAlreadyComputed.Min(room => room.GetIntensity());
            lastIntensity = Mathf.Min(lastIntensity, minChildrenIntensity);
        }

        float intensity = lastIntensity + 1;
        float maxIntensity = intensity;

        // On met à jour l'intensité
        currentRoom.SetIntensity(intensity);

        // On parcourt les enfant de même KeyLevel pour update leur intensité
        if (childrens != null)
        {
            // On ne gère que l'intensité du même étage pour être sûr que toutes les pièces ont été gérées avant de passer à l'étage suivant
            // Si on a un enfant dont l'intensité est déjà calculée, on ne le recalcule pas sauf s'il a besoin d'etre corrigé
            foreach (var roomChild in childrens.Where(c => c.GetKeyLevel() == currentKeyLevel && ((c.GetIntensity() > 0 && intensity+1 < c.GetIntensity()) || c.GetIntensity() <= 0) && c.GetRoomType() != RoomType.START)) 
            {
                float childMaxIntensity = SetRoomIntensity(roomChild.GetId(), intensity);
                maxIntensity = Mathf.Max(maxIntensity, childMaxIntensity); // Si plusieurs enfants, l'intensité max est la plus grande des enfants
            }
        }

        return maxIntensity;
    }

    /// <summary>
    /// Précise l'intensité des <see cref="DungeonRoom"/> par niveau de clé
    /// </summary>
    /// <returns>L'intensité max du donjon</returns>
    private float SetRoomIntensityByKeyLevel()
    {
        float nextLevelIntensity = 0.0f;

        for (int i = 0; i < currentKeyLevel; i++)
        {
            float currentIntensity = i > 0 ? nextLevelIntensity * 0.75f : 0.0f;
            currentIntensity--; // On prévoit le intensity+1 du SetRoomIntensity

            DungeonRoom firstRoomOfKeyLevel = GetRoom(i); // On récupère la premère room du niveau
            if (firstRoomOfKeyLevel == null)
                throw new System.Exception("Impossible de récupérer la première pièce d'un niveau");

            float maxLevelIntensity = SetRoomIntensity(firstRoomOfKeyLevel.GetId(), currentIntensity);
            nextLevelIntensity = Mathf.Max(nextLevelIntensity, maxLevelIntensity);
        }

        return nextLevelIntensity; // a la fin nextLevelIntensity = l'intensité max
    }

    /// <summary>
    /// On veut que l'intensité de la salle de boss soit le plus forte possible et que celle de fin soit nulle
    /// </summary>
    /// <param name="maxIntensity">L'intensité maximale trouvée dans le donjon jusqu'ici</param>
    /// <returns>L'intensité de la salle du boss</returns>
    private float SetBossAndEndRoomIntensity(float maxIntensity)
    {
        // Gestion de l'intensité de la salle de boss
        DungeonRoom bossRoom = allRooms.FirstOrDefault(r => r.GetRoomType() == RoomType.BOSS);
        if (bossRoom == null)
            throw new System.Exception("Impossible de trouver une salle de boss");

        float bossRoomIntensity = maxIntensity + 1.0f;
        bossRoom.SetIntensity(bossRoomIntensity);

        // Gestion de l'intensité de la salle de fin, la salle de fin est forcément le seul enfant de la salle de boss, sinon il y a une erreur
        DungeonRoom endRoom = bossRoom.GetChildrens().FirstOrDefault();
        if (endRoom == null)
            throw new System.Exception("Impossible de trouver une salle de fin liée à la salle de boss");

        endRoom.SetIntensity(0.0f);

        return bossRoomIntensity;
    }

    /// <summary>
    ///  On passe toutes nos intensités entre 0.0f et 1.0f pour faciliter l'implémentation du donjon
    /// </summary>
    /// <param name="maxIntensity">L'intensité maximale trouvée dans le donjon (le boss)</param>
    private void NormalizeRoomsIntensity(float maxIntensity)
    {
        foreach (var room in allRooms) // On s'en fiche de l'ordre ici puisqu'on compare l'intensité de la pièece par rapport à l'intensité max
            room.SetIntensity(room.GetIntensity() / maxIntensity);

    }

    private Vector2 GetPosInDir(Vector2 initPos, Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
                return new Vector2(initPos.x, initPos.y + 1);
            case Direction.East:
                return new Vector2(initPos.x + 1, initPos.y);
            case Direction.South:
                return new Vector2(initPos.x, initPos.y - 1);
            case Direction.West:
                return new Vector2(initPos.x - 1, initPos.y);
            default:
                return new Vector2(initPos.x, initPos.y + 1);
        }
    }

    /// <summary>
    /// Ajoute des <see cref="DungeonEdge"/> entre les deux rooms, par défaut on change le parent de l'enfant mais on peut le désactiver
    /// NB : on veut le désactiver lors qu'on graphifie notre arbre pour s'assurer d'avoir toujours un point d'entrée unique par level
    /// </summary>
    /// <param name="parentRoom">La pièce parente</param>
    /// <param name="childRoom">La pièce enfant</param>
    /// <param name="keyLevel">Le niveau de clé</param>
    /// <param name="onlyChildrensRelation">si <see cref="true"/> seules les listes des rooms enfants sont mise à jour</param>
    private void LinkParentAndChild(DungeonRoom parentRoom, DungeonRoom childRoom, int keyLevel, bool onlyChildrensRelation = false)
    {
        // On link les salles enfants et parents entre elles
        parentRoom.Link(childRoom, keyLevel); // Si le niveau de clé a changé, le lien devient conditionnel
        childRoom.Link(parentRoom, keyLevel);

        if (!onlyChildrensRelation)
            childRoom.SetParent(parentRoom);
        else
            childRoom.AddChild(parentRoom);

        parentRoom.AddChild(childRoom);
    }
    
    #endregion
}
