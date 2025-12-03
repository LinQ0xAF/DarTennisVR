using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 멀티 방 생성 시 서버가 RoomConfig를 전파하고 네트워크 씬을 로드하는 최소 컨트롤러.
/// 매칭/로비는 외부에서 처리한다고 가정한다.
/// </summary>
public class MultiMatchLoader : NetworkBehaviour
{      
    [SerializeField] private RoomConfigSO roomConfig; // 프리셋+런타임 상태를 함께 들고 있는 SO

    [ClientRpc]
    void ConfigBroadcastClientRpc( int balloonCount, int timeLimitSeconds, int setCount)
    {
        // 각 클라이언트에 RoomConfig를 저장한다. 씬 진입 후 GameManager 등이 소비한다.
        var cfg = new RoomConfigDto
        {
            balloonCount = balloonCount,
            timeLimitSeconds = timeLimitSeconds,
            setCount = setCount
        };
        if (roomConfig != null)
            roomConfig.SetRuntime(cfg); 
    }

    /// <summary>
    /// 서버가 호출: 설정 전파 후 네트워크 씬 로드.
    /// 매칭이 완료되어 두 클라이언트가 준비된 상태에서만 호출한다.
    /// </summary>
    public void StartMatch(RoomConfigDto cfg, string sceneName)
    {
        if (!IsServer)
        {
            Debug.LogWarning("StartMatch는 서버/호스트만 호출할 수 있습니다.", this);
            return;
        }

        if (cfg == null || string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("StartMatch: 유효하지 않은 RoomConfig", this);
            return;
        }

        if (roomConfig == null)
            Debug.LogWarning("MultiMatchLoader: RoomConfigSO가 할당되지 않아 설정을 공유하지 못합니다.", this);

        if (roomConfig != null)
            roomConfig.SetRuntime(cfg); // 서버/호스트에 동일 상태 보관

        // 1) 룸 설정 전파
        ConfigBroadcastClientRpc(cfg.balloonCount, cfg.timeLimitSeconds, cfg.setCount);

        // 2) 네트워크 씬 로드(모든 클라가 따라옴)
        NetworkManager.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    /// <summary>
    /// 매치 종료 등 명시적인 시점에 런타임 상태를 비울 때 호출.
    /// </summary>
    public void ClearRuntimeConfig()
    {
        if (roomConfig != null)
            roomConfig.ResetRuntime();
    }
}
