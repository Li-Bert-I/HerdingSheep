using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Sheep))]
public class StatusLabelHandle : Editor
{
    // private Camera cam;
    // private GUIStyle textStyle;

    // void Start()
    // {
    //     cam = Camera.main;
    // }

    // void OnEnable()
    // {
    //     textStyle = new GUIStyle();
    //     textStyle.fontSize = 25;
    //     textStyle.alignment = TextAnchor.MiddleCenter;
    // }

    // void OnSceneGUI()
    // {
    //     Sheep t = (Sheep)target;
    //     if (t == null)
    //     {
    //         return;
    //     }
    //     Handles.color = Color.blue;
    //     Vector3 textPosition = cam.ScreenToWorldPoint(new Vector3(100, 100, cam.nearClipPlane));
    //     UnityEditor.Handles.Label(textPosition, t.status.name, textStyle);
    // }
}
