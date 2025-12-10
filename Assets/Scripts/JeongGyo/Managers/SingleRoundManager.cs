using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 라운드 진행/승패 판정/타이머 관리 전담.
/// - 매치 승패나 다음 라운드 진행 여부는 MatchManager가 결정한다.
/// </summary>

public class SingleRoundManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BalloonManager _TargetBalloonManager;
    
    [Header("Defaults (fallback if SingleMatchManager doesn't override)")]
    [SerializeField, Min(1)] private int _InitialTargetBalloonsCount = 1;
    [SerializeField] private float _PrepDurationSeconds = 3f; // 라운드 시작 전 대기 시간
    [SerializeField] private int _InitialTimeLimitSeconds = 60;

    private bool _MatchEnded = false;
    private bool _RoundEnding;
    private bool _OnPrepPhase = false;
    private int _TargetBalloonsCount;
    private int _ConfiguredTimeLimitSeconds;
    private int _Balloons_remaining;
    private float RoundStartTime = 0f;

    /// <summary>라운드 결과를 알리는 이벤트(Success/Fail).</summary>
    public event Action<bool> OnRoundResult;
    /// <summary>라운드 수가 설정될 때 알림.</summary>
    public event Action OnRoundsConfigured;
    /// <summary>라운드 종료 알림(점수 집계, UI 등).</summary>
    public event Action OnRoundEnd;
    /// <summary>다음 라운드 준비 시 실행(풍선 리셋 등).</summary>
    public event Action OnPrepareNextRound;
    /// <summary>라운드 시작 직전(Prep 단계 진입) 알림.</summary>
    public event Action OnRoundPreStart;
    /// <summary>라운드 실제 시작(Prep 종료, 타이머 시작) 알림.</summary>
    public event Action OnRoundStart;

    public int TimeLimitSeconds => _ConfiguredTimeLimitSeconds;

    private void Awake()
    {
        _ConfiguredTimeLimitSeconds = _InitialTimeLimitSeconds;
        _TargetBalloonsCount = Mathf.Max(1, _InitialTargetBalloonsCount);

        if (_TargetBalloonManager == null)
            _TargetBalloonManager = FindFirstObjectByType<BalloonManager>();
    }

    private void Start()
    {
        _TargetBalloonManager.OnBalloonPop += HandleBalloonPop;
    }

    public void ApplySettings(int balloonsCount, int timeLimitSeconds)
    {
        _TargetBalloonsCount = Mathf.Max(1, balloonsCount);
        _ConfiguredTimeLimitSeconds = Mathf.Max(1, timeLimitSeconds);
        OnRoundsConfigured?.Invoke();
    }

    public void NotifyMatchEnded()
    {
        _MatchEnded = true;
    }

    public void StartRound()
    {
        _MatchEnded = false;
        _RoundEnding = false;

        _Balloons_remaining = _TargetBalloonsCount;
        _TargetBalloonManager.ResetBalloons(_TargetBalloonsCount);

        StartCoroutine(RoundStartSequence());
    }

    void Update()
    {
        if (_MatchEnded || _RoundEnding || _OnPrepPhase)
            return;

        CheckTimeLimit();
    }

    private void CheckTimeLimit()
    {
        float elapsed = GetElapsedLocalSeconds();
        float remain = _ConfiguredTimeLimitSeconds - elapsed;

        if (remain <= 0f)
        {
            EndRound(isTimeUp: true);
        }
    }

    /// <summary>
    /// 로컬 기준 흐른 시간(초)을 반환.
    /// </summary>
    public float GetElapsedLocalSeconds()
    {
        return Mathf.Max(0f, Time.time - RoundStartTime);
    }

    private IEnumerator RoundStartSequence()
    {
        _OnPrepPhase = true;
        OnRoundPreStart?.Invoke();

        yield return new WaitForSeconds(_PrepDurationSeconds);

        _OnPrepPhase = false;
        RoundStartTime = Time.time;
        OnRoundStart?.Invoke();
    }

    private void EndRound(bool isTimeUp)
    {
        if (_RoundEnding || _MatchEnded)
            return;

        _RoundEnding = true;
        bool isSuccess = !isTimeUp && (_Balloons_remaining == 0);

        OnRoundEnd?.Invoke();
        OnRoundResult?.Invoke(isSuccess);
    }

    // 풍선 개수 관리 로직
    private void HandleBalloonPop(int remainingCount)
    {
        _Balloons_remaining = remainingCount;

        if (_Balloons_remaining <= 0)
        {
            EndRound(isTimeUp: false);
        }
    }
}