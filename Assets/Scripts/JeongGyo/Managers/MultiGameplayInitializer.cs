using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 멀티 플레이 씬 진입 시 MultiRoomNetController에서 전달한 RoomConfig를 적용한다.
/// - Start에서 TryConsumePendingConfig로 설정을 가져와 풍선 개수/시간 등을 매니저에 반영한다.
/// </summary>
public class MultiGameplayInitializer : MonoBehaviour
{
    [Header("Optional targets")]
    [SerializeField] private NetworkBalloonManager balloonManager; // 씬에 배치된 BalloonManager가 있으면 할당

    [Header("Fallback defaults")]
    [SerializeField, Range(1, 5)] private int defaultBalloonCount = 1; // config를 못 받았을 때 사용할 기본 풍선 수
    [SerializeField] private int defaultTimeLimitSeconds = 60; // config를 못 받았을 때 사용할 기본 제한 시간
    [SerializeField, Min(1)] private int defaultSetCount = 1; // config를 못 받았을 때 사용할 기본 세트 수

    public int TimeLimitSeconds { get; private set; } // 다른 스크립트에서 참조할 수 있도록 노출
    public int SetCount { get; private set; } = 1; // 설정된 세트 수(게임매니저 등에서 참조)
    public int BalloonCount { get; private set; } = 1; // 설정된 풍선 수
    public float ServerMatchStartTime { get; private set; } // 서버 기준 매치 시작 시각
    public NetworkBalloonManager LocalBalloonManager => balloonManager; // 로컬 아바타의 풍선 매니저 참조
    public bool IsInitialized { get; private set; } = false; // 초기화 완료 여부

    void Start()
    {  
        RoomConfigDto cfg = null; // 설정 객체, RoomConfigDto에 각 클라이언트에 전달된 설정이 이미 저장된 상태
        MultiRoomNetController.TryConsumePendingConfig(out cfg); // 전달된 설정 가져오기, 필요한 룸에 대한 정보는 cfg에 저장됨
        
        if (balloonManager == null)
            balloonManager = FindLocalPlayersBalloonManager();

        // 서버가 브로드캐스트한 매치 시작 시각 저장. 이후에는 NetworkManager.ServerTime으로 흐르는 값을 계속 사용할 수 있다.
        if (!MultiRoomNetController.TryConsumePendingStartTime(out var startTime))
        {
            // 못 받았다면 현재 서버 시간을 fallback으로 사용(로컬 타이머와 약간 오차가 날 수 있음)
            startTime = NetworkManager.Singleton != null ? (float)NetworkManager.Singleton.ServerTime.Time : 0f;
            Debug.LogWarning("MultiGameplayInitializer: StartTime을 받지 못해 현재 ServerTime으로 대체합니다.", this);
        }

        ServerMatchStartTime = startTime;

        if (cfg == null)
        {
            // config가 없으면 기본값으로 진행
            BalloonCount = defaultBalloonCount;
            if (balloonManager != null)
            {
                balloonManager.MaxBalloonCount = defaultBalloonCount;
                ActivateBalloonManager();
            }

            TimeLimitSeconds = defaultTimeLimitSeconds;
            SetCount = defaultSetCount;
            Debug.LogWarning("MultiGameplayInitializer: RoomConfig를 받지 못해 기본값으로 진행합니다.", this);
        }
        else
        {
            Debug.Log("MultiGameplayInitializer: RoomConfig를 정상적으로 받았습니다.", this);
            // 풍선 개수 적용
            BalloonCount = cfg.balloonCount;
            if (balloonManager != null)
            {
                balloonManager.MaxBalloonCount = cfg.balloonCount;
                Debug.Log("받아온 자기 자신의 값으로 벌룬메니저 세팅 완료.");
                ActivateBalloonManager();
            }

            // 제한 시간 저장(실제 타이머 적용은 다른 타이머/게임매니저가 이 값을 참조하도록 연결)
            TimeLimitSeconds = cfg.timeLimitSeconds;
            SetCount = cfg.setCount > 0 ? cfg.setCount : defaultSetCount;
        }
        
        IsInitialized = true;
    }

    /// <summary>
    /// 서버 기준 흐른 시간(초)을 반환. NetworkManager.ServerTime은 자동 동기화된다.
    /// </summary>
    public float GetElapsedServerSeconds()
    {
        if (NetworkManager.Singleton == null)
            return 0f;

        return (float)(NetworkManager.Singleton.ServerTime.Time - ServerMatchStartTime);
    }

    /// <summary>
    /// 서버에서 세트 시작 시각을 재동기화할 때 사용(다음 세트 시작 시 등).
    /// </summary>
    public void SetServerMatchStartTime(float startTime)
    {
        ServerMatchStartTime = startTime;
    }

    /// <summary>
    /// 로컬 플레이어가 소유한 아바타에서 BalloonManager를 찾아 반환한다.
    /// </summary>
    private NetworkBalloonManager FindLocalPlayersBalloonManager()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.LocalClient == null)
            return null;

        var playerObj = nm.LocalClient.PlayerObject;
        if (playerObj == null)
            return null;

        return playerObj.GetComponentInChildren<NetworkBalloonManager>(true);
    }

    /// <summary>
    /// 한 프레임 뒤 풍선 매니저를 활성화하고 초기화한다(텔레포트/리깅 후 위치 안정화용).
    /// </summary>
    private void ActivateBalloonManager()
    {
        if (balloonManager == null)
            return;

        if (!balloonManager.gameObject.activeSelf)
            balloonManager.gameObject.SetActive(true);

        balloonManager.Initialize();
    }
}
