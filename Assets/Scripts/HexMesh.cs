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
        Vector3 center = cell.Position;
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        TriangulateEdgeFan(center, e, cell.color);

        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, e);
        }
    }

    void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1) {

        HexCell neighbour = cell.GetNeighbour(direction);
        if (neighbour == null) {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbour.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v4 + bridge);


        if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
            TriangulateEdgeTerraces(e1, cell, e2, neighbour);

        } else {
            TriangulateEdgeStrip(e1, cell.color, e2, neighbour.color);
        }

        //triangular connections (holes)
        HexCell nextNeighbour = cell.GetNeighbour(direction.Next());
        if (direction <= HexDirection.E && nextNeighbour != null) {
            Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbour.Position.y;
            //is cell lower than its neighbours, or tied for the lowest
            if (cell.Elevation <= neighbour.Elevation) {
                if (cell.Elevation <= nextNeighbour.Elevation) {
                    TriangulateCorner(e1.v4, cell, e2.v4, neighbour, v5, nextNeighbour);
                } 
                //then next neighbour is the lowest cell, rotate triangle counterclockwise to keep it correctly oriented
                else {
                    TriangulateCorner(v5, nextNeighbour, e1.v4, cell, e2.v4, neighbour);
                }
            }
            //if edge neighbour is the lowest rotate clockwise
            else if (neighbour.Elevation <= nextNeighbour.Elevation) {
                TriangulateCorner(e2.v4, neighbour, v5, nextNeighbour, e1.v4, cell);
            }
            //otherwise counterclockwise
            else {
                TriangulateCorner(v5, nextNeighbour, e1.v4, cell, e2.v4, neighbour);
            }

        }
    }

    //create a triangle fan between a cell's center and one of its edges
    void TriangulateEdgeFan (Vector3 center, EdgeVertices edge, Color color) {
        AddTriangle(center, edge.v1, edge.v2);
        AddTriangleColor(color);
        AddTriangle(center, edge.v2, edge.v3);
        AddTriangleColor(color);
        AddTriangle(center, edge.v3, edge.v4);
        AddTriangleColor(color);
    }

    //triangulate a strip of quads between two edges
    void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2) {
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        AddQuadColor(c1, c2);
        AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        AddQuadColor(c1, c2);
        AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        AddQuadColor(c1, c2);
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
    void TriangulateEdgeTerraces( EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell) {

        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);
        //first step
        TriangulateEdgeStrip(begin, beginCell.color, e2, c2);

        //intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            //compute new colors
            c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        //last step
        TriangulateEdgeStrip(e2, c2, end, endCell.color);
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
        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
        Color boundaryColor = Color.Lerp(begincell.color, rightCell.color, b);

        TriangulateBoundaryTriangle(begin, begincell, left, leftCell, boundary, boundaryColor);

        //if the top edge is another slope, add a rotated boundary triangle
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        //else must be a slope 
        else {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
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
        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
        Color boundaryColor = Color.Lerp(begincell.color, leftCell.color, b);

        TriangulateBoundaryTriangle(right, rightCell, begin, begincell, boundary, boundaryColor);

        //if the top edge is another slope, add a rotated boundary triangle
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        //else must be a slope 
        else {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor) {

        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        //first collapsing step
        //no pertrub on the boundary points
        AddTriangleUnperturbed(Perturb(begin), v2, boundary);
        AddTriangleColor(beginCell.color, c2, boundaryColor);
        //steps between first and last
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            AddTriangleUnperturbed(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }
        //last step
        AddTriangleUnperturbed(v2, Perturb(left), boundary);
        AddTriangleColor(c2, leftCell.color, boundaryColor);
    }


    //given 3 vertex positions add a triangle
    void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
        int vertexIndex = vertices.Count;
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    //AddTriangle alternative that does not perturb the vertices
    void AddTriangleUnperturbed (Vector3 v1, Vector3 v2, Vector3 v3) {
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
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        vertices.Add(Perturb(v4));
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

    //perturbing each vertex individually, takes a unperturbed point and returns a perturbed one
    Vector3 Perturb (Vector3 position) {
        //sample noise with unperturbed point
        Vector4 sample = HexMetrics.SampleNoise(position);
        // X Z Y noise samples
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        //position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }
}
