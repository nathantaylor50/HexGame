using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
public class HexGrid : MonoBehaviour {

	public int width = 6;
	public int height = 6;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

    public Color defaultColor = Color.white;

	HexCell[] cells;

	Canvas gridCanvas;
	HexMesh hexMesh;

	void Awake () {
        //get the canvas from child object
        gridCanvas = GetComponentInChildren<Canvas>();
		hexMesh = GetComponentInChildren<HexMesh>();

		cells = new HexCell[height * width];

		for (int z = 0, i = 0; z < height; z++) {
			for (int x = 0; x < width; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	void Start () {
		hexMesh.Triangulate(cells);
	}


    public void ColorCell (Vector3 position, Color color) {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        //convert the cell coordinates to the appropriate array index
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        //grab cell
        HexCell cell = cells[index];
        //change cell color
        cell.color = color;
        //triangulate the mesh again
        hexMesh.Triangulate(cells);
    }

	void CreateCell (int x, int z, int i) {
		Vector3 position;
        //distance between adjacent cells in x direction is equal to twice the inner radius
        //offset each row by adding half of z, and undo part of the offset so every second row moves back one step
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
        //distance to the next row of cells should be 1.5 times the outer radius
        position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.SetParent(transform, false);
		cell.transform.localPosition = position;
        //take advantage of the new coordinates
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        //asign default color
        cell.color = defaultColor;
        //first cell in each row doesnt have an east neighbour but all other cells in the row do
        if (x > 0 ) {
            cell.SetNeighbour(HexDirection.W, cells[i - 1]);
        }
        //skip first row
        if (z > 0) {
            //all cells in even rows have a SE neighbour so connect those
            if ((z & 1) == 0) {
                cell.SetNeighbour(HexDirection.SE, cells[i - width]);
                //connect to SW neighbour (except from first cell in each row)
                if (x > 0) {
                    cell.SetNeighbour(HexDirection.SW, cells[i - width - 1]);
                }
            }
            //mirrored logic for Odd rows
            else {
                cell.SetNeighbour(HexDirection.SW, cells[i - width]);
                if (x < width - 1) {
                    cell.SetNeighbour(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        //place newline character between X and Z so they end up on separate lines
        Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.SetParent(gridCanvas.transform, false);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();
	}
}