using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

    public int width = 6;
    public int height = 6;

    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;
    HexCell[] cells;
    Canvas gridCanvas;
    HexMesh hexMesh;

    void Awake() {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();
        cells = new HexCell[height * width];

        for (int z = 0, i = 0; z < height; z++) {
            for (int x = 0; x < width; x++) {
                CreateCell(x, z, i++);
            }
        }
    }

    void Start() {
        //triangulate cells
        hexMesh.Triangulate(cells);
    }


    public HexCell GetCell (Vector3 position) {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        //convert cell coords to the appropriate array index
        // X + Z * width + Z / 2
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        return cells[index];
    }


    //create cell
    void CreateCell (int x, int z, int i) {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);

        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        //assign default color
        cell.color = defaultColor;

        //cells that are not the first cell of a row (E + W directions)
        if (x > 0) {
            cell.SetNeighbour(HexDirection.W, cells[i - 1]);
        }
        //skip first row
        if (z > 0) {
            //if even then it has a SE neighbour
            if ((z & 1) == 0) {
                cell.SetNeighbour(HexDirection.SE, cells[i - width]);
                //connecting from NE to SW on even rows
                if (x > 0) {
                    cell.SetNeighbour(HexDirection.SW, cells[i - width - 1]);
                }
            }
            //odd rows follow same logic but mirrored
            else {
                cell.SetNeighbour(HexDirection.SW, cells[i - width]);
                if (x < width - 1) {
                    cell.SetNeighbour(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        //label prefabs
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeperateLines();
        //UI labels position
        cell.uiRect = label.rectTransform;
    }

    //triangulae the mesh
    public void Refresh() {
        hexMesh.Triangulate(cells);
    }
}
