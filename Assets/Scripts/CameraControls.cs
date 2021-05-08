using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public Transform pivot;
    public float rotationSensitivity;
    public float panningSensitivity;
    public float scrollSensitivity;

    void Start()
    {
        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {
        var currentMousePos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            m_prevMousePos = currentMousePos;
        }

        var delta = currentMousePos - m_prevMousePos;
        m_prevMousePos = currentMousePos;

        var localPos = transform.localPosition;
        var euler = pivot.eulerAngles;
        var position = pivot.position;

        if ((Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0)) || Input.GetMouseButton(1))
        {
            euler += rotationSensitivity * new Vector3(-delta.y, delta.x, 0);
        }
        else if (Input.GetMouseButton(2))
        {
            position -= (localPos.z * panningSensitivity) * (Quaternion.Euler(0, euler.y, 0) *  new Vector3(delta.x, 0, delta.y));
        }

        var scoll = Input.mouseScrollDelta;

        localPos.z *= (1.0f + scoll.y * scrollSensitivity);

        transform.localPosition = localPos;

        if (euler.x > 180)
            euler.x -= 360;
        if (euler.x < -180)
            euler.x += 360;
        euler.x = Mathf.Clamp(euler.x, 0, 89);

        pivot.rotation = Quaternion.Euler(euler);
        pivot.position = position;
    }

    private Vector3 m_prevMousePos;
}
