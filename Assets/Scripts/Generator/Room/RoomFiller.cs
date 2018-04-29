using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// RoomFilled Description
/// </summary>
public class RoomFiller
{
    private int graphWidth;
    private int graphHeight;
    private DirectionFlag doorsPosition;
    private GrammarPattern[] patternsToApply;

    private RoomTypeFlags currentRoomType;
    private float currentRoomIntensity;

    /// <summary>
    /// Une room est cadrillée de <see cref="RoomContent"/>
    /// </summary>
    private List<RoomContent> allContents;

    public RoomFiller(int width, int height, DirectionFlag doors, GrammarPattern[] dungeonPatterns, RoomType roomType, float roomIntensity)
    {
        graphHeight = width;
        graphWidth = width;
        doorsPosition = doors;
        patternsToApply = dungeonPatterns;

        switch (roomType)
        {
            case RoomType.START:
                currentRoomType = RoomTypeFlags.START;
                break;
            case RoomType.END:
                currentRoomType = RoomTypeFlags.END;
                break;
            case RoomType.BOSS:
                currentRoomType = RoomTypeFlags.BOSS;
                break;
            case RoomType.NORMAL:
                currentRoomType = RoomTypeFlags.NORMAL;
                break;
            case RoomType.KEY:
                currentRoomType = RoomTypeFlags.KEY;
                break;
            default:
                break;
        }

        currentRoomIntensity = roomIntensity;
        allContents = new List<RoomContent>();
    }

    public int Generate()
    {
        // 1 - Générer un simili graph de graphWidth x graphHeight élements vides
        // Cela nous permet de nous assurer qu'on aura toujours un graph de même taille
        // Comme nous avons des rooms carrées / rectangles de même tailles c'est suffisant
        InitGraph();

        // 2 - On applique les patterns de structure de facon aléatoire mais en fonction de leur niveau 
        // Pour garder une cohérence de génération
        int patternApplied = ApplyPatterns();

        // 3 - On remplis les trous restants par du vide
        FillContentToDefine();

        return patternApplied;
    }

    private void InitGraph()
    {
        // Création du des élements vides
        CreateEmptyContentGraph();

        // Créations des liens entre les éléments pour former le graph
        LinkGraphContents();

        // On souhaite que les cases correspondants aux portes soient vides pour éviter qu'un joueur se retrouve nez à nez avec un monstre dès le début
        InitDoorsPositionContent();
    }

    /// <summary>
    /// Applique les patterns en fonction de leur level d'execution
    /// On essaie d'appliquer tous les pattern de niveau 0 puis de niveau 1 etc ...
    /// Cela permet de controler la cohérence, sans empêcher une génération chaotique horizontale si voulue (avec que des pattern de niveau 0)
    /// </summary>
    private int ApplyPatterns()
    {
        int nbPatternToApply = 5;
        int nbPatternApplied = 0;

        if (patternsToApply.Any())
        {
            int maxLevel = patternsToApply.Max(p => p.Level) + 1;
            for (int i = 0; i < maxLevel; i++)
            {
                int nbPatternAppliedPerLevel = 0;
                List<GrammarPattern> patternsList = patternsToApply.Where(p => p.Level == i && (p.ApplyToRoomOfType & currentRoomType) == currentRoomType).ToList();
                if (patternsList != null && patternsList.Any())
                {
                    // On veut que dans le "pire des cas" au moins chacun des patterns du niveau soit testé pour éviter d'avoir des "trous"
                    List<int> baseIndices = new List<int>();
                    for (int l = 0; l < patternsList.Count; l++)
                        baseIndices.Add(l);

                    // On ajoute d'autres indices pour ajouter au random
                    for (int k = 0; k < (patternsList.Count); k++)
                        baseIndices.Add(UnityEngine.Random.Range(0, patternsList.Count));

                    baseIndices.Shuffle();
                    Queue<int> patternIndices = new Queue<int>(baseIndices);

                    while (patternIndices.Count > 0 && ((nbPatternAppliedPerLevel < nbPatternToApply && i == maxLevel - 1) || (i != maxLevel - 1)))
                    {
                        int indice = patternIndices.Dequeue();
                        if (TryApplyPattern(patternsList[indice]))
                            nbPatternAppliedPerLevel++;
                    }
                }

                nbPatternToApply += nbPatternAppliedPerLevel;
            }
        }

        return nbPatternApplied;
    }

