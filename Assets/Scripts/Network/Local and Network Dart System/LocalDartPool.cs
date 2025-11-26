using UnityEngine;
using UnityEngine.Pool;

public class LocalDartPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("XRI 컴포넌트(XRGrabInteractable)가 붙어있는 로컬용 프리팹")]
    public GameObject LocalDartPrefab; 
    public int DefaultCapacity = 10;
    public int MaxPoolSize = 20;

    private IObjectPool<GameObject> _Pool;

    private void Awake()
    {
        // 풀 초기화
        _Pool = new ObjectPool<GameObject>(
            createFunc: CreateDart,
            actionOnGet: OnGetDart,
            actionOnRelease: OnReleaseDart,
            actionOnDestroy: OnDestroyDart,
            collectionCheck: true, // 동일한 오브젝트 중복 반납 방지 체크
            defaultCapacity: DefaultCapacity,
            maxSize: MaxPoolSize
        );
    }

    // --- Public API ---
    public GameObject GetDart(Vector3 position, Quaternion rotation)
    {
        GameObject dart = _Pool.Get();
        dart.transform.SetPositionAndRotation(position, rotation);
        return dart;
    }

    public void ReleaseDart(GameObject dart)
    {
        _Pool.Release(dart);
    }

    // --- 풀링 콜백 함수들 ---

    private GameObject CreateDart()
    {
        // 1. 생성
        GameObject dart = Instantiate(LocalDartPrefab);
        // 매니저 할당
        dart.GetComponent<LocalDart>().SetPoolManager(this);
        return dart;
    }

    private void OnGetDart(GameObject dart)
    {
        // 2. 꺼낼 때: 활성화
        dart.SetActive(true);
    }

    private void OnReleaseDart(GameObject dart)
    {
        // 3. 반납할 때: 비활성화 및 부모 해제 (중요!)
        // 손에 잡혀있던 상태로 반납될 수 있으므로 부모를 끊어줘야 함
        dart.transform.SetParent(null);
        dart.SetActive(false);
    }

    private void OnDestroyDart(GameObject dart)
    {
        // 4. 풀이 꽉 찼을 때: 파괴
        Destroy(dart);
    }
}