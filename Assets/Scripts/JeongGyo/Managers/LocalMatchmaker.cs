// 서버에 붙는 간단한 매칭 대기 스크립트
using Unity.Netcode;
using UnityEngine;
public class LocalMatchmaker : NetworkBehaviour
{
    [SerializeField] private MultiRoomNetController roomNetController; // 같은 오브젝트나 씬 상의 컨트롤러 참조

    private int readyCount;
    private MultiRoomNetController.RoomConfig pendingConfig; // 호스트가 가진 config 기준

    void Awake()
    {
        // 인스펙터에서 비워뒀다면 동일 오브젝트에서 자동 검색
        if (roomNetController == null)
            roomNetController = GetComponent<MultiRoomNetController>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReadyServerRpc(int balloon, int timeLimit, string sceneName)
    {
        if (roomNetController == null)
        {
            Debug.LogError("LocalMatchmaker: roomNetController 참조가 없습니다. 같은 오브젝트에 붙이거나 인스펙터에서 할당하세요.", this);
            return;
        }

        readyCount++;
        // 첫 번째 도착한 config를 기준으로 사용(단순화)
        if (pendingConfig == null)
        {
            pendingConfig = new MultiRoomNetController.RoomConfig {
                balloonCount = balloon,
                timeLimitSeconds = timeLimit,
                gamePlaySceneName = sceneName
            };
        }
        if (readyCount >= 2) // 두 명 모이면
        {
            roomNetController.StartMatch(pendingConfig);
            readyCount = 0;
            pendingConfig = null;
        }
    }
}
