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
        m_euler = pivot.eulerAngles;
        m_position = pivot.position;
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
        var localPos = transform.localPosition;

        if ((Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0)) || Input.GetMouseButton(1))
        {
            m_euler += rotationSensitivity * new Vector3(-delta.y, delta.x, 0);
        }
        else if (Input.GetMouseButton(2))
        {
            m_position -= (localPos.z * panningSensitivity) * (Quaternion.Euler(0, m_euler.y, 0) *  new Vector3(delta.x, 0, delta.y));
        }

        var scoll = Input.mouseScrollDelta;

        localPos.z *= (1.0f + scoll.y * scrollSensitivity);


        transform.localPosition = localPos;


        m_prevMousePos = currentMousePos;
        pivot.rotation = Quaternion.Euler(m_euler);
        pivot.position = m_position;
    }

    private Vector3 m_prevMousePos;

    private Vector3 m_euler;
    private Vector3 m_position;
}
