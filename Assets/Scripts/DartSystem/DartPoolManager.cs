using UnityEngine;
using UnityEngine.Pool;

public class DartPoolManager : MonoBehaviour
{
    public GameObject DartPrefab;
    public int InitialPoolSize = 10;
    public int MaxPoolSize = 20;

    private IObjectPool<GameObject> _DartPool;

    private void Awake()
    {
        // ObjectPool 초기화
        _DartPool = new ObjectPool<GameObject>(
            createFunc: CreateDart,
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReleaseToPool,
            actionOnDestroy: OnDestroyInPool,
            collectionCheck: true, // 중복 반납 체크
            defaultCapacity: InitialPoolSize,
            maxSize: MaxPoolSize
        );
    }

#region Pool_Public_API
    // 풀에서 다트를 꺼내고 위치를 설정하는 공개 함수
    public GameObject GetDart(Vector3 position, Quaternion rotation)
    {
        GameObject dart = _DartPool.Get();
        dart.transform.SetPositionAndRotation(position, rotation);
        return dart;
    }

    // 풀에 다트를 반납하는 공개 함수
    public void ReleaseDart(GameObject dart)
    {
        _DartPool.Release(dart);
    }
#endregion

#region Pool_Callbacks
    private GameObject CreateDart()
    {
        GameObject dartInstance = Instantiate(DartPrefab);
        dartInstance.GetComponent<Dart>().SetPoolManager(this);
        return dartInstance;
    }

    private void OnGetFromPool(GameObject dart)
    {
        dart.SetActive(true);
    }

    private void OnReleaseToPool(GameObject dart)
    {
        dart.SetActive(false);
    }

    private void OnDestroyInPool(GameObject dart)
    {
        Destroy(dart);
    }
#endregion

}
