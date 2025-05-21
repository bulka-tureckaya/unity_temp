using System.Collections;
using UnityEngine;

public class ObjectStateManager : MonoBehaviour
{
    // �������� �������� ��� �������� � ��������� Warning (10 ���) � Error (15 ���)
    public float warningInterval = 10f;
    public float errorInterval = 15f;

    // ����� ��� ��������� (�� 0 �� 1)
    [Range(0f, 1f)]
    public float warningChance = 0.2f;  // 20% ���� �������� ������� �� ��������� Good � Warning ������ 10 ���.
    [Range(0f, 1f)]
    public float errorChanceIfGood = 0.05f;      // 5% ���� �������� �� Good � Error ������ 15 ���.
    [Range(0f, 1f)]
    public float errorChanceIfWarning = 0.3f;      // 30% ���� �������� �� Warning � Error ������ 15 ���.

    void Start()
    {
        // ��������� ��������, ������� ������������ ������ ��������� ��������
        StartCoroutine(WarningRoutine());
        StartCoroutine(ErrorRoutine());
    }

    IEnumerator WarningRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(warningInterval);
            // ������� ��� ������� � ����������� ObjectStateController
            ObjectStateController[] objects = FindObjectsOfType<ObjectStateController>();

            foreach (ObjectStateController obj in objects)
            {
                // ���� ������ � ��������� Good, �������� ������� � Warning
                if (obj.GetState() == ObjectState.Good)
                {
                    if (Random.value < warningChance)
                    {
                        obj.SetState(ObjectState.Warning);
                    }
                }
            }
        }
    }

    IEnumerator ErrorRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(errorInterval);
            // ������� ��� ������� � ����������� ObjectStateController
            ObjectStateController[] objects = FindObjectsOfType<ObjectStateController>();

            foreach (ObjectStateController obj in objects)
            {
                // ���� ������ � ��������� Good, ���� �������� � Error ����� errorChanceIfGood
                if (obj.GetState() == ObjectState.Good)
                {
                    if (Random.value < errorChanceIfGood)
                    {
                        obj.SetState(ObjectState.Error);
                    }
                }
                // ���� ������ ��� Warning, ���� �������� � Error ������
                else if (obj.GetState() == ObjectState.Warning)
                {
                    if (Random.value < errorChanceIfWarning)
                    {
                        obj.SetState(ObjectState.Error);
                    }
                }
                // ���� ������ ��� Error � ����� ������ �� ������ ��� �� ������ ������ ���������������, ���� �����������.
            }
        }
    }
}
