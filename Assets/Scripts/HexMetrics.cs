using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public static class HexMetrics {
    //distance from the center to any corner
    public const float outerRadius = 10f;
    //distance from the center to each of the edges
    public const float innerRadius = outerRadius * 0.866025404f;

    //blending metrics
    public const float solidFactor = 0.75f;
    public const float blendFactor = 1f - solidFactor;

    //elevation step
    public const float elevationStep = 5f;

    //amount of terraces per slope
    public const int terracesPerSlope = 2;
    //amount of steps from amount of terraces
    public const int terraceSteps = terracesPerSlope * 2 + 1;
    //size of the horizontal terrace steps
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    //define the positions of the six corners relative to the cell's center.
    //corner at the top, start with this corner and add the rest in a clockwise order.
    static Vector3[] corners = {
        new Vector3(0f, 0f, outerRadius),
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(0f, 0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
        //duplicated first corner to avoid out of range index errors
        new Vector3(0f, 0f, outerRadius),
    };

    public static Vector3 GetFirstCorner(HexDirection direction) {
        return corners[(int)direction];
    }
    
    public static Vector3 GetSecondCorner(HexDirection direction) {
        return corners[(int)direction + 1];
    }

	public static Vector3 GetFirstSolidCorner(HexDirection direction) {
		return corners[(int)direction] * solidFactor;
	}


    public static Vector3 GetSecondSolidCorner(HexDirection direction) {
        return corners[(int)direction + 1] * solidFactor;
    }

    //taking midpoint between two relevant corners, then apply blender factor
    public static Vector3 GetBridge (HexDirection direction) {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            blendFactor;
    }

    public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
        //horizontal interpolation
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        //vertical interpolation
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    //terrace interpolation for colors (pretend the connection is flat)
    public static Color TerraceLerp (Color a, Color b, int step) {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    //get what connection 
    public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
        //if elevations are the same then = flat edge
        if (elevation1 == elevation2) {
            return HexEdgeType.Flat;
        }
        //if level difference = 1 step, then slope
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1) {
            return HexEdgeType.Slope;
        }
        //otherwise we have a cliff
        return HexEdgeType.Cliff;
    }

}
