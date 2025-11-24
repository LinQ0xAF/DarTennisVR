using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class NetworkDartPoolManager : NetworkBehaviour, INetworkPrefabInstanceHandler
{
    [Header("Network Prefab")]
    public GameObject NetworkDartPrefab; // NetworkObject 필수
    private IObjectPool<NetworkObject> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<NetworkObject>(
            createFunc: () => Instantiate(NetworkDartPrefab).GetComponent<NetworkObject>(),
            actionOnGet: (no) => no.gameObject.SetActive(true),
            actionOnRelease: (no) => { no.gameObject.SetActive(false); no.transform.parent = null; },
            actionOnDestroy: (no) => Destroy(no.gameObject),
            defaultCapacity: 20, maxSize: 50
        );
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.PrefabHandler.AddHandler(NetworkDartPrefab, this);
    }
    
    public override void OnNetworkDespawn()
    {
        if(NetworkManager.Singleton != null)
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(NetworkDartPrefab);
    }

    // [Server] 외부 요청 처리
    public void Server_SpawnProjectile(ulong throwerId, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        if (!IsServer) return;

        NetworkObject netDart = _pool.Get();
        netDart.transform.position = pos;
        netDart.transform.rotation = rot;
        netDart.Spawn(); // 클라이언트들에게 스폰 명령

        // 물리 초기화 명령 (던진 사람 ID 전달)
        var script = netDart.GetComponent<NetworkDart>();
        if (script != null) script.InitializeClientRpc(throwerId, vel, angVel);
    }

    // Interface
    public NetworkObject Instantiate(ulong id, Vector3 p, Quaternion r) => _pool.Get();
    public void Destroy(NetworkObject no) => _pool.Release(no);
}