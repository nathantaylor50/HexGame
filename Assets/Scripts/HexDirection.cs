using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HexDirection {
    NE, E, SE, SW, W, NW
}

//extension method to get the opposite direction,
public static class HexDirectionExtensions {
    public static HexDirection Opposite (this HexDirection direction) {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    //previous direction
    public static HexDirection Previous (this HexDirection direction) {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }
    //next direction
    public static HexDirection Next (this HexDirection direction) {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }
}