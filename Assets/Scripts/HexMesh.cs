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
        //override the height of the other end of the bridge for slopes
        v3.y = v4.y = neighbour.Elevation * HexMetrics.elevationStep;

        //if slope create terrace
        if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
            TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbour);
        } else {
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(cell.color, neighbour.color);
        }

        //triangular connection
        HexCell nextNeighbour = cell.GetNeighbour(direction.Next());
        //if there is a neighbour
        if (direction <= HexDirection.E && nextNeighbour != null) {
            //triangle that connects to the next neighbour
            Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbour.Elevation * HexMetrics.elevationStep;  

            //if cell is lower than its neighbours, or tied for lowest
            if (cell.Elevation <= neighbour.Elevation) {
                if (cell.Elevation <= nextNeighbour.Elevation) {
                    TriangulateCorner(v2, cell, v4, neighbour, v5, nextNeighbour);
                }
                //else means the next neighbour is the lowest cell
                else {
                    TriangulateCorner(v5, nextNeighbour, v2, cell, v4, neighbour);
                }
            }
            //if previous IF failed check which neighbour is the lowest
            else if (neighbour.Elevation <= nextNeighbour.Elevation) {
                //rotate clockwise
                TriangulateCorner(v4, neighbour, v5, nextNeighbour, v2, cell);
            } 
            else {
                //rotate counterclockwise
                TriangulateCorner(v5, nextNeighbour, v2, cell, v4, neighbour);
            }

        }
    }

    /// <summary>
    /// terraced corner connections
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="bottomCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCorner (Vector3 bottom, HexCell bottomCell, 
        Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {
        //determine the types of the left and right edges
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        //check if were have a triangle with a slope on two sides and a flat on its third
        if (leftEdgeType == HexEdgeType.Slope) {
            if (rightEdgeType == HexEdgeType.Slope) {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            //if right edge is flat
            else if (rightEdgeType == HexEdgeType.Flat) {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            //left edge is a slope
            else {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        //if right edge is a slope with a left flat
        else if (rightEdgeType == HexEdgeType.Slope) {
            if (leftEdgeType == HexEdgeType.Flat) {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            } 
            else {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }

        //double cliffs, bottom cell has cliffs on both sides
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            if (leftCell.Elevation < rightCell.Elevation) {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else {
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }
    }

    /// <summary>
    /// triangulate edge connection
    /// </summary>
    /// <param name="beginLeft"></param>
    /// <param name="beginRight"></param>
    /// <param name="beginCell"></param>
    /// <param name="endLeft"></param>
    /// <param name="endRight"></param>
    /// <param name="endCell"></param>
    void TriangulateEdgeTerraces(Vector3 beginLeft, Vector3 beginRight, HexCell beginCell,
        Vector3 endLeft, Vector3 endRight, HexCell endCell) {

        //first quad, short slope thats steeper than the original slope
        Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

        AddQuad(beginLeft, beginRight, v3, v4);
        AddQuadColor(beginCell.color, c2);

        //intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            //last two vertices become the new first two
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            //last color becomes new color
            Color c1 = c2;
            //new vectors are computed
            v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
            v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            //new color is computed
            c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
            //add quad
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2);
        }

        //last step
        AddQuad(v3, v4, endLeft, endRight);
        AddQuadColor(c2, endCell.color);
    }

    /// <summary>
    /// this method will deal with when we have a triangle with a slope on two sides
    /// and a flat on its third
    /// 
    /// to fill hole connect left and right terraces with a triangle for the first triangular step
    /// then a quad for the last step
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, 
        Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {

        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);
        //first triangular step
        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.color, c3, c4);

        //all steps in between which will be quads
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }

        //last quad step
        AddQuad(v3, v4, left, right);
        AddQuadColor(c3, c4, leftCell.color, rightCell.color);

    }

    /// <summary>
    /// when a slope meets a cliff
    /// 
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerTerracesCliff (Vector3 begin, HexCell beginCell, 
        Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {
        //boundary point one elevation level above the bottom cell
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        //make sure boundary interpolators are positive
        if (b < 0) {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(begin, right, b);
        Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        //if slope add a rotated boundary triangle
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        //otherwise a simple triangle
        else {
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }

    }

    /// <summary>
    /// when a cliff meats a slope
    /// CSS and CSC
    /// mirror of TriangulateCornerTerracesCliff
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {
        //boundary point one elevation level above the bottom cell
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        //make sure boundary interpolators are positive
        if (b < 0) {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(begin, left, b);
        Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        //if slope add a rotated boundary triangle
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        //otherwise a simple triangle
        else {
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="boundary"></param>
    /// <param name="boundaryColor"></param>
    void TriangulateBoundaryTriangle (Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor) {

        //first collapsing step
        Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

        //bottom part with a single triangle
        AddTriangle(begin, v2, boundary);
        AddTriangleColor(beginCell.color, c2, boundaryColor);

        //all steps in between with triangles
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            AddTriangle(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        //last collapsing step
        AddTriangle(v2, left, boundary);
        AddTriangleColor(c2, leftCell.color, boundaryColor);


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

    //variant of AddQuadColor that only needs two colors
    void AddQuadColor(Color c1, Color c2) {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    void AddQuadColor (Color c1, Color c2, Color c3, Color c4) {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }

}