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
        MultiRoomNetController.RoomConfig cfg = null;
        MultiRoomNetController.TryConsumePendingConfig(out cfg);
        if (cfg == null)
        {
            // config가 없으면 기본값으로 진행
            if (balloonManager != null)
                balloonManager.BalloonNumber = defaultBalloonCount;

            TimeLimitSeconds = defaultTimeLimitSeconds;
            Debug.LogWarning("MultiGameplayInitializer: RoomConfig를 받지 못해 기본값으로 진행합니다.", this);
            return;
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