    private void FillContentToDefine()
    {
        if(allContents.Any(content => content.GetContentType() == ContentType.ToDefine))
        {
            var contentsToChange = allContents.Where(content => content.GetContentType() == ContentType.ToDefine).ToArray();
            foreach (var content in contentsToChange)
                content.SetContentType(ContentType.Empty);
        }
    }
    ///////////////////////////////////////////////////

    /// <summary>
    /// Créer un graph vide rectangle de graphWidth x graphHeight cases avec des coordonnées sur 2 dimensions
    /// </summary>
    private void CreateEmptyContentGraph()
    {
        for (int y = 0; y < graphHeight; y++)
            for (int x = 0; x < graphWidth; x++)
            {
                var newContent = new RoomContent(new Vector2(x, y));
                allContents.Add(newContent);
            }
    }

    /// <summary>
    /// Création des liens entre les noeuds adjacents du graph
    /// </summary>
    private void LinkGraphContents()
    {
        for (int i = 0; i < allContents.Count; i++)
        {
            // Comme on a ajouté les éléments en commencant par les X puis Y dans une liste,
            // On sait que pour trouver les voisins on peut suivre la règle :
            // La case adjacente à gauche est la case précédente du tableau => i - 1
            // La case adjacente à droite est la case précédente du tableau => i + 1
            // La case adjacente au dessus est la case du tableau           => i - graphWidth
            // La case adjacente à gauche est la case précédente du tableau => i + graphWidth
            if (i - 1 > 0)
                allContents[i].Link(allContents[i - 1]); // Gauche
            if (i + 1 < allContents.Count)
                allContents[i].Link(allContents[i + 1]); // Droite
            if (i - graphWidth > 0)
                allContents[i].Link(allContents[i - graphWidth]); // Haut
            if (i + graphWidth < allContents.Count)
                allContents[i].Link(allContents[i + graphWidth]); // Bas
        }
    }

    /// <summary>
    /// Comme on veut que les cases d'entrées/sorties de room soient vides, on le spécifie dès le début
    /// </summary>
    private void InitDoorsPositionContent()
    {
        if ((doorsPosition & DirectionFlag.North) == DirectionFlag.North)
        {
            Vector2 door = new Vector2((graphWidth / 2), graphHeight - 1);
            GetContent(door).SetContentType(ContentType.Empty);
        }

        if ((doorsPosition & DirectionFlag.South) == DirectionFlag.South)
        {
            Vector2 door = new Vector2((graphWidth / 2), 0);
            GetContent(door).SetContentType(ContentType.Empty);
        }

        if ((doorsPosition & DirectionFlag.East) == DirectionFlag.East)
        {
            Vector2 door = new Vector2(graphWidth - 1, (graphHeight / 2));
            GetContent(door).SetContentType(ContentType.Empty);
        }

        if ((doorsPosition & DirectionFlag.West) == DirectionFlag.West)
        {
            Vector2 door = new Vector2(0, (graphHeight / 2));
            GetContent(door).SetContentType(ContentType.Empty);
        }
    }

    private bool TryApplyPattern(GrammarPattern pattern, bool randomizePositions = true)
    {
        List<Vector2> pos = new List<Vector2>();
        for (int y = 0; y < graphHeight; y++)
            for (int x = 0; x < graphWidth; x++)
                pos.Add(new Vector2(x, y));

        if (randomizePositions)
            pos.Shuffle();

        foreach (var position in pos)
        {
            List<int> nbRotation = new List<int> { UnityEngine.Random.Range(0, 4) };
            if(pattern.ApplyAtAnyCost)
            {
                nbRotation = new List<int> { 0, 1, 2, 3 };
                nbRotation.Shuffle();
            }

            Queue<int> nbRotationQueue = new Queue<int>(nbRotation);
            do
            {
                if (TryApplyPattern(pattern, nbRotationQueue.Dequeue(), position))
                    return true;
            } while (nbRotationQueue.Count > 0);
        }

        return false;
    }

