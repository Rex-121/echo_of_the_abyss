using UnityEngine;

// 相机硬跟随
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float zOffset = -10f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 p = target.position;
        transform.position = new Vector3(p.x, p.y, zOffset);
    }
}
