using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Text.RegularExpressions;

/// <summary>
/// 싱글 플레이용 매치 상태 매니저.
/// - BalloonManager의 풍선 소진을 감시해 세트 종료
/// - 세트가 남아 있으면 다음 세트로 넘어가고, 없으면 매치 종료 후 메인 씬으로 복귀 옵션 제공
/// </summary>
public class SingleMatchManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RoomConfigSO _RoomConfig; // 룸 설정(Runtime 포함)
    [SerializeField] private SingleRoundManager _RoundManager;

    [Header("Round/Match Settings")]
    [SerializeField, Range(1, 5)] private int _TotalRounds = 1; // 초기값(초기화 시 초기화 스크립트 값으로 대체)
    [SerializeField, Range(1, 5)] private int _BalloonsPerRound = 1; // 라운드당 풍선 수(초기화 시 덮어씀)
    [SerializeField] private float _RoundEndPauseSeconds = 3f; // 라운드 종료 후 대기 시간
    [SerializeField] private float _MatchEndWaitSeconds = 5f; // 매치 종료 후 로비 이동 전 대기 시간
    [SerializeField] private int _TimeLimitSeconds = 60; // 라운드 당 제한 시간(초)

    [Header("Scene Flow")]
    [SerializeField] private bool _ReturnToLobbyOnMatchEnd = true;
    [SerializeField] private string _LobbySceneName = "Enterance_Alpha";

    [Header("Events (optional)")]
    [SerializeField] private UnityEvent onRoundEnd;            // 세트 종료 시 알림
    [SerializeField] private UnityEvent onPrepareNextRound;    // 다음 세트 준비 시 알림
    [SerializeField] private UnityEvent onTimeUp;            // 제한 시간 만료 시 알림

    /// <summary>라운드 결과 알림(클리어 여부).</summary>
    public event Action<bool> OnRoundResult;
    /// <summary>라운드 수 설정 알림.</summary>
    public event Action<int> OnRoundsConfigured;
    /// <summary>매치 최종 결과 알림(True면 승리, False면 패배).</summary>
    public event Action<bool> OnMatchResult;
    /// <summary>세트 시작 직전(카운트다운 등) 알림.</summary>
    public event Action OnRoundPreStart;

    private bool _MatchEnded = false;
    private bool _RoundEventsWired = false;
    private int _CurrentRoundIndex = 1;

    /// <summary>현재 총 라운드 수.</summary>
    public int TotalRounds => _TotalRounds;
    /// <summary>현재 진행 중인 라운드 번호(1부터 시작).</summary>
    public int CurrentRoundIndex => _CurrentRoundIndex;
    /// <summary>라운드 당 시간 제한(초).</summary>
    public int TimeLimitSeconds => _RoundManager != null ? _RoundManager.TimeLimitSeconds : _TimeLimitSeconds;


    private void Awake()
    {
        if (_RoundManager == null)
            _RoundManager = FindFirstObjectByType<SingleRoundManager>();
    }

    private void OnEnable()
    {
        if(_RoundManager != null)
        {
            _RoundManager.OnRoundResult += HandleRoundResult;

            RelayRoundManagerEvents();
        }
    }

    private void OnDisable()
    {
        if(_RoundManager != null)
        {
            _RoundManager.OnRoundResult -= HandleRoundResult;

            UnrelayRoundManagerEvents();
        }
    }

    private void Start() 
    {
        ConfigureFromRoomConfig();
        StartCoroutine(MatchFlowRoutine(lastRoundSuccess: true));
    }

    private void ConfigureFromRoomConfig()
    {
        if (_RoomConfig != null && _RoomConfig.runtimeConfig != null)
        {
            var cfg = _RoomConfig.runtimeConfig;
            _TotalRounds = Mathf.Max(1, cfg.setCount);
            _BalloonsPerRound = Mathf.Clamp(cfg.balloonCount, 1, 5);
            _TimeLimitSeconds = Mathf.Max(1, cfg.timeLimitSeconds);

            Debug.Log("SingleMatchManager: RoomConfig를 정상적으로 받았습니다.", this);
        }
        else
        {
            Debug.LogWarning("SingleMatchManager: RoomConfig를 받지 못해 기본값으로 진행합니다.", this);
        }

        _RoundManager.ApplySettings(_BalloonsPerRound, _TimeLimitSeconds);
    }

    private void HandleRoundResult(bool isSuccess)
    {
        OnRoundResult?.Invoke(isSuccess);
        StartCoroutine(MatchFlowRoutine(isSuccess));
    }

    private IEnumerator MatchFlowRoutine(bool lastRoundSuccess)
    {
        yield return new WaitForSeconds(_RoundEndPauseSeconds);

        if (_MatchEnded) yield break;

        // check match end condition
        bool matchOver = false;

        // condition 1: All rounds played
        if (_CurrentRoundIndex >= _TotalRounds)
        {
            matchOver = true;
        }

        // Condition 2: Round Defeat
        if (!lastRoundSuccess)
        {
            matchOver = true;
        }

        if (matchOver)
        {
            yield return new WaitForSeconds(_MatchEndWaitSeconds);
            EndGame(lastRoundSuccess);
        }
        else
        {
            // prepare next round
            _CurrentRoundIndex++;
            onPrepareNextRound?.Invoke();

            _RoundManager.StartRound();
        }
    }

    private void EndGame(bool matchResult)
    {
        if (_MatchEnded)
            return;

        _MatchEnded = true;

        Debug.Log($"SingleMatchManager: 매치 종료 | result={(matchResult ? "victory" : "defeat")}", this);

        if(_RoundManager != null)
        {
            _RoundManager.NotifyMatchEnded();
        }

        OnMatchResult?.Invoke(matchResult);

        StartCoroutine(EndGameRoutine());
    }

    private IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(_MatchEndWaitSeconds);

        if (_ReturnToLobbyOnMatchEnd)
        {
            ReturnToLobby();
        }
    }

    public void ReturnToLobby()
    {
        if (!string.IsNullOrWhiteSpace(_LobbySceneName))
        {
            SceneManager.LoadScene(_LobbySceneName);
        }
    }

    public void RestartMatch()
    {
        StopAllCoroutines();
        if (_MatchEnded)
        {
            // Reset state
            _MatchEnded = false;
            _CurrentRoundIndex = 1;

            // Reconfigure from RoomConfig
            ConfigureFromRoomConfig();

            // Start first round
            StartCoroutine(MatchFlowRoutine(lastRoundSuccess: true));
        }
    }

    /// <summary>
    /// 외부에서 강제 종료할 때 사용할 수 있는 메서드(예: 테스트용).
    /// </summary>
    public void ForceEndGame()
    {
        EndGame(matchResult: false);
    }

    private void RelayRoundManagerEvents()
    {
        if (_RoundManager == null || _RoundEventsWired)
            return;

        // 라운드 이벤트를 SingleMatch Manager/UnityEvent로 중계
        _RoundManager.OnRoundEnd += RelayRoundEnd;
        _RoundManager.OnPrepareNextRound += RelayPrepareNextRound;
        _RoundManager.OnRoundsConfigured += RelayRoundsConfigured;
        _RoundManager.OnRoundPreStart += RelayRoundPreStart;
        
        _RoundEventsWired = true;
    }

    private void UnrelayRoundManagerEvents()
    {
        if (_RoundManager == null || !_RoundEventsWired)
            return;

        // 라운드 이벤트 중계 해제
        _RoundManager.OnRoundEnd -= RelayRoundEnd;
        _RoundManager.OnPrepareNextRound -= RelayPrepareNextRound;
        _RoundManager.OnRoundsConfigured -= RelayRoundsConfigured;
        _RoundManager.OnRoundPreStart -= RelayRoundPreStart;

        _RoundEventsWired = false;
    }

    private void RelayRoundEnd()
    {
        onRoundEnd?.Invoke();
    }
    private void RelayPrepareNextRound()
    {
        onPrepareNextRound?.Invoke();
    }
    private void RelayRoundsConfigured()
    {
        OnRoundsConfigured?.Invoke(_TotalRounds);
    }
    private void RelayRoundPreStart()
    {
        OnRoundPreStart?.Invoke();
    }
}
