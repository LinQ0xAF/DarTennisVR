using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Dart Object Pool의 공통 기능을 제공하는 추상 클래스
/// </summary>
/// <typeparam name="TDart">pooling할 Dart 컴포넌트 타입</typeparam>
public abstract class DartPoolBase<TDart> : MonoBehaviour where TDart : Component
{
    [Header("Pool Settings")]
    public GameObject DartPrefab;
    public int DefaultCapacity = 12;
    public int MaxPoolSize = 24;

    protected IObjectPool<GameObject> Pool;

    protected virtual void Awake()
    {
        Pool = new ObjectPool<GameObject>(
            createFunc: CreateDart,
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReleaseToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: DefaultCapacity,
            maxSize: MaxPoolSize
        );
    }

#region Public API
    /// <summary>
    /// 풀에서 다트를 꺼내고 위치를 설정
    /// </summary>
    public GameObject GetDart(Vector3 position, Quaternion rotation)
    {
        GameObject dart = Pool.Get();
        dart.transform.SetPositionAndRotation(position, rotation);
        return dart;
    }

    /// <summary>
    /// 풀에 다트를 반납
    /// </summary>
    public void ReleaseDart(GameObject dart)
    {
        Pool.Release(dart);
    }
#endregion

#region Pool Callbacks
    protected virtual GameObject CreateDart()
    {
        GameObject dartInstance = Instantiate(DartPrefab);
        // 자식 클래스에서 다트 컴포넌트 초기화
        InitializeDartComponent(dartInstance);
        return dartInstance;
    }

    protected virtual void OnGetFromPool(GameObject dart)
    {
        dart.SetActive(true);
    }

    protected virtual void OnReleaseToPool(GameObject dart)
    {
        // 손에 잡혀있던 상태로 반납될 수 있으므로 부모를 끊어줌
        dart.transform.SetParent(null);
        dart.SetActive(false);
    }

    protected virtual void OnDestroyPoolObject(GameObject dart)
    {
        Destroy(dart);
    }
#endregion

    /// <summary>
    /// 다트 생성 시 컴포넌트 초기화 (자식 클래스에서 구현)
    /// </summary>
    protected abstract void InitializeDartComponent(GameObject dartInstance);
}
