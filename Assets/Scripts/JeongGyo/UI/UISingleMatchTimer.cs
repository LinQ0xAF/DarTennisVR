using UnityEngine;
using TMPro;

/// <summary>
/// 로컬 시간 기반 싱글 플레이 매치 타이머 UI.
/// SingleGameplayInitializer에서 제한 시간과 시작 시각을 받아 로컬 시간 기준으로 표시한다.
/// </summary>
public class UISingleMatchTimer : MonoBehaviour
{
    [SerializeField] private SingleMatchManager _MatchManager; // 씬에 존재하는 라운드 스크립트 참조
    [SerializeField] private TMP_Text _TimerText; // mm:ss 표시용
    [SerializeField] private bool _ClampToZero = true; // 0 아래로 내려갈지 여부
    private float _TimeLimitSeconds = -1f;
    public float TimeLimitSeconds => _MatchManager != null ? _MatchManager.TimeLimitSeconds : _TimeLimitSeconds;

    private void Awake()
    {
        // 인스펙터에 없으면 씬에서 자동 검색
        if (_MatchManager == null)
            _MatchManager = FindFirstObjectByType<SingleMatchManager>();
    }

    private void OnEnable()
    {
        if (_MatchManager != null)
            _TimeLimitSeconds = _MatchManager.TimeLimitSeconds;
    }

    private void Update()
    {
        if (_MatchManager == null || _TimerText == null)
            return;

        var elapsed = _MatchManager.GetElapsedLocalSeconds();
        var remain = TimeLimitSeconds - elapsed;
        if (_ClampToZero && remain < 0f)
            remain = 0f;

        FormatToText(remain);
    }

    private void FormatToText(float remainSeconds)
    {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(remainSeconds));
        int minutes = seconds / 60;
        int sec = seconds % 60;
        _TimerText.text = $"{minutes:00}:{sec:00}";
    }
}
