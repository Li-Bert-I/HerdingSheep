using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


[ExecuteInEditMode]
public class Sheep : MonoBehaviour
{
    [System.Serializable]
    public struct Status
    {
        private static string[] idToName = {"Idle", "Search food", "Want lie down", "Escape", "Fight", "Eat", "Lie down"};
        private int idValue;
        
        public int id;
        // {
        //     get { return idValue; }
        //     set
        //     {
        //         if (!(value >= 0 && value < idToName.Length))
        //         {
        //             throw new Exception("Недопустимый id статуса овцы");
        //         }
        //         idValue = value;
        //     }
        // }
        public string name
        {
            get { return idToName[id]; }
            private set { name = value; }
        }
    }
    
    [SerializeField]
    public Status status = new Status();
    private void UpdateState()
    {
        
    }

    void Start()
    {
        
    }

    void Update()
    {
        UpdateState();
    }
}
