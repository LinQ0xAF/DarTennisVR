using UnityEngine;

/// <summary>
/// 멀티 플레이 씬 진입 시 MultiRoomNetController에서 전달한 RoomConfig를 적용한다.
/// - Start에서 TryConsumePendingConfig로 설정을 가져와 풍선 개수/시간 등을 매니저에 반영한다.
/// </summary>
public class MultiGameplayInitializer : MonoBehaviour
{
    [Header("Optional targets")]
    [SerializeField] private BalloonManager balloonManager; // 씬에 배치된 BalloonManager가 있으면 할당

    [Header("Fallback defaults")]
    [SerializeField, Range(1, 5)] private int defaultBalloonCount = 1; // config를 못 받았을 때 사용할 기본 풍선 수
    [SerializeField] private int defaultTimeLimitSeconds = 60; // config를 못 받았을 때 사용할 기본 제한 시간

    public int TimeLimitSeconds { get; private set; } // 다른 스크립트에서 참조할 수 있도록 노출

    void Start()
    {
        MultiRoomNetController.RoomConfig cfg = null; // 설정 객체, MultiRoomNetController.RoomConfig 에 각 클라이언트에 전달된 설정이 이미 저장된 상태
        MultiRoomNetController.TryConsumePendingConfig(out cfg); // 전달된 설정 가져오기, 필요한 룸에 대한 정보는 cfg에 저장됨
        if (cfg == null)
        {
            // config가 없으면 기본값으로 진행
            if (balloonManager != null)
                balloonManager.BalloonNumber = defaultBalloonCount;

            TimeLimitSeconds = defaultTimeLimitSeconds;
            Debug.LogWarning("MultiGameplayInitializer: RoomConfig를 받지 못해 기본값으로 진행합니다.", this);
            return;
        }
        else
        {
            Debug.Log("MultiGameplayInitializer: RoomConfig를 정상적으로 받았습니다.", this);
        }

        // 풍선 개수 적용
        if (balloonManager != null)
        {
            balloonManager.BalloonNumber = cfg.balloonCount;
        }

        // 제한 시간 저장(실제 타이머 적용은 다른 타이머/게임매니저가 이 값을 참조하도록 연결)
        TimeLimitSeconds = cfg.timeLimitSeconds;
    }
}