    private bool TryApplyPattern(GrammarPattern pattern, int nbRotation, Vector2 firstPosition)
    {
        List<ContentType> graphBeforeRotated = RotatePattern(pattern.GraphBefore, pattern.Width, nbRotation);
        List<ContentType> graphAfterRotated = RotatePattern(pattern.GraphAfter, pattern.Width, nbRotation);

        // On récupère toutes les cases qui sont couvertes par le pattern à partir de la position précisée
        List<RoomContent> equivalentToPatternGraph = allContents.Where(c => c.GetPos().x >= firstPosition.x
                                                                        && c.GetPos().y >= firstPosition.y
                                                                        && c.GetPos().x < firstPosition.x + pattern.Width
                                                                        && c.GetPos().y < firstPosition.y + pattern.Width).ToList();

        // Si la taille du pattern et la tailles des cases qu'il couvre  est différentes, alors c'est que l'on est dans un coin
        // -> on ne pourra pas appliquer le pattern
        if (equivalentToPatternGraph.Count != graphBeforeRotated.Count)
            return false;

        // On vérifie si le pattern 'avant' colle bien au pattern récupéré
        for (int i = 0; i < equivalentToPatternGraph.Count; i++)
        {
            if (graphBeforeRotated[i] == ContentType.Anything)
                continue;

            if (equivalentToPatternGraph[i].GetContentType() != graphBeforeRotated[i])
                return false;
        }

        // Si on arrive ici c'est qu'on peut appliquer le pattern
        for (int i = 0; i < equivalentToPatternGraph.Count; i++)
        {
            // Anything veut dire que l'on laisse la case dans son état, mais qu'on avait bien besoin d'une case (par un mur)
            if (graphAfterRotated[i] == ContentType.Anything)
                continue;

            equivalentToPatternGraph[i].SetContentType(graphAfterRotated[i]);
        }


        return true;
    }

    /// <summary>
    /// Fait une rotation à 90° dans le sens des aiguilles d'une montre
    /// </summary>
    /// <param name="graphPattern">La liste/graph des type de contenu du pattern</param>
    /// <param name="patternWidth">La largeur du pattern</param>
    /// <param name="nbRotation">Le nombre de rotation à 90° à faire</param>
    /// <returns></returns>
    private List<ContentType> RotatePattern(List<ContentType> graphPattern, int patternWidth, int nbRotation)
    {
        ContentType[] newGraph = new ContentType[graphPattern.Count];
        List<ContentType> baseGraph = graphPattern.ToList();
        for (int nb = 0; nb < nbRotation; nb++)
        {
            for (int i = 0; i < baseGraph.Count; i++)
            {
                int x = i % patternWidth;
                int y = i / patternWidth;

                int newX = patternWidth - (y + 1);
                int newY = x;

                int newI = newX + patternWidth * newY;
                newGraph[newI] = baseGraph[i];
            }
            baseGraph = newGraph.ToList();
        }

        return baseGraph;
    }

    public RoomContent GetContent(string id)
    {
        return allContents.FirstOrDefault(c => c.GetId() == id);
    }

    public RoomContent GetContent(Vector2 pos)
    {
        string id = RoomContent.GetIdFromPos(pos);
        return allContents.FirstOrDefault(c => c.GetId() == id);
    }

    public List<RoomContent> GetAllContents()
    {
        return allContents;
    }

    public int GetGridWidth()
    {
        return graphWidth;
    }

    public int GetGridHeight()
    {
        return graphHeight;
    }
}
