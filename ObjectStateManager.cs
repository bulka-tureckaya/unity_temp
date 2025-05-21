using System.Collections;
using UnityEngine;

public class ObjectStateManager : MonoBehaviour
{
    // Интервал проверки для перехода в состояние Warning (10 сек) и Error (15 сек)
    public float warningInterval = 10f;
    public float errorInterval = 15f;

    // Шансы для переходов (от 0 до 1)
    [Range(0f, 1f)]
    public float warningChance = 0.2f;  // 20% шанс перевода объекта из состояния Good в Warning каждые 10 сек.
    [Range(0f, 1f)]
    public float errorChanceIfGood = 0.05f;      // 5% шанс перехода из Good в Error каждые 15 сек.
    [Range(0f, 1f)]
    public float errorChanceIfWarning = 0.3f;      // 30% шанс перехода из Warning в Error каждые 15 сек.

    void Start()
    {
        // Запускаем корутины, которые периодически меняют состояние объектов
        StartCoroutine(WarningRoutine());
        StartCoroutine(ErrorRoutine());
    }

    IEnumerator WarningRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(warningInterval);
            // Находим все объекты с компонентом ObjectStateController
            ObjectStateController[] objects = FindObjectsOfType<ObjectStateController>();

            foreach (ObjectStateController obj in objects)
            {
                // Если объект в состоянии Good, возможен переход в Warning
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
            // Находим все объекты с компонентом ObjectStateController
            ObjectStateController[] objects = FindObjectsOfType<ObjectStateController>();

            foreach (ObjectStateController obj in objects)
            {
                // Если объект в состоянии Good, шанс перехода в Error равен errorChanceIfGood
                if (obj.GetState() == ObjectState.Good)
                {
                    if (Random.value < errorChanceIfGood)
                    {
                        obj.SetState(ObjectState.Error);
                    }
                }
                // Если объект уже Warning, шанс перехода в Error больше
                else if (obj.GetState() == ObjectState.Warning)
                {
                    if (Random.value < errorChanceIfWarning)
                    {
                        obj.SetState(ObjectState.Error);
                    }
                }
                // Если объект уже Error – можно ничего не делать или по другой логике восстанавливать, если потребуется.
            }
        }
    }
}
