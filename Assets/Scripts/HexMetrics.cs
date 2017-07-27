using UnityEngine;

/// <summary>
/// metrics for in outer radius and inner radius of the hexagons
/// hexagon is made of six equilateral triangles, inner radius is equal to the height of one of the triangles
/// </summary>
public static class HexMetrics {

	public const float outerRadius = 10f;

	public const float innerRadius = outerRadius * 0.866025404f;


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
}