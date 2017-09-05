using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour {

    public HexCoordinates coordinates;

    public Color color;
    public RectTransform uiRect;
    int elevation;

    [SerializeField]
    HexCell[] neightbours;

    public int Elevation {
        get {
            return elevation;
        }
        set {
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            //applies pertubation to cell's vertical position
            position.y +=
                (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            //ui
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
        }
    } 

    //to get a cells position
    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    //retrieve a cell's neighbour in one direction
    public HexCell GetNeighbour (HexDirection direction) {
        return neightbours[(int)direction];
    }

    // set a neighbour
    public void SetNeighbour (HexDirection direction, HexCell cell) {
        neightbours[(int)direction] = cell;
        cell.neightbours[(int)direction.Opposite()] = this;
    }

    //get a cell's edge type in a certain direction
    public HexEdgeType GetEdgeType (HexDirection direction) {
        return HexMetrics.GetEdgeType(elevation, neightbours[(int)direction].elevation);
    }

    //determine a slope between any two cells
    public HexEdgeType GetEdgeType (HexCell othercell) {
        return HexMetrics.GetEdgeType(elevation, othercell.elevation);
    }


}
