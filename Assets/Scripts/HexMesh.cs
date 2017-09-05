using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

    Mesh hexMesh;
    List<Vector3> vertices;
    List<int> triangles;
    List<Color> colors;
    MeshCollider meshCollider;

    void Awake() {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        hexMesh.name = "Hex Mesh";
        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color>();
    }

    public void Triangulate (HexCell[] cells) {
        //clear old data
        hexMesh.Clear();
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
        //loop through all the cells, triangulate them individually
        for (int i = 0; i < cells.Length; i++) {
            Triangulate(cells[i]);
        }
        //assign generated vertices and triangles to the mesh
        hexMesh.vertices = vertices.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.colors = colors.ToArray();
        //recalculate mesh normals
        hexMesh.RecalculateNormals();
        //assign mesh to collider
        meshCollider.sharedMesh = hexMesh;
    }

    //ToDo:  possibly four triangles instead of 6 by sharing vertices
    void Triangulate (HexCell cell) {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            Triangulate(d, cell);
        }
    }

    void Triangulate (HexDirection direction, HexCell cell) {
        //center of hex
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.color);

        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, v1, v2);
        }


    }

    void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2) {
        HexCell neighbour = cell.GetNeighbour(direction);
        if (neighbour == null) {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;
        //override the height of the other end of the bridge
        v3.y = v4.y = neighbour.Elevation * HexMetrics.elevationStep;

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
            TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbour);

        } else {
            //covers flats and cliffs
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(cell.color, neighbour.color); 
        }

        //triangular connections (holes)
        HexCell nextNeighbour = cell.GetNeighbour(direction.Next());
        if (direction <= HexDirection.E && nextNeighbour != null) {
            Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbour.Elevation * HexMetrics.elevationStep;
            //is cell lower than its neighbours, or tied for the lowest
            if (cell.Elevation <= neighbour.Elevation) {
                if (cell.Elevation <= nextNeighbour.Elevation) {
                    TriangulateCorner(v2, cell, v4, neighbour, v5, nextNeighbour);
                } 
                //then next neighbour is the lowest cell, rotate triangle counterclockwise to keep it correctly oriented
                else {
                    TriangulateCorner(v5, nextNeighbour, v2, cell, v4, neighbour);
                }
            }
            //if edge neighbour is the lowest rotate clockwise
            else if (neighbour.Elevation <= nextNeighbour.Elevation) {
                TriangulateCorner(v4, neighbour, v5, nextNeighbour, v2, cell);
            }
            //otherwise counterclockwise
            else {
                TriangulateCorner(v5, nextNeighbour, v2, cell, v4, neighbour);
            }

        }
    }

    void TriangulateCorner (Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell) {
        //determine the types of the left and right edges 
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        //check if Slope slope flat 
        if (leftEdgeType == HexEdgeType.Slope) {
            if (rightEdgeType == HexEdgeType.Slope) {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }

            //if right edge is flat then slope flat slope
            else if (rightEdgeType == HexEdgeType.Flat) {
                //begin terracing from the left
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            //when left is a slope but right is not a flat, then its a cliff
            else {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell); 
            }
        }

        //if left edge is flat then flat slope slope
        else if (rightEdgeType == HexEdgeType.Slope) {
            if (leftEdgeType == HexEdgeType.Flat) {
                //begin terracing from the right
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            //when right is a slope but left is not a flat, then its a cliff
            else {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        //Cliff Cliif Slope 
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            //if right
            if (leftCell.Elevation < rightCell.Elevation) {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            //left
            else {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        } 
        //covers FFF, CCF, CCCCR, CCCCL, which are covered with a single triangle
        else {
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color); 
        }
    }

    //connect the left and right terraces across the gap
    void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell) {

        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

        //first step
        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.color, c3, c4);

        //quad steps in between
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

    //triangulate an edge connection
    void TriangulateEdgeTerraces( Vector3 beginLeft, Vector3 beginRight, HexCell beginCell,
        Vector3 endLeft, Vector3 endRight, HexCell endCell) {

        Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);
        //first step
        AddQuad(beginLeft, beginRight, v3, v4);
        AddQuadColor(beginCell.color, c2);

        //intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            //previous last two vertices become the new first two
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            //previous last color becomes new color
            Color c1 = c2;
            //compute new vectors
            v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
            v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            //compute new colors
            c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
            //add another quad
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2);
        }

        //last step
        AddQuad(v3, v4, endLeft, endRight);
        AddQuadColor(c2, endCell.color);
    }

    //method to deal with Slope Cliff Slope, and Slope Cliff Cliff
    void TriangulateCornerTerracesCliff (Vector3 begin, HexCell begincell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell) {
        //boundary point is one elevation level above the bottom cell
        float b = 1f / (rightCell.Elevation - begincell.Elevation);
        //make sure the boundary interpolators are always positive
        if (b < 0) {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(begin, right, b);
        Color boundaryColor = Color.Lerp(begincell.color, rightCell.color, b);

        TriangulateBoundaryTriangle(begin, begincell, left, leftCell, boundary, boundaryColor);

        //if the top edge is another slope, add a rotated boundary triangle
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        //else must be a slope 
        else {
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    //method to deal with Cliff Slope Slope and Cliff Slope Cliff 
    void TriangulateCornerCliffTerraces(Vector3 begin, HexCell begincell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell) {
        //boundary point is one elevation level above the bottom cell
        float b = 1f / (leftCell.Elevation - begincell.Elevation);
        //make sure the boundary interpolators are always positive
        if (b < 0) {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(begin, left, b);
        Color boundaryColor = Color.Lerp(begincell.color, leftCell.color, b);

        TriangulateBoundaryTriangle(right, rightCell, begin, begincell, boundary, boundaryColor);

        //if the top edge is another slope, add a rotated boundary triangle
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        //else must be a slope 
        else {
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor) {

        Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        //first collapsing step
        AddTriangle(begin, v2, boundary);
        AddTriangleColor(beginCell.color, c2, boundaryColor);
        //steps between first and last
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            AddTriangle(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }
        //last step
        AddTriangle(v2, left, boundary);
        AddTriangleColor(c2, leftCell.color, boundaryColor);
    }

    //given 3 vertex positions add a triangle
    void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    void AddTriangleColor (Color color) {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    //seperate color for each vertex
    void AddTriangleColor (Color c1, Color c2, Color c3) {
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

    void AddQuadColor(Color c1, Color c2, Color c3, Color c4) {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);

    }

    void AddQuadColor (Color c1, Color c2) {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }
}
