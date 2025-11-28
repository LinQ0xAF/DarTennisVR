using UnityEngine;

/// <summary>
/// 멀티플레이용 로컬 다트 오브젝트 풀
/// </summary>
public class LocalDartPool : DartPoolBase<LocalDart>
{
    protected override void InitializeDartComponent(GameObject dartInstance)
    {
        dartInstance.GetComponent<LocalDart>().SetPoolManager(this);
    }
}