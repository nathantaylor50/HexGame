using UnityEngine;

/// <summary>
/// this class creates the grid cells used to create a hexagon grid
/// </summary>
public class HexCell : MonoBehaviour {

    public HexCoordinates coordinates;

    public Color color;

    //serialized so connections survive recompiles
    [SerializeField]
    HexCell[] neighbours;

    /// <summary>
    /// retrieve a cell's neighbour
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public HexCell GetNeighbour (HexDirection direction) {
        return neighbours[(int)direction];
    }

    /// <summary>
    /// set a neighbour
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    public void SetNeighbour (HexDirection direction, HexCell cell) {
        neighbours[(int)direction] = cell;
        //set bi-direction
        cell.neighbours[(int)direction.Opposite()] = this;
    }
}