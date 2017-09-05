using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// in-game map editor
/// </summary>
public class HexMapEditor : MonoBehaviour {

    public Color[] colors;
    public HexGrid hexGrid;
    private Color activeColor;
    int activeElevation;

    void Awake() {
        SelectColor(0);
    }

    void Update() {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) {
            HandleInput();
        }
    }

    //send raycast out
    void HandleInput() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            EditCell(hexGrid.GetCell(hit.point));
        }
    }

    public void SelectColor (int index) {
        activeColor = colors[index];
    }

    //edit the cell then refresh the grid
    void EditCell (HexCell cell) {
        cell.color = activeColor;
        //adjust elevation
        cell.Elevation = activeElevation;
        hexGrid.Refresh();
    }

    //set the active elevation level
    public void SetElevation(float elevation) {
        activeElevation = (int)elevation;
    }
}
