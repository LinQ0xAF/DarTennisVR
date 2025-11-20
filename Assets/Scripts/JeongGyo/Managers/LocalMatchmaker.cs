// 서버에 붙는 간단한 매칭 대기 스크립트
using Unity.Netcode;
public class LocalMatchmaker : NetworkBehaviour
{
    private int readyCount;
    private MultiRoomNetController.RoomConfig pendingConfig; // 호스트가 가진 config 기준

    [ServerRpc(RequireOwnership = false)]
    public void ReadyServerRpc(int balloon, int timeLimit, string sceneName)
    {
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
            GetComponent<MultiRoomNetController>().StartMatch(pendingConfig);
            readyCount = 0;
            pendingConfig = null;
        }
    }
}
