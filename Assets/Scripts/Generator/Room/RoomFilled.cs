using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// RoomFilled Description
/// </summary>
public class RoomFilled
{
    private int graphWidth;
    private int graphHeight;
    private DirectionFlag doorsPosition;
    private GrammarPattern[] patternsToApply;

    /// <summary>
    /// Une room est cadrillée de <see cref="RoomContent"/>
    /// </summary>
    private List<RoomContent> allContents;

    public RoomFilled(int width, int height, DirectionFlag doors, GrammarPattern[] dungeonPatterns)
    {
        graphHeight = width;
        graphWidth = width;
        doorsPosition = doors;
        patternsToApply = dungeonPatterns;

        allContents = new List<RoomContent>();
    }

    public void Generate()
    {
        // 1 - Générer un simili graph de graphWidth x graphHeight élements vides
        // Cela nous permet de nous assurer qu'on aura toujours un graph de même taille
        // Comme nous avons des rooms carrées / rectangles de même tailles c'est suffisant
        InitGraph();

        // 2 - On applique les patterns de structure : càd les patterns qui sont fait avant tous les autres pour s'assurer 
        // qu'il y a toujours un chemin dispo pour aller à chacune des sorties
        if (!ApplyStructurePattern())
            Debug.LogError("Attention, aucun pattern de structure n'a pu être appliqué à une room");

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
    /// Applique 1 seul et unique patterne dit de 'structure' pour s'assurer qu'il y ait un chemin entre les portes
    /// </summary>
    /// <returns>Retroune <see cref="true"/> si un patern à pu être appliqué, sinon retourne <see cref="false"/></returns>
    private bool ApplyStructurePattern()
    {
        List<GrammarPattern> structurePatternsList = patternsToApply.Where(p => p.IsStructurePattern).ToList();
        structurePatternsList.Shuffle();

        Queue<GrammarPattern> structurePatternsQueue = new Queue<GrammarPattern>(structurePatternsList);
        bool patternApplied = false;

        if (structurePatternsQueue != null && structurePatternsQueue.Any())
        {
            GrammarPattern patternToApply = null;
            do
            {
                patternToApply = structurePatternsQueue.Dequeue();

                if (patternsToApply == null)
                    throw new Exception("Impossible de trouver un pattern à appliquer");

                patternApplied = TryApplyPattern(patternToApply);

            } while (!patternApplied && structurePatternsQueue.Count > 0); // Tant que l'on peut, on essaie d'appliquer un pattern
        }

        return patternApplied;
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
            List<int> nbRotation = new List<int> { 0, 1, 2, 3 };
            nbRotation.Shuffle();

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

        // On récupère l'index dans le tableau du apttern de la première position
        int positionInPatternTab = (int)firstPosition.x + (int)firstPosition.y * pattern.Width;

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
