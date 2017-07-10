using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor only script
/// defining a custom property drawer for the HexCoordinates type
/// </summary>
[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer {

    /// <summary>
    /// provides the screen rectangle to draw inside,
    /// the serialized data of the property,
    /// and the label of the field it belongs to
    /// 
    /// then extract the x and z values from the property, and use those 
    /// to create a new set of coordinates.
    /// then draw a GUI label at the specified position using the HexCoordinates.ToString method
    /// </summary>
    /// <param name="position"></param>
    /// <param name="property"></param>
    /// <param name="label"></param>
    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
        HexCoordinates coordinates = new HexCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue
            );
        position = EditorGUI.PrefixLabel(position, label);
        GUI.Label(position, coordinates.ToString());
    }

}
