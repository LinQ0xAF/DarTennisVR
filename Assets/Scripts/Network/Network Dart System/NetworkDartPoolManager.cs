using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class NetworkDartPoolManager : NetworkBehaviour, INetworkPrefabInstanceHandler
{
    // [삭제됨] public static DartPoolManager Instance { get; private set; } 

    [SerializeField] private GameObject _DartPrefab; 
    public int DefaultCapacity = 20;
    public int MaxPoolSize = 60;
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

        // if (IsServer && _DartSpawnChannel != null)
        // {
        //     _DartSpawnChannel.OnSpawnRequested += Server_OnSpawnRequested;
        // }
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.PrefabHandler.RemoveHandler(_DartPrefab);
        
        // if (IsServer && _DartSpawnChannel != null)
        // {
        //     _DartSpawnChannel.OnSpawnRequested -= Server_OnSpawnRequested;
        // }
    }

#region 1. Server Logic (request handling & spawn command)

    public void Server_OnSpawnRequested(ulong clientId, Vector3 pos, Quaternion rot, bool isRightHand)
    {
        if (!IsServer) return; 

        NetworkObject netDart = _NetworkDartPool.Get();
        
        netDart.transform.position = pos;
        netDart.transform.rotation = rot;

        netDart.Spawn();

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
           var playerSetup = client.PlayerObject.GetComponent<NetworkVRPlayerDriver>();
           if (playerSetup != null)
            {
                Transform targetHand = isRightHand ? playerSetup.NetRightHandIKTarget : playerSetup.NetLeftHandIKTarget;
                netDart.TrySetParent(targetHand);
            }
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
        // 물리 초기화 등은 NetworkDart.cs의 OnEnable에서 처리
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