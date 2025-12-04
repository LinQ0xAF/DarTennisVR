// // 서버에 붙는 간단한 매칭 대기 스크립트
// using Unity.Netcode;
// using UnityEngine;
// using System.Collections.Generic;
// using System.Collections;
// public class LocalMatchmaker : NetworkBehaviour
// {
   
//     private RoomConfigDto pendingConfig; // 호스트가 가진 config 기준
//     private string pendingSceneName;
//     private HashSet<ulong> readyClients = new HashSet<ulong>(); // 중복 Ready 방지

//     [Header("references")]
//     [SerializeField] private MultiMatchLoader matchLoader; // 같은 오브젝트나 씬 상의 컨트롤러 참조

//     void Awake()
//     {
//         // 인스펙터에서 비워뒀다면 동일 오브젝트에서 자동 검색
//         if (matchLoader == null)
//             matchLoader = GetComponent<MultiMatchLoader>();
//     }

//     [ServerRpc(RequireOwnership = false)]
//     public void ReadyServerRpc(int balloon, int timeLimit, int setCount, string sceneName, ServerRpcParams rpcParams = default) 
//     {
//         if (matchLoader == null)
//         {
//             Debug.LogError("LocalMatchmaker: matchLoader 참조가 없습니다. 같은 오브젝트에 붙이거나 인스펙터에서 할당하세요.", this);
//             return;
//         }
       
//         ulong senderId = rpcParams.Receive.SenderClientId; //

//         // 첫 번째 도착한 config(호스트)를 기준으로 객체를 생성하고 방을 만듦
//         if (pendingConfig == null) 
//         {
//             pendingConfig = new RoomConfigDto
//             {
//                 balloonCount = balloon,
//                 timeLimitSeconds = timeLimit,
//                 setCount = setCount
//             };
//             pendingSceneName = sceneName;
//             readyClients.Clear();
//             readyClients.Add(senderId);
//             Debug.Log($"[LocalMatchmaker] 첫 Ready 도착 (clientId:{senderId}) => Scene:{sceneName}, Balloons:{balloon}, Time:{timeLimit}", this);
//             return;
//         }

//         // 기존 config와 불일치하면 리셋하고 로그만 남김
//         if (!ConfigsMatch(pendingConfig, balloon, timeLimit, setCount, sceneName))
//         {
//             Debug.LogWarning($"[LocalMatchmaker] 설정 불일치로 Ready 거절 및 접속 해제. sender:{senderId} /scene:{pendingSceneName}/{pendingConfig.balloonCount}/{pendingConfig.timeLimitSeconds}/{pendingConfig.setCount} vs/{sceneName}/{balloon}/{timeLimit}/{setCount}", this);
//             // 미스매치한 클라이언트는 접속 종료(방에 못 들어오게)
//             if (NetworkManager != null)
//             {
//                 NetworkManager.DisconnectClient(senderId);
//             }
//             return;
//         }

//         // 동일 클라이언트의 중복 Ready는 무시
//         if (readyClients.Contains(senderId))
//         {
//             Debug.Log($"[LocalMatchmaker] 중복 Ready 무시 (clientId:{senderId})", this);
//             return;
//         }

//         readyClients.Add(senderId);

//         if (readyClients.Count >= 2) // 두 명 모이면
//         {
//             matchLoader.StartMatch(pendingConfig, pendingSceneName); 
//             pendingConfig = null;
//             pendingSceneName = null;
//             readyClients.Clear();
//         }
//     }

//     [ServerRpc(RequireOwnership = false)]
//     public void CancelReadyServerRpc(ServerRpcParams rpcParams = default)
//     {
//         ulong senderId = rpcParams.Receive.SenderClientId;
//         if (readyClients.Remove(senderId))
//         {
//             Debug.Log($"[LocalMatchmaker] Ready 취소 (sender:{senderId})", this);
//         }

//         // 모두 취소되면 config도 초기화
//         if (readyClients.Count == 0)
//         {
//             pendingConfig = null;
//             pendingSceneName = null;
//         }
//     }

//     private bool ConfigsMatch(RoomConfigDto a, int balloon, int timeLimitSeconds, int setCount, string sceneName)
//     {
//         if (a == null) return false;
//         return a.balloonCount == balloon &&
//                a.timeLimitSeconds == timeLimitSeconds &&
//                a.setCount == setCount &&
//                pendingSceneName == sceneName;
//     }
//     /// <summary>
//     /// UI에서 호출: 네트워크 시작(Host/Client) + Ready 등록까지 내부에서 처리.
//     /// 코루틴인 이유: 네트워크 시작(호스트/클라) 후 NetworkObject가 스폰될 때까지 프레임 단위로 대기하기 위함.
//     /// UI는 입력값만 DTO로 만들어 StartCoroutine(BeginLocalMatch(cfg, sceneName))로 호출하면 된다.
//     /// </summary>
//     public IEnumerator BeginLocalMatch(RoomConfigDto config, string sceneName)
//     {
//         var nm = NetworkManager.Singleton;
//         if (nm == null)
//         {
//             Debug.LogError("LocalMatchmaker: NetworkManager Singleton을 찾을 수 없습니다.", this);
//             yield break;
//         }

//         // 첫 번째 진입자는 Host 시도, 실패 시 Client로 전환
//         if (!nm.IsListening && !nm.IsServer && !nm.IsClient)
//         {
//             if (!nm.StartHost())
//             {
//                 Debug.LogWarning("[LocalMatchmaker] StartHost 실패, Client로 재시도", this);
//                 if (!nm.StartClient())
//                 {
//                     Debug.LogError("LocalMatchmaker: StartClient까지 실패", this);
//                     yield break;
//                 }
//                 Debug.Log("[LocalMatchmaker] Client로 시작합니다.", this);
//             }
//             else
//             {
//                 Debug.Log("[LocalMatchmaker] Host로 시작합니다.", this);
//             }
//         }
//         else if (!nm.IsServer && !nm.IsClient)
//         {
//             if (!nm.StartClient())
//             {
//                 Debug.LogError("LocalMatchmaker: StartClient 실패", this);
//                 yield break;
//             }
//             Debug.Log("[LocalMatchmaker] Client로 시작합니다.", this);
//         }

//         // NetworkObject 스폰 대기(이미 스폰돼 있으면 바로 진행)
//         float timeout = Time.time + 5f;
//         while (!IsSpawned && Time.time < timeout)
//         {
//             yield return null;
//         }

//         if (!IsSpawned)
//         {
//             Debug.LogWarning("LocalMatchmaker: NetworkObject 스폰을 기다렸지만 실패했습니다.", this);
//             yield break;
//         }

//         // Ready 등록
//         CancelReadyServerRpc();
//         pendingSceneName = sceneName;
//         Debug.Log($"[LocalMatchmaker] 로컬 매칭 대기 등록 - Scene:{sceneName} Balloons:{config.balloonCount}, Time:{config.timeLimitSeconds}s", this);
//         ReadyServerRpc(config.balloonCount, config.timeLimitSeconds,config.setCount, sceneName);
//        // Netcode RPC는 “직렬화 가능한 값 타입”만 인자로 받을 수 있어서 
//        // Dto에 Serializable 을 붙여도 소용이 없음
//        //RoomConfigDto를 struct로 바꾸고 INetworkSerializable을 구현하거나 NetworkSerializeByMemcpy를 붙여서 Netcode가 직렬화할 수 있게 만들고,
//        //RPC 시그니처를 ReadyServerRpc(RoomConfigDto dto, ServerRpcParams rpcParams = default)처럼 변경
//     }


// }
