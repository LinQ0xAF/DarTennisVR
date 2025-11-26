// 서버에 붙는 간단한 매칭 대기 스크립트 (임시 수정 버전)
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TempLocalMatchmaker : NetworkBehaviour
{
    private RoomConfigDto pendingConfig; // 호스트가 가진 config 기준
    private HashSet<ulong> readyClients = new HashSet<ulong>(); // 중복 Ready 방지

    [Header("references")]
    [SerializeField] private MultiRoomNetController roomNetController; // 같은 오브젝트나 씬 상의 컨트롤러 참조
    [SerializeField] private SimpleNetworkDiscovery discovery; // 자동 연결을 위한 탐색기

    void Awake()
    {
        // 인스펙터에서 비워뒀다면 동일 오브젝트에서 자동 검색
        if (roomNetController == null)
            roomNetController = GetComponent<MultiRoomNetController>();
        
        if (discovery == null)
            discovery = GetComponent<SimpleNetworkDiscovery>();
        
        // 없으면 추가
        if (discovery == null)
            discovery = gameObject.AddComponent<SimpleNetworkDiscovery>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyServerRpc(int balloon, int timeLimit, string sceneName, int setCount, ServerRpcParams rpcParams = default) 
    {
        if (roomNetController == null)
        {
            Debug.LogError("TempLocalMatchmaker: roomNetController 참조가 없습니다. 같은 오브젝트에 붙이거나 인스펙터에서 할당하세요.", this);
            return;
        }
       
        ulong senderId = rpcParams.Receive.SenderClientId; 
        
        var incoming = new RoomConfigDto {
            balloonCount = balloon,
            timeLimitSeconds = timeLimit,
            gamePlaySceneName = sceneName,
            setCount = setCount
        };

        // 첫 번째 도착한 config를 기준으로 사용
        if (pendingConfig == null)
        {
            pendingConfig = incoming;
            readyClients.Clear();
            readyClients.Add(senderId);
            Debug.Log($"[TempLocalMatchmaker] 첫 Ready 도착 (clientId:{senderId}) => Scene:{sceneName}, Balloons:{balloon}, Time:{timeLimit}", this);
            return;
        }

        // 기존 config와 불일치하면 리셋하고 로그만 남김
        if (!ConfigsMatch(pendingConfig, incoming))
        {
            Debug.LogWarning($"[TempLocalMatchmaker] 설정 불일치로 Ready 거절 및 접속 해제. sender:{senderId} 기존:{pendingConfig.gamePlaySceneName}/{pendingConfig.balloonCount}/{pendingConfig.timeLimitSeconds}/{pendingConfig.setCount} vs 새:{sceneName}/{balloon}/{timeLimit}/{setCount}", this);
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
            Debug.Log($"[TempLocalMatchmaker] 중복 Ready 무시 (clientId:{senderId})", this);
            return;
        }

        readyClients.Add(senderId);

        if (readyClients.Count >= 2) // 두 명 모이면
        {
            // 매칭 성사 시 브로드캐스팅 중단 (더 이상 사람 안 받음)
            if (discovery != null) discovery.StopDiscovery();

            roomNetController.StartMatch(pendingConfig);
            pendingConfig = null;
            readyClients.Clear();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CancelReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (readyClients.Remove(senderId))
        {
            Debug.Log($"[TempLocalMatchmaker] Ready 취소 (sender:{senderId})", this);
        }

        // 모두 취소되면 config도 초기화
        if (readyClients.Count == 0)
        {
            pendingConfig = null;
        }
    }

    private bool ConfigsMatch(RoomConfigDto a, RoomConfigDto b) 
    {
        if (a == null || b == null) return false;
        return a.balloonCount == b.balloonCount &&
               a.timeLimitSeconds == b.timeLimitSeconds &&
               a.gamePlaySceneName == b.gamePlaySceneName &&
               a.setCount == b.setCount;
    }

    /// <summary>
    /// UI에서 호출: 네트워크 시작(Host/Client) + Ready 등록까지 내부에서 처리.
    /// </summary>
    public IEnumerator BeginLocalMatch(RoomConfigDto config)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("TempLocalMatchmaker: NetworkManager Singleton을 찾을 수 없습니다.", this);
            yield break;
        }

        // 이미 연결된 상태라면 바로 Ready 진행
        if (nm.IsClient || nm.IsServer)
        {
            Debug.Log("[TempLocalMatchmaker] 이미 연결됨. 바로 Ready 진행.");
        }
        else
        {
            // 1. 먼저 주변에 방이 있는지 탐색 (2초간)
            string foundServerIp = null;
            bool searchComplete = false;

            Debug.Log("[TempLocalMatchmaker] 주변 방 탐색 시작...");
            discovery.SearchForServer((ip) => {
                foundServerIp = ip;
                searchComplete = true;
            });

            // 탐색 대기
            while (!searchComplete) yield return null;

            if (!string.IsNullOrEmpty(foundServerIp))
            {
                // 2. 방을 찾음 -> Client로 접속
                Debug.Log($"[TempLocalMatchmaker] 방 발견! ({foundServerIp}) 접속을 시도합니다.");
                
                // UnityTransport IP 설정
                var transport = nm.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = foundServerIp;
                }
                
                if (nm.StartClient())
                {
                    Debug.Log("[TempLocalMatchmaker] Client 시작 성공");
                }
                else
                {
                    Debug.LogError("[TempLocalMatchmaker] Client 시작 실패");
                    yield break;
                }
            }
            else
            {
                // 3. 방이 없음 -> Host로 시작
                Debug.Log("[TempLocalMatchmaker] 방을 찾지 못함. Host로 시작합니다.");
                if (nm.StartHost())
                {
                    // 호스트 시작 성공 시 브로드캐스팅 시작 (다른 사람이 찾을 수 있게)
                    discovery.StartBroadcasting();
                }
                else
                {
                    Debug.LogError("[TempLocalMatchmaker] Host 시작 실패");
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
            Debug.LogWarning("TempLocalMatchmaker: NetworkObject 스폰을 기다렸지만 실패했습니다.", this);
            yield break;
        }

        // Ready 등록
        CancelReadyServerRpc();
        Debug.Log($"[TempLocalMatchmaker] 로컬 매칭 대기 등록 - Scene:{config.gamePlaySceneName}, Balloons:{config.balloonCount}, Time:{config.timeLimitSeconds}s", this);
        ReadyServerRpc(config.balloonCount, config.timeLimitSeconds, config.gamePlaySceneName, config.setCount);
    }
}
