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

    /// <summary>
    /// Une room est cadrillée de <see cref="RoomContent"/>
    /// </summary>
    private List<RoomContent> allContents;

    public RoomFilled(int width, int height, DirectionFlag doors)
    {
        graphHeight = height;
        graphWidth = width;
        doorsPosition = doors;

        allContents = new List<RoomContent>();
    }

    public void Generate()
    {
        // 1 - Générer un simili graph de graphWidth x graphHeight élements vides
        // Cela nous permet de nous assurer qu'on aura toujours un graph de même taille
        // Comme nous avons des rooms carrées / rectangles de même tailles c'est suffisant
        InitGraph();



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


    ///////////////////////////////////////////////////
    private void CreateEmptyContentGraph()
    {
        for (int y = 0; y < graphHeight; y++)
            for (int x = 0; x < graphWidth; x++)
            {
                var newContent = new RoomContent(new Vector2(x, y));
                allContents.Add(newContent);
            }
    }

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
            Vector2 door = new Vector2(graphWidth-1, (graphHeight / 2));
            GetContent(door).SetContentType(ContentType.Empty);
        }

        if ((doorsPosition & DirectionFlag.West) == DirectionFlag.West)
        {
            Vector2 door = new Vector2(0, (graphHeight / 2));
            GetContent(door).SetContentType(ContentType.Empty);
        }
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
