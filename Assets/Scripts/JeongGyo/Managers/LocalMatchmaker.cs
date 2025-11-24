// 서버에 붙는 간단한 매칭 대기 스크립트
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
public class LocalMatchmaker : NetworkBehaviour
{
    [SerializeField] private MultiRoomNetController roomNetController; // 같은 오브젝트나 씬 상의 컨트롤러 참조

    private MultiRoomNetController.RoomConfig pendingConfig; // 호스트가 가진 config 기준
    private HashSet<ulong> readyClients = new HashSet<ulong>(); // 중복 Ready 방지

    void Awake()
    {
        // 인스펙터에서 비워뒀다면 동일 오브젝트에서 자동 검색
        if (roomNetController == null)
            roomNetController = GetComponent<MultiRoomNetController>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyServerRpc(int balloon, int timeLimit, string sceneName, int setCount, ServerRpcParams rpcParams = default)
    {
        if (roomNetController == null)
        {
            Debug.LogError("LocalMatchmaker: roomNetController 참조가 없습니다. 같은 오브젝트에 붙이거나 인스펙터에서 할당하세요.", this);
            return;
        }

        ulong senderId = rpcParams.Receive.SenderClientId;
        var incoming = new MultiRoomNetController.RoomConfig {
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
            Debug.Log($"[LocalMatchmaker] 첫 Ready 도착 (clientId:{senderId}) => Scene:{sceneName}, Balloons:{balloon}, Time:{timeLimit}", this);
            return;
        }

        // 기존 config와 불일치하면 리셋하고 로그만 남김
        if (!ConfigsMatch(pendingConfig, incoming))
        {
            Debug.LogWarning($"[LocalMatchmaker] 설정 불일치로 Ready 거절 및 접속 해제. sender:{senderId} 기존:{pendingConfig.gamePlaySceneName}/{pendingConfig.balloonCount}/{pendingConfig.timeLimitSeconds}/{pendingConfig.setCount} vs 새:{sceneName}/{balloon}/{timeLimit}/{setCount}", this);
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
            Debug.Log($"[LocalMatchmaker] 중복 Ready 무시 (clientId:{senderId})", this);
            return;
        }

        readyClients.Add(senderId);

        if (readyClients.Count >= 2) // 두 명 모이면
        {
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
            Debug.Log($"[LocalMatchmaker] Ready 취소 (sender:{senderId})", this);
        }

        // 모두 취소되면 config도 초기화
        if (readyClients.Count == 0)
        {
            pendingConfig = null;
        }
    }

    private bool ConfigsMatch(MultiRoomNetController.RoomConfig a, MultiRoomNetController.RoomConfig b)
    {
        if (a == null || b == null) return false;
        return a.balloonCount == b.balloonCount &&
               a.timeLimitSeconds == b.timeLimitSeconds &&
               a.gamePlaySceneName == b.gamePlaySceneName &&
               a.setCount == b.setCount;
    }
}
