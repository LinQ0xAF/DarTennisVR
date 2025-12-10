// using UnityEngine;

// /// <summary>
// /// 싱글/로컬 플레이 씬 진입 시 GameSceneLoadManager에서 전달한 RoomConfig를 적용하고
// /// 로컬 시간 기준 경과 시간을 제공한다.
// /// - Awake에서 TryConsumePendingConfig로 설정을 가져와 풍선 개수/시간 등을 반영한다.
// /// - Start에서 MatchStartTime을 찍어 UI/게임 매니저가 동일한 기준을 사용하도록 한다.
// /// </summary>
// public class SingleGameplayInitializer : MonoBehaviour
// {
//     [Header("Optional targets")]
//     [SerializeField] private BalloonManager balloonManager; // 씬에 배치된 BalloonManager가 있으면 할당

//     [Header("Fallback defaults")]
//     [SerializeField, Range(1, 5)] private int defaultBalloonCount = 1; // config를 못 받았을 때 사용할 기본 풍선 수
//     [SerializeField] private int defaultTimeLimitSeconds = 60; // config를 못 받았을 때 사용할 기본 제한 시간
//     [SerializeField, Min(1)] private int defaultSetCount = 1; // config를 못 받았을 때 사용할 기본 세트 수

//     public int TimeLimitSeconds { get; private set; } // 다른 스크립트에서 참조할 수 있도록 노출
//     public int SetCount { get; private set; } = 1; // 설정된 세트 수(게임매니저 등에서 참조)
//     public int BalloonCount { get; private set; } = 1; // 설정된 풍선 수
//     public float MatchStartTime { get; private set; } // 로컬 기준 매치 시작 시각
//     public bool IsInitialized { get; private set; } = false; // 초기화 완료 여부

//     /// <summary>씬에서 찾은 풍선 매니저를 외부에 노출.</summary>
//     public BalloonManager LocalBalloonManager => balloonManager;

//     private void Awake()
//     {
//         if (balloonManager == null)
//             balloonManager = FindFirstObjectByType<BalloonManager>();

//     //    GameSceneLoadManager.TryConsumePendingConfig(out var cfg); // 설정 가져오기
//     //    ApplyConfig(cfg);
//     }

//     private void Start()
//     {
//         MatchStartTime = Time.time;
//         IsInitialized = true;
//     }

//     /// <summary>
//     /// 설정 값을 적용하고, 없는 경우 기본값을 사용한다.
//     /// </summary>
//     private void ApplyConfig(RoomConfigDto cfg)
//     {
//         BalloonCount = Mathf.Clamp(cfg?.balloonCount ?? defaultBalloonCount, 1, 5);
//         TimeLimitSeconds = Mathf.Max(1, cfg?.timeLimitSeconds ?? defaultTimeLimitSeconds);
//         SetCount = Mathf.Max(1, cfg?.setCount ?? defaultSetCount);

//         if (cfg == null)
//             Debug.LogWarning("SingleGameplayInitializer: RoomConfig를 받지 못해 기본값으로 진행합니다.", this);
//         else
//             Debug.Log("SingleGameplayInitializer: RoomConfig를 정상적으로 받았습니다.", this);

//         ConfigureBalloonManager();
//     }

//     /// <summary>
//     /// 로컬 기준 흐른 시간(초)을 반환.
//     /// </summary>
//     public float GetElapsedLocalSeconds()
//     {
//         return Mathf.Max(0f, Time.time - MatchStartTime);
//     }

//     /// <summary>
//     /// 매치 시작 시각을 수동으로 재설정(리트라이/리셋 시).
//     /// </summary>
//     public void SetMatchStartTime(float startTime)
//     {
//         MatchStartTime = startTime;
//     }

//     /// <summary>
//     /// BalloonManager에 설정된 풍선 수를 반영한다.
//     /// </summary>
//     private void ConfigureBalloonManager()
//     {
//         if (balloonManager == null)
//             return;

//         balloonManager.BalloonNumber = BalloonCount;
//         balloonManager.BalloonCurrentNumber = BalloonCount;
//     }
// }
