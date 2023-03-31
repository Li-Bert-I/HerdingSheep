using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Sheep))]
public class StatusLabelHandle : Editor
{
    private GUIStyle textStyle;
    void OnEnable()
    {
        textStyle = new GUIStyle();
        textStyle.fontSize = 25;
        textStyle.alignment = TextAnchor.MiddleCenter;
    }

    void OnSceneGUI()
    {
        Sheep t = (Sheep)target;
        if (t == null)
        {
            return;
        }
        Handles.color = Color.blue;
        UnityEditor.Handles.Label(t.transform.position + Vector3.up * 1, t.status.name, textStyle);
    }
}
