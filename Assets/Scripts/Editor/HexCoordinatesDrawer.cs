using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer {

    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label){

        //extract x + z values from the property
        HexCoordinates coordinates = new HexCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue
            );
        //field name
        position = EditorGUI.PrefixLabel(position, label);
        //draw a gui label at specified position
        GUI.Label(position, coordinates.ToString());
    }
}
