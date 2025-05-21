using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;         // �������� ����������� ������
    public float unitSize = 50f;          // ������ ����� ������ (������ ��������� � ����������)
    public int gridResolution = 64;       // ����� ����� �� ����� ��� (����� 64x64)

    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;    // �������� �������� ������ (����������������)
    public float minPitch = -10f;         // ����������� ���� �� ��������� (������� ������ ����)
    public float maxPitch = 60f;          // ������������ ���� �� ��������� (������� ������ �����)

    // ���������� ���������� ��� �������� ����� ��������
    private float yaw = 0f;               // �������������� ����
    private float pitch = 0f;             // ������������ ����

    void Start()
    {
        // �������������� ���� �� �������� �������� ������
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        // ������ ��������� �������, ��� ��� ������� ������������ �����������.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    /// <summary>
    /// ����������� ������ ������������ ����������� �������.
    /// �������� �������������� �� ���������� ��������, � �������� ������� �������������� ��������� ��������������� �����.
    /// </summary>
    void HandleMovement()
    {
        // �������� ����������� ������������ �������� ������� ������.
        // ������������ ���������� ����������, ����� �������� ����������� ������ �� �����������.
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        // ������ �������� �������� � ���������� (�������)
        float verticalInput = 0f;
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            verticalInput += 1f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            verticalInput -= 1f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput += 1f;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput -= 1f;
        }

        Vector3 move = (forward * verticalInput + right * horizontalInput) * moveSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + move;

        // ������������ ��������� ������ �� X � Z, ����� ��� �� �������� �� ������� ��������������� �������.
        float minX = 0f;
        float maxX = gridResolution * unitSize;
        float minZ = 0f;
        float maxZ = gridResolution * unitSize;
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);

        transform.position = newPos;
    }

    /// <summary>
    /// ������� ������ � ������� ������ WASD:
    /// W/S - �������� ������������ ���� (pitch) � ������������,
    /// A/D - �������� �������������� ���� (yaw).
    /// </summary>
    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.W))
        {
            // ������� W ������� pitch (������ ������� ����)
            pitch -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            // ������� S ����������� pitch (������ ������� ����)
            pitch += rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            yaw -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            yaw += rotationSpeed * Time.deltaTime;
        }

        // ������������ ������������ ����, ����� �������� ��������� �������
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
