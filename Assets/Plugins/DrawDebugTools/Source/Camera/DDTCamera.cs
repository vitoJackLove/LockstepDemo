using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DDTCamera : MonoBehaviour
{
    public Camera m_Camera;
    
    private float m_DebugCameraMovSpeedMultiplier = 0.1f;
    private Vector2 m_DebugCameraMovSpeedMultiplierRange = new Vector2(0.01f, 10.0f);
    private float m_DebugCameraPitch = 0.0f;
    private float m_DebugCameraYaw = 0.0f;

    void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }

    void Start()
    {
        transform.position = Camera.main.transform.position;
        transform.rotation = Camera.main.transform.rotation;
    }

    public void InitializeCamRotation()
    {
        m_DebugCameraYaw = transform.eulerAngles.y;
        m_DebugCameraPitch = transform.eulerAngles.x;
    }

    void Update()
    {
        // Change camera movement speed
        float SpeedMultiplierSensitivity = m_DebugCameraMovSpeedMultiplier < 1.0f ? 2.0f : 10.0f;
        m_DebugCameraMovSpeedMultiplier += Input.mouseScrollDelta.y * SpeedMultiplierSensitivity * Time.unscaledDeltaTime;
        m_DebugCameraMovSpeedMultiplier = Mathf.Clamp(m_DebugCameraMovSpeedMultiplier, m_DebugCameraMovSpeedMultiplierRange.x, m_DebugCameraMovSpeedMultiplierRange.y);

        // Camera translation and rotation
        float MoveSpeed = 50.0f * m_DebugCameraMovSpeedMultiplier;
        float RotateSpeed = 100.0f;

        Vector3 DirectionSpeed = Vector3.zero;
        DirectionSpeed.z = Input.GetAxisRaw("Vertical") * MoveSpeed * Time.unscaledDeltaTime;
        DirectionSpeed.x = Input.GetAxisRaw("Horizontal") * MoveSpeed * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.E))
            DirectionSpeed.y = 0.8f * MoveSpeed * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.Q))
            DirectionSpeed.y = -0.8f * MoveSpeed * Time.unscaledDeltaTime;

        // Set debug cam position
        transform.position += transform.right * DirectionSpeed.x + transform.forward * DirectionSpeed.z + Vector3.up * DirectionSpeed.y;

        // Set debug cam rotation
        if (Input.GetMouseButton(0))
        {
            m_DebugCameraYaw += Input.GetAxis("Mouse X") * RotateSpeed * Time.unscaledDeltaTime;
            m_DebugCameraPitch += -Input.GetAxis("Mouse Y") * RotateSpeed * Time.unscaledDeltaTime;
            transform.eulerAngles = new Vector3(m_DebugCameraPitch, m_DebugCameraYaw, 0.0f);
        }
    }

    public float GetDebugCameraMovSpeedMultiplier() { return m_DebugCameraMovSpeedMultiplier; }
}
