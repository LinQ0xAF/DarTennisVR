using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class NetworkDartPoolManager : NetworkBehaviour, INetworkPrefabInstanceHandler
{
    // [삭제됨] public static DartPoolManager Instance { get; private set; } 

    [SerializeField] private GameObject _DartPrefab; 
    public int DefaultCapacity = 20;
    public int MaxPoolSize = 40;
    public DartSpawnChannelSO _DartSpawnChannel; 
    
    private IObjectPool<NetworkObject> _NetworkDartPool;

    private void Awake()
    {
        // [수정] 싱글톤 중복 체크 로직 삭제
        // 대신, 혹시 실수로 매니저를 2개 만들었을 때 경고를 띄우는 정도는 가능
        // if (FindObjectsByType<NetworkDartPoolManager>(FindObjectsSortMode.None).Length > 1)
        // {
        //     Debug.LogError("More than 1 NetworkDartPoolManager instances found in the scene.", this);
        // }

        // object pool initialize
        _NetworkDartPool = new ObjectPool<NetworkObject>(
            createFunc: CreateDart,
            actionOnGet: OnGetDart,
            actionOnRelease: OnReleaseDart,
            actionOnDestroy: OnDestroyDart,
            defaultCapacity: DefaultCapacity,
            maxSize: MaxPoolSize
        );
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.PrefabHandler.AddHandler(_DartPrefab, this);

        if (IsServer && _DartSpawnChannel != null)
        {
            _DartSpawnChannel.OnSpawnRequested += Server_OnSpawnRequested;
        }
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.PrefabHandler.RemoveHandler(_DartPrefab);
        
        if (IsServer && _DartSpawnChannel != null)
        {
            _DartSpawnChannel.OnSpawnRequested -= Server_OnSpawnRequested;
        }
    }

#region 1. Server Logic (request handling & spawn command)

    private void Server_OnSpawnRequested(ulong clientId, Vector3 pos, Quaternion rot, bool isRightHand)
    {
        if (!IsServer) return; 

        NetworkObject netDart = _NetworkDartPool.Get();
        
        netDart.transform.position = pos;
        netDart.transform.rotation = rot;

        netDart.Spawn();

        // 4. 요청한 플레이어의 손에 붙여주기 (Parenting)
        // (NetworkManager.ConnectedClients[requesterId].PlayerObject... 로 손을 찾아서)
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            // 플레이어 구조에 따라 손 오브젝트를 찾는 로직이 필요할 수 있음
            // 예시: 일단 플레이어 루트에 붙이거나, 특정 태그/이름으로 손을 찾아서 부착
            // netDart.TrySetParent(foundHandTransform); 
            
            // *주의: 정확한 손 위치에 붙이려면 HandRoleManager가 
            // 요청 시 손의 NetworkObjectId를 같이 보내주는 것이 더 정확할 수 있습니다.
        }
    }

    #endregion

    #region 2. Interface Implementation (called by NGOs)

    // Server Spawn()
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        var netObj = _NetworkDartPool.Get();
        netObj.transform.position = position;
        netObj.transform.rotation = rotation;
        return netObj;
    }

    // NGO Despawn()
    public void Destroy(NetworkObject networkObject)
    {
        _NetworkDartPool.Release(networkObject);
    }

    #endregion

    #region 3. Internal Pooling Logic (Create/Get/Release/Destroy)

    private NetworkObject CreateDart()
    {
        GameObject go = Instantiate(_DartPrefab);
        return go.GetComponent<NetworkObject>();
    }

    private void OnGetDart(NetworkObject netObj)
    {
        netObj.gameObject.SetActive(true);
        // 물리 초기화 등은 Dart.cs의 OnEnable에서 처리하는 것이 좋음
    }

    private void OnReleaseDart(NetworkObject netObj)
    {
        netObj.gameObject.SetActive(false);
        netObj.transform.parent = null; 
    }

    private void OnDestroyDart(NetworkObject netObj)
    {
        Destroy(netObj.gameObject);
    }

    #endregion
}