// using Unity.Netcode;
// using UnityEngine;
// using UnityEngine.Pool;

// public class NetworkDartPoolManager : NetworkBehaviour, INetworkPrefabInstanceHandler
// {
//     [SerializeField] private GameObject _NetworkDartPrefab; 
//     public int DefaultCapacity = 20;
//     public int MaxPoolSize = 60;
//     // public DartSpawnChannelSO _DartSpawnChannel; 
    
//     private IObjectPool<NetworkObject> _NetworkDartPool;

//     private void Awake()
//     {
//         // object pool initialize
//         _NetworkDartPool = new ObjectPool<NetworkObject>(
//             createFunc: CreateDart,
//             actionOnGet: OnGetDart,
//             actionOnRelease: OnReleaseDart,
//             actionOnDestroy: OnDestroyDart,
//             defaultCapacity: DefaultCapacity,
//             maxSize: MaxPoolSize
//         );
//     }

//     public override void OnNetworkSpawn()
//     {
//         NetworkManager.Singleton.PrefabHandler.AddHandler(_NetworkDartPrefab, this);
//     }

//     public override void OnNetworkDespawn()
//     {
//         NetworkManager.Singleton.PrefabHandler.RemoveHandler(_NetworkDartPrefab);
//     }

// #region 1. Server Logic (request handling & spawn command)

//     // public void Server_OnSpawnRequested(ulong ownerClientId, Vector3 pos, Quaternion rot, bool isRightHand, int socketIndex)
//     // {
//     //     if (!IsServer) return; 

//     //     NetworkObject netDart = _NetworkDartPool.Get();
        
//     //     netDart.transform.position = pos;
//     //     netDart.transform.rotation = rot;

//     //     netDart.Spawn();

//     //     if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var client))
//     //     {
//     //         netDart.TrySetParent(client.PlayerObject);
//     //         netDart.GetComponent<NetworkDart>().AttachToHandClientRpc(isRightHand, socketIndex, ownerClientId);
//     //     }
//     // }

//     public void Server_SpawnProjectile(ulong throwerID, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angularVel)
//     {
//         if (!IsServer) return;

//         NetworkObject netDart = _NetworkDartPool.Get();

//         netDart.transform.position = pos;
//         netDart.transform.rotation = rot;

//         netDart.Spawn();

//         var netDartComponent = netDart.GetComponent<NetworkDart>();
//         if(netDartComponent != null)
//         {
//             netDartComponent.InitializeClientRpc(throwerID, vel, angularVel);
//         }
//     }

// #endregion

// #region 2. Interface Implementation

//     // Server Spawn()
//     public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
//     {
//         var netObj = _NetworkDartPool.Get();
//         netObj.transform.position = position;
//         netObj.transform.rotation = rotation;
//         return netObj;
//     }

//     // NGO Despawn()
//     public void Destroy(NetworkObject networkObject)
//     {
//         _NetworkDartPool.Release(networkObject);
//     }

// #endregion

// #region 3. Internal Pooling Logic (Create/Get/Release/Destroy)

//     private NetworkObject CreateDart()
//     {
//         GameObject go = Instantiate(_NetworkDartPrefab);
//         return go.GetComponent<NetworkObject>();
//     }

//     private void OnGetDart(NetworkObject netObj)
//     {
//         netObj.gameObject.SetActive(true);
//         // 물리 초기화 등은 NetworkDart.cs의 OnEnable에서 처리
//     }

//     private void OnReleaseDart(NetworkObject netObj)
//     {
//         netObj.gameObject.SetActive(false);
//         netObj.transform.parent = null; 
//     }

//     private void OnDestroyDart(NetworkObject netObj)
//     {
//         Destroy(netObj.gameObject);
//     }

// #endregion
// }