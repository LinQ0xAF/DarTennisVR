using UnityEngine;

/// <summary>
/// 싱글플레이용 다트 오브젝트 풀
/// </summary>
public class DartPoolManager : DartPoolBase<Dart>
{
    protected override void InitializeDartComponent(GameObject dartInstance)
    {
        dartInstance.GetComponent<Dart>().SetPoolManager(this);
    }
}
