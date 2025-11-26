using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 멀티 방 생성 시 서버가 RoomConfig를 전파하고 네트워크 씬을 로드하는 최소 컨트롤러.
/// 매칭/로비는 외부에서 처리한다고 가정한다.
/// </summary>
public class MultiRoomNetController : NetworkBehaviour
{
    private static RoomConfigDto pendingConfig; // 씬 진입 후 초기화용으로 소비할 설정
    private static float pendingServerMatchStartTime; // 씬 진입 후 초기화용으로 소비할 서버 시작 시각
    private static bool hasPendingServerMatchStartTime; // 시작 시각이 유효한지 여부
    private float serverMatchStartTime; // 서버가 브로드캐스트한 매치 시작 시각

    private void SetStartTime(float startTime)
    {
        serverMatchStartTime = startTime;
        pendingServerMatchStartTime = startTime;
        hasPendingServerMatchStartTime = true;
    }

    public static void SetPendingConfig(RoomConfigDto cfg)
    {
        pendingConfig = cfg;
    }
    public static bool TryConsumePendingConfig(out RoomConfigDto cfg)
    {
        if (pendingConfig == null)
        {
            cfg = null;
            return false;
        }

        cfg = pendingConfig;
        pendingConfig = null;
        return true;
    }
    [ClientRpc]
    void ConfigBroadcastClientRpc(string sceneName, int balloonCount, int timeLimitSeconds, int setCount)
    {
        // 각 클라이언트에 RoomConfig를 저장한다. 씬 진입 후 MultiGameplayInitializer가 소비한다.
        var cfg = new RoomConfigDto
        {
            gamePlaySceneName = sceneName,
            balloonCount = balloonCount,
            timeLimitSeconds = timeLimitSeconds,
            setCount = setCount
        };
        SetPendingConfig(cfg);
    }

    [ClientRpc]
    void MatchStartTimeClientRpc(double startServerTime)
    {
        // 타이머 동기화용 시작 시각을 전달. 실제 타이머에서 NetworkManager.ServerTime과 비교해 사용.
        SetStartTime((float)startServerTime);
    }
    public static bool TryConsumePendingStartTime(out float startTime) // 서버가 브로드캐스트한 매치 시작 시각 가져오기
    {
        if (!hasPendingServerMatchStartTime)
        {
            startTime = 0f;
            return false;
        }

        startTime = pendingServerMatchStartTime;
        hasPendingServerMatchStartTime = false;
        return true;
    }
    /// <summary>
    /// 서버가 호출: 설정 전파 후 네트워크 씬 로드.
    /// 매칭이 완료되어 두 클라이언트가 준비된 상태에서만 호출한다.
    /// </summary>
    public void StartMatch(RoomConfigDto cfg)
    {
        if (!IsServer)
        {
            Debug.LogWarning("StartMatch는 서버/호스트만 호출할 수 있습니다.", this);
            return;
        }

        if (cfg == null || string.IsNullOrWhiteSpace(cfg.gamePlaySceneName))
        {
            Debug.LogError("StartMatch: 유효하지 않은 RoomConfig", this);
            return;
        }

        // 1) 룸 설정 전파
        ConfigBroadcastClientRpc(cfg.gamePlaySceneName, cfg.balloonCount, cfg.timeLimitSeconds, cfg.setCount);

        // 2) 시작 시각 동기화 전파(서버 시간 사용)
        double now = NetworkManager.ServerTime.Time;
        SetStartTime((float)now);
        MatchStartTimeClientRpc(now);

        // 3) 네트워크 씬 로드(모든 클라가 따라옴)
        NetworkManager.SceneManager.LoadScene(cfg.gamePlaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    /// <summary>
    /// 클라이언트/서버 공통: 서버가 브로드캐스트한 시작 시각을 가져오는 헬퍼.
    /// </summary>
    public float GetServerMatchStartTime() => serverMatchStartTime;
}
