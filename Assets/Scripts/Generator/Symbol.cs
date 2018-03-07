using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Symbol {
    /// <summary>
    /// -1 = START
    /// -2 = END
    /// -3 = BOSS
    /// ...
    /// [0-1000] = KEY LEVEL
    /// </summary>
    private int value;

    /// <summary>
    /// Init the symbol with the wanted value
    /// </summary>
    /// <param name="valueWanted"> -1 = START, -2 = END, -3 = BOSS, -4 = KEY ITEM IN ROOM, ..., [0-1000] = KEY LEVEL</param>
    public Symbol(int valueWanted)
    {
        value = valueWanted;
    }
}
