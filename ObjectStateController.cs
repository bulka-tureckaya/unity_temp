using UnityEngine;

public enum ObjectState
{
    Good,    // ������� � ������ � ������� ���������
    Warning, // ������ � ������ � ������ ��������
    Error    // ������� � ������ ����� ����
}

public class ObjectStateController : MonoBehaviour
{
    // ������� ��������� �������
    [SerializeField]
    private ObjectState currentState = ObjectState.Good;

    // ����� ��� ������� ��������� (����� ������ � ����������)
    public Color goodColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;

    // ����� �������� ������ �� Renderer, ���� �� ������� ��������� ���������� � ����� ����������� ���������
    private Renderer objRenderer;

    void Awake()
    {
        objRenderer = GetComponent<Renderer>();
        SetState(ObjectState.Good); // ���������� ������ ������� (�������)
    }

    /// <summary>
    /// ������ ����� ��������� � ��������� ������� ��� �������.
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
    /// ��������� �������� ������� � ����������� �� ���������.
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
    /// ���� ������ ��������� � ��������� Warning ��� Error, �� ����� ���������� � Good.
    /// ���������, ��� � ������� ���� Collider.
    /// </summary>
    void OnMouseDown()
    {
        // ����� �������� ��������, ����� ��� �� �������� ��� ������� �� UI (���� ������������ EventSystem)
        if (currentState == ObjectState.Warning || currentState == ObjectState.Error)
        {
            SetState(ObjectState.Good);
        }
    }
}
