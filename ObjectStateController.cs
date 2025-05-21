using UnityEngine;

public enum ObjectState
{
    Good,    // Зеленое – объект в хорошем состоянии
    Warning, // Желтое – объект в режиме ворнинга
    Error    // Красное – объект имеет сбой
}

public class ObjectStateController : MonoBehaviour
{
    // Текущее состояние объекта
    [SerializeField]
    private ObjectState currentState = ObjectState.Good;

    // Цвета для каждого состояния (можно менять в инспекторе)
    public Color goodColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;

    // Можно добавить ссылку на Renderer, если на префабе несколько рендереров – тогда потребуется доработка
    private Renderer objRenderer;

    void Awake()
    {
        objRenderer = GetComponent<Renderer>();
        SetState(ObjectState.Good); // изначально всегда хорошее (зеленое)
    }

    /// <summary>
    /// Задает новое состояние и обновляет внешний вид объекта.
    /// </summary>
    public void SetState(ObjectState newState)
    {
        currentState = newState;
        UpdateColor();
    }

    public ObjectState GetState()
    {
        return currentState;
    }

    /// <summary>
    /// Обновляет материал объекта в зависимости от состояния.
    /// </summary>
    private void UpdateColor()
    {
        if (objRenderer != null)
        {
            switch (currentState)
            {
                case ObjectState.Good:
                    objRenderer.material.color = goodColor;
                    break;
                case ObjectState.Warning:
                    objRenderer.material.color = warningColor;
                    break;
                case ObjectState.Error:
                    objRenderer.material.color = errorColor;
                    break;
            }
        }
    }

    /// <summary>
    /// Если объект находится в состоянии Warning или Error, по клику сбрасываем в Good.
    /// Убедитесь, что у объекта есть Collider.
    /// </summary>
    void OnMouseDown()
    {
        // Можно добавить проверку, чтобы это не работало при нажатии по UI (если используется EventSystem)
        if (currentState == ObjectState.Warning || currentState == ObjectState.Error)
        {
            SetState(ObjectState.Good);
        }
    }
}
