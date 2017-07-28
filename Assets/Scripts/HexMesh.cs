using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// class that makes a mesh from vertices create triangles to form hexagons
/// 
/// NOTE: future optimization could be using only four triangles instead of siz to create 
/// the hexagons, but this early in this project I want to keep things simple
/// but this could be a possible optimization down the road.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	Mesh hexMesh;
	List<Vector3> vertices;
	List<int> triangles;
    List<Color> colors;

	MeshCollider meshCollider;

	void Awake () {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		meshCollider = gameObject.AddComponent<MeshCollider>();
		hexMesh.name = "Hex Mesh";
		vertices = new List<Vector3>();
		triangles = new List<int>();
        colors = new List<Color>();
	}

    /// <summary>
    /// clear old data, 
    /// loop through all the cells and triangulate them individually
    /// then assign generated vertices and triangles to the mesh
    /// then recalculate mesh normals
    /// </summary>
    /// <param name="cells"></param>
	public void Triangulate (HexCell[] cells) {
		hexMesh.Clear();
		vertices.Clear();
		triangles.Clear();
        colors.Clear();
		for (int i = 0; i < cells.Length; i++) {
			Triangulate(cells[i]);
		}
		hexMesh.vertices = vertices.ToArray();
		hexMesh.triangles = triangles.ToArray();
        hexMesh.colors = colors.ToArray();
		hexMesh.RecalculateNormals();
		meshCollider.sharedMesh = hexMesh;
	}

    /// <summary>
    /// use directions to triangulate
    /// </summary>
    /// <param name="cell"></param>
	void Triangulate (HexCell cell) {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            Triangulate(d, cell);
        }
	}

    /// <summary>
    /// triangulate cells
    ///  center of the hexagon. 
    ///  first and second corners, 
    /// relative to its center.
    /// 
    /// quad blends beween the solid color and the two corner colors
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    void Triangulate(HexDirection direction, HexCell cell) {
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.color);

        //add bridge when dealing with a NE connection
        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, v1, v2);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2) {
        HexCell neighbour = cell.GetNeighbour(direction);
        //if no neighbour then no bridge connection
        if (neighbour == null) { return; }

        //blending bridges between neighbours
        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;

        AddQuad(v1, v2, v3, v4);
        AddQuadColor(cell.color, neighbour.color);

        //triangular connection
        HexCell nextNeighbour = cell.GetNeighbour(direction.Next());
        //if there is a neighbour
        if (direction <= HexDirection.E && nextNeighbour != null) {
            //triangle that connects to the next neighbour
            AddTriangle(v2, v4, v2 + HexMetrics.GetBridge(direction.Next()));
            AddTriangleColor(cell.color, neighbour.color, nextNeighbour.color);
        }
    }


    /// <summary>
    /// using 3 vertex positions adds the vertices in order
    /// adds vertices to form a triangle
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
	void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

    /// <summary>
    /// single color
    /// </summary>
    /// <param name="color"></param>
    void AddTriangleColor(Color color) {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    /// <summary>
    /// separate color for each vertex
    /// </summary>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    /// <param name="c3"></param>
    void AddTriangleColor(Color c1, Color c2, Color c3) {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    void AddQuadColor (Color c1, Color c2, Color c3, Color c4) {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }

    //variant of AddQuadColor that only needs two colors
    void AddQuadColor (Color c1, Color c2) {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }


}