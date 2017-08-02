using UnityEngine;

/// <summary>
/// this class creates the grid cells used to create a hexagon grid
/// </summary>
public class HexCell : MonoBehaviour {

    public HexCoordinates coordinates;

    public Color color;
    public RectTransform uiRect;

    private int elevation;

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

    /// <summary>
    /// 
    /// </summary>
    public int Elevation {
        get {
            return elevation;
        }
        //adjust cell's vertical position
        set {
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            transform.localPosition = position;

            //adjust cell's UI position
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = elevation * -HexMetrics.elevationStep;
            uiRect.localPosition = uiPosition;
        }
    }

    /// <summary>
    /// get the cell's edge type in a certain direction
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public HexEdgeType GetEdgeType (HexDirection direction) {
        return HexMetrics.GetEdgeType(elevation, neighbours[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType (HexCell otherCell) {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }
}