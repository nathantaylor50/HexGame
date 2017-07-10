using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// struct which used to convert to different coordinate system
/// </summary>
[System.Serializable]
public struct HexCoordinates {

    [SerializeField]
    private int x, z;

    public int X {
        get {
            return x;
        }
    }

    public int Z { 
        get {
            return z;
        }
    }

    public HexCoordinates (int x, int z) {
        this.x = x;
        this.z = z;
    }

    /// <summary>
    /// create set of coordinates using regular offset coordinates
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static HexCoordinates FromOffsetCoordinates (int x, int z) {
        return new HexCoordinates(x - z / 2, z);
    }

    /// <summary>
    /// compute the Y coordinate to use in the string method
    /// </summary>
    public int Y {
        get {
            return -X - Z;
        }
    }

    /// <summary>
    /// override to return the coordinates on a single line
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "(" + 
            X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    /// <summary>
    /// puts the coordinates on serparate lines
    /// </summary>
    /// <returns></returns>
    public string ToStringOnSeparateLines() {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    public static HexCoordinates FromPosition (Vector3 position) {
        //divide x by the horizontal width of a hexagon
        float x = position.x / (HexMetrics.innerRadius * 2f);
        //y is a mirror of x coordinate, so negatuve of x gives us y
        float y = -x;

        //every two rows shift the entire unit left
        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;

        //round to ints
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        //discard the coordinate with the largest rounding data
        //then reconstruct it from the other two ( X + Z)
        if (iX + iY + iZ != 0) {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(iZ - iZ);

            if (dX > dY && dX > dZ) {
                iX = -iY - iZ;
            }
            else if (dZ > dY) {
                iZ = -iX - iY;
            }

        }

        //construct final coordinates
        return new HexCoordinates(iX, iZ);
    }
}
