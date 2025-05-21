using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;         // скорость перемещения камеры
    public float unitSize = 50f;          // размер одной ячейки (должен совпадать с генерацией)
    public int gridResolution = 64;       // число ячеек по одной оси (схема 64x64)

    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;    // скорость поворота камеры (чувствительность)
    public float minPitch = -10f;         // минимальный угол по вертикали (поворот камеры вниз)
    public float maxPitch = 60f;          // максимальный угол по вертикали (поворот камеры вверх)

    // Внутренние переменные для хранения углов поворота
    private float yaw = 0f;               // горизонтальный угол
    private float pitch = 0f;             // вертикальный угол

    void Start()
    {
        // Инициализируем углы из текущего поворота камеры
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        // Курсор оставляем видимым, так как поворот производится клавиатурой.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    /// <summary>
    /// Перемещение камеры относительно направления взгляда.
    /// Движение осуществляется по стрелочным клавишам, и конечная позиция ограничивается границами сгенерированной схемы.
    /// </summary>
    void HandleMovement()
    {
        // Получаем направление относительно текущего взгляда камеры.
        // Вертикальная компонента обнуляется, чтобы движение происходило только по горизонтали.
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        // Читаем значения движения с клавиатуры (стрелки)
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

        // Ограничиваем положение камеры по X и Z, чтобы она не выходила за пределы сгенерированной области.
        float minX = 0f;
        float maxX = gridResolution * unitSize;
        float minZ = 0f;
        float maxZ = gridResolution * unitSize;
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);

        transform.position = newPos;
    }

    /// <summary>
    /// Поворот камеры с помощью клавиш WASD:
    /// W/S - изменяют вертикальный угол (pitch) с ограничением,
    /// A/D - изменяют горизонтальный угол (yaw).
    /// </summary>
    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.W))
        {
            // Клавиша W снижает pitch (камера смотрит выше)
            pitch -= rotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            // Клавиша S увеличивает pitch (камера смотрит ниже)
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

        // Ограничиваем вертикальный угол, чтобы избежать излишнего наклона
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
