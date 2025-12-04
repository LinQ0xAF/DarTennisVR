// 서버에 붙는 간단한 매칭 대기 스크립트 (임시 수정 버전)
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class WLANLocalMatchmaker : NetworkBehaviour
{
    // --- Events for UI ---
    public event Action OnMatchmakingStarted;      // 탐색 시작
    public event Action OnWaitingForOpponent;      // 호스트가 되어 대기 중
    public event Action<string> OnJoiningRoom;     // 방 발견, 접속 시도
    public event Action OnMatchFound;              // 매칭 성사 (씬 이동 전)
    public event Action<string> OnMatchFailed;     // 실패 (에러 메시지)
    public event Action OnMatchCancelled;          // 취소됨

    private RoomConfigDto pendingConfig; // 호스트가 가진 config 기준
    private string pendingSceneName;
    private HashSet<ulong> readyClients = new HashSet<ulong>(); // 중복 Ready 방지

    [Header("references")]
    [SerializeField] private MultiMatchLoader matchLoader; // 같은 오브젝트나 씬 상의 컨트롤러 참조
    [SerializeField] private SimpleNetworkDiscovery discovery; // 자동 연결을 위한 탐색기

    void Awake()
    {
        // 인스펙터에서 비워뒀다면 동일 오브젝트에서 자동 검색
        if (matchLoader == null)
            matchLoader = GetComponent<MultiMatchLoader>();
        
        if (discovery == null)
            discovery = GetComponent<SimpleNetworkDiscovery>();
        
        // 없으면 추가
        if (discovery == null)
            discovery = gameObject.AddComponent<SimpleNetworkDiscovery>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyServerRpc(int balloon, int timeLimit,int setCount, string sceneName, ServerRpcParams rpcParams = default) 
    {
        if (matchLoader == null)
        {
            Debug.LogError("WLANLocalMatchmaker: matchLoader 참조가 없습니다. 같은 오브젝트에 붙이거나 인스펙터에서 할당하세요.", this);
            return;
        }
       
        ulong senderId = rpcParams.Receive.SenderClientId; 
        
        var incoming = new RoomConfigDto {
            balloonCount = balloon,
            timeLimitSeconds = timeLimit,
            setCount = setCount
        };
        pendingSceneName = pendingSceneName ?? sceneName;

        // 첫 번째 도착한 config를 기준으로 사용
        if (pendingConfig == null)
        {
            pendingConfig = incoming;
            readyClients.Clear();
            readyClients.Add(senderId);
            Debug.Log($"[WLANLocalMatchmaker] 첫 Ready 도착 (clientId:{senderId}) =>Scene:{sceneName} Balloons:{balloon}, Time:{timeLimit}", this);
            return;
        }

        // 기존 config와 불일치하면 리셋하고 로그만 남김
        if (!ConfigsMatch(pendingConfig, incoming, sceneName))
        {
            Debug.LogWarning($"[WLANLocalMatchmaker] 설정 불일치로 Ready 거절 및 접속 해제. sender:{senderId} 기존:{pendingConfig.balloonCount}/{pendingConfig.timeLimitSeconds}/{pendingConfig.setCount} vs 새:/{balloon}/{timeLimit}/{setCount}", this);
            // 미스매치한 클라이언트는 접속 종료(방에 못 들어오게)
            if (NetworkManager != null)
            {
                NetworkManager.DisconnectClient(senderId);
            }
            return;
        }

        // 동일 클라이언트의 중복 Ready는 무시
        if (readyClients.Contains(senderId))
        {
            Debug.Log($"[WLANLocalMatchmaker] 중복 Ready 무시 (clientId:{senderId})", this);
            return;
        }

        readyClients.Add(senderId);

        if (readyClients.Count >= 2) // 두 명 모이면
        {
            // 매칭 성사 시 브로드캐스팅 중단 (더 이상 사람 안 받음)
            if (discovery != null) discovery.StopDiscovery();

            // UI 알림 (Host 측)
            OnMatchFound?.Invoke();
            MatchFoundClientRpc();

            matchLoader.StartMatch(pendingConfig, pendingSceneName);
            pendingConfig = null;
            pendingSceneName = null;
            readyClients.Clear();
        }
    }

    [ClientRpc]
    private void MatchFoundClientRpc()
    {
        if (!IsHost) OnMatchFound?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CancelReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (readyClients.Remove(senderId))
        {
            Debug.Log($"[WLANLocalMatchmaker] Ready 취소 (sender:{senderId})", this);
        }

        // 모두 취소되면 config도 초기화
        if (readyClients.Count == 0)
        {
            pendingConfig = null;
            pendingSceneName = null;
        }
    }

    private bool ConfigsMatch(RoomConfigDto a, RoomConfigDto b, string sceneName) 
    {
        if (a == null || b == null) return false;
        return a.balloonCount == b.balloonCount &&
               a.timeLimitSeconds == b.timeLimitSeconds &&
               a.setCount == b.setCount &&
               pendingSceneName == sceneName; // sceneName은 ReadyServerRpc 인자로 동일해야 함
    }

    /// <summary>
    /// UI에서 호출: 네트워크 시작(Host/Client) + Ready 등록까지 내부에서 처리.
    /// </summary>
    public IEnumerator BeginLocalMatch(RoomConfigDto config, string sceneName)
    {
        OnMatchmakingStarted?.Invoke();

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("WLANLocalMatchmaker: NetworkManager Singleton을 찾을 수 없습니다.", this);
            OnMatchFailed?.Invoke("NetworkManager Missing");
            yield break;
        }

        // 이미 연결된 상태라면 바로 Ready 진행
        if (nm.IsClient || nm.IsServer)
        {
            Debug.Log("[WLANLocalMatchmaker] 이미 연결됨. 바로 Ready 진행.");
        }
        else
        {
            // 1. 먼저 주변에 방이 있는지 탐색 (2초간)
            string foundServerIp = null;
            string username = "Placeholder Player";
            bool searchComplete = false;

            Debug.Log("[WLANLocalMatchmaker] 주변 방 탐색 시작...");
            discovery.SearchForServer((ip) => {
                foundServerIp = ip;
                searchComplete = true;
            });

            // 탐색 대기
            while (!searchComplete) yield return null;

            if (!string.IsNullOrEmpty(foundServerIp))
            {
                // 2. 방을 찾음 -> Client로 접속
#if UNITY_EDITOR
                Debug.Log($"[WLANLocalMatchmaker] 방 발견! ({foundServerIp}) 접속을 시도합니다.");
                username = foundServerIp;
#endif
                OnJoiningRoom?.Invoke(username);
                
                // UnityTransport IP 설정
                var transport = nm.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = foundServerIp;
                }
                
                if (nm.StartClient())
                {
                    Debug.Log("[WLANLocalMatchmaker] Client 시작 성공");
                }
                else
                {
                    Debug.LogError("[WLANLocalMatchmaker] Client 시작 실패");
                    OnMatchFailed?.Invoke("Client Start Failed");
                    yield break;
                }
            }
            else
            {
                // 3. 방이 없음 -> Host로 시작
                Debug.Log("[WLANLocalMatchmaker] 방을 찾지 못함. Host로 시작합니다.");
                if (nm.StartHost())
                {
                    // 호스트 시작 성공 시 브로드캐스팅 시작 (다른 사람이 찾을 수 있게)
                    discovery.StartBroadcasting();
                    OnWaitingForOpponent?.Invoke();
                }
                else
                {
                    Debug.LogError("[WLANLocalMatchmaker] Host 시작 실패");
                    OnMatchFailed?.Invoke("Host Start Failed");
                    yield break;
                }
            }
        }

        // NetworkObject 스폰 대기(이미 스폰돼 있으면 바로 진행)
        float timeout = Time.time + 5f;
        while (!IsSpawned && Time.time < timeout)
        {
            yield return null;
        }

        if (!IsSpawned)
        {
            Debug.LogWarning("WLANLocalMatchmaker: NetworkObject 스폰을 기다렸지만 실패했습니다.", this);
            OnMatchFailed?.Invoke("Network Spawn Timeout");
            yield break;
        }

        // Ready 등록
        CancelReadyServerRpc();
        pendingSceneName = sceneName;
        Debug.Log($"[WLANLocalMatchmaker] 로컬 매칭 대기 등록 - Scene:{sceneName} Balloons:{config.balloonCount}, Time:{config.timeLimitSeconds}s", this);
        ReadyServerRpc(config.balloonCount, config.timeLimitSeconds, config.setCount, sceneName);
    }

    /// <summary>
    /// 매칭 취소 (UI 버튼 등에서 호출)
    /// </summary>
    public void CancelMatchmaking()
    {
        if (IsSpawned)
        {
            CancelReadyServerRpc();
        }

        if (discovery != null) discovery.StopDiscovery();
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        OnMatchCancelled?.Invoke();
    }
}
