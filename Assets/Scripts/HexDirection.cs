/// <summary>
/// ordered list of directions for a hexcell
/// </summary>
public enum HexDirection  {
    NE, E, SE, SW, W, NW
}

/// <summary>
/// odered list of edge types for a hexcell
/// </summary>
public enum HexEdgeType {
    Flat, Slope, Cliff
}

/// <summary>
/// get the directions for;
/// OPPOSITE,
/// PREVIOUS,
/// NEXT
/// neighbours
/// </summary>
public static class HexDirectionExtensions {

    //get the direction from the opposite neighbour
    public static HexDirection Opposite(this HexDirection direction) {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    //get the direction from the previos neighbour
    public static HexDirection Previous(this HexDirection direction) {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    //get the direction from the next neigbour
    public static HexDirection Next(this HexDirection direction) {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }
}
