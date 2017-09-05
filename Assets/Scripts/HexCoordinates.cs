using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    public int Y {
        get {
            return -X - Z;
        }
    }

    public HexCoordinates (int x, int z) {
        this.x = x;
        this.z = z;
    }

    //create a set of coordinates using regular offset coordinates
    public static HexCoordinates FromOffsetCoordinates (int x, int z) {
        return new HexCoordinates(x - z / 2, z);
    }

    //string converstion to retirn the coordinates on a single line
    public override string ToString() {
        return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ") ";
    }

    //put coordinates on seperate lines
    public string ToStringOnSeperateLines() {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    //which coordinate belongs to a position
    public static HexCoordinates FromPosition (Vector3 position) {
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x;

        //shift
        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;
        //round to ints
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x -y);

        //if we get cords that != 0
        if (iX + iY + iZ != 0) {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x -y - iZ);
            //reconstruct coord from other two (dont bother with y)
            if (dX > dY && dX > dZ) {
                iX = -iY - iZ;
            } else if (dZ > dY) {
                iZ = -iX - iY;
            }

        }

        return new HexCoordinates(iX, iZ);

    }
}
