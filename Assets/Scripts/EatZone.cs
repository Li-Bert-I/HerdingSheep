using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zone : MonoBehaviour
{
    public Collider colliderMesh;
    float margin = 0.1f;

    void Start()
    {
        colliderMesh = GetComponent<Collider>();
    }
    public bool IsInside(Vector3 point)
    {
        var closest = colliderMesh.ClosestPoint(point);
        return closest == point;
    }

    public Vector3 GetPointInDistanceFrom(Vector3 position, float distance)
    {
        Vector3 result;
        do {
            result = new Vector3(
                    UnityEngine.Random.Range(colliderMesh.bounds.min.x + margin, colliderMesh.bounds.max.x - margin),
                    colliderMesh.bounds.center.y,
                    UnityEngine.Random.Range(colliderMesh.bounds.min.z + margin, colliderMesh.bounds.max.z - margin)
                );
        } while (!(IsInside(result) && (result - position).magnitude <= distance));
        return result;
    }
}

public class EatZone : Zone
{
    
}
