using UnityEngine;

/// <summary>
/// metrics for in outer radius and inner radius of the hexagons
/// hexagon is made of six equilateral triangles, inner radius is equal to the height of one of the triangles
/// </summary>
public static class HexMetrics {

	public const float outerRadius = 10f;

	public const float innerRadius = outerRadius * 0.866025404f;

    //solid region 
    public const float solidFactor = 0.75f;
    //blend region
    public const float blendFactor = 1f - solidFactor;

    public const float elevationStep = 5f;

    //terraces
    public const int terracesPerSlope = 2;
    public const int terraceSteps = terracesPerSlope * 2 + 1;
    public const float horizonalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);


    /// <summary>
    /// define positions of the six corners relative to the cell's center
    /// position the corner at the top and adds the rest in clockwise order in the xz plane
    /// </summary>
    static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};

    /// <summary>
    /// first corner
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstCorner (HexDirection direction) {
        return corners[(int)direction];
    }

    /// <summary>
    /// second corner
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSecondCorner (HexDirection direction) {
        return corners[(int)direction + 1];
    }

    /// <summary>
    /// retrieve first corner of solid inner hexagon
    /// used for blending between hexcells by other methods
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstSolidCorner (HexDirection direction) {
        return corners[(int)direction] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner (HexDirection direction) {
        return corners[(int)direction + 1] * solidFactor;
    }

    /// <summary>
    /// taking the midpoint between two relevent corners, 
    /// then applying the blend factor to that
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetBridge (HexDirection direction) {
        return (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
    }

    /// <summary>
    /// interpolation
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
        //horizontal interpolation
        float h = step * HexMetrics.horizonalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        //vertical interpolation just on odd steps
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    /// <summary>
    /// terrace interpolation for colors
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static Color TerraceLerp (Color a, Color b, int step) {
        float h = step * HexMetrics.horizonalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    /// <summary>
    /// derive what kind of connection between cells
    /// </summary>
    /// <param name="elevation1"></param>
    /// <param name="elevation2"></param>
    /// <returns></returns>
    public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
        //if the elevations are the same, we have a flat edge
        if(elevation1 == elevation2) {
            return HexEdgeType.Flat;
        }
        //if level difference is exactly one step when we have a slope
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1) {
            return HexEdgeType.Slope;
        }
        //in all other cases we have a cliff
        return HexEdgeType.Cliff;
    }
}