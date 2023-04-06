using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Setup : MonoBehaviour
{
    public float timeScale = 1.0f;

    void Start()
    {
        Debug.Log("Start scene");
    }

    void Update()
    {
        Time.timeScale = timeScale;
    }
}
