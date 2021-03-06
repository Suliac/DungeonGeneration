﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GrammarPattern", menuName = "DungeonGenerator/Grammar/Pattern", order = 1)]
public class GrammarPattern : ScriptableObject
{
    public int Width = 3;
    public int Level = 0; // Certains patterns sont appliqués avant tous les autres pour s'assurer d'avoir toujours un chemin libre
    public bool IntensityIncreaseProbability = false;
    public bool ApplyAtAnyCost = false;
    public RoomTypeFlags ApplyToRoomOfType = RoomTypeFlags.NORMAL;
    public List<ContentType> GraphBefore = new List<ContentType>();
    public List<ContentType> GraphAfter = new List<ContentType>();
}
