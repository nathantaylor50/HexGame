using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

    public Color[] colors;
    public HexGrid hexGrid;
    int activeElevation;

    private Color activeColor;

    void Awake() {
        SelectColor(0);
    }

    void Update() {
        if (Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject()) {
            HandleInput();
        }
    }

    void HandleInput() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            EditCell(hexGrid.GetCell(hit.point));
        }
    }

    /// <summary>
    /// handles the editing of a cell
    /// assign elevation level
    /// and refreshing the grid
    /// </summary>
    /// <param name="cell"></param>
    void EditCell (HexCell cell) {
        cell.color = activeColor;
        cell.Elevation = activeElevation;
        hexGrid.Refresh();
    }

    /// <summary>
    /// set elevation level
    /// UI sliders work with floats, so convert float parameter into an int
    /// </summary>
    /// <param name="elevation"></param>
    public void SetElevation(float elevation) {
        activeElevation = (int)elevation;
    }

    public void SelectColor (int index) {
        activeColor = colors[index];
    }
}
