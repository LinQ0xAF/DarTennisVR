using UnityEngine;
using TMPro;
using Gameplay.Match.Interfaces;

/// <summary>
/// 매치(세트/라운드) 남은 시간을 표시하는 통합 UI 타이머.
/// MatchManager(멀티) 또는 SingleMatchManager(싱글)를 자동으로 감지하여 작동한다.
/// </summary>
public class UIMatchTimer : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MatchManager _multiManager;
    [SerializeField] private SingleMatchManager _singleManager;

    [Header("UI Settings")]
    [SerializeField] private TMP_Text timerText; // mm:ss 표시용
    [SerializeField] private bool clampToZero = true; // 0 아래로 내려갈지 여부

    private IMatchManager _activeManager;

    private void Awake()
    {
        // 1. 인스펙터 할당 우선
        _activeManager = (_multiManager as IMatchManager) ?? (_singleManager as IMatchManager);

        // 2. 없으면 구체적인 타입으로 검색 (가장 빠르고 안전함)
        if (_activeManager == null)
        {
            if (_multiManager == null) _multiManager = FindFirstObjectByType<MatchManager>();
            if (_singleManager == null) _singleManager = FindFirstObjectByType<SingleMatchManager>();
            
            _activeManager = (_multiManager as IMatchManager) ?? (_singleManager as IMatchManager);
        }
    }

    private void Update()
    {
        if (_activeManager == null || timerText == null)
            return;

        // 인터페이스를 통해 경과 시간 및 제한 시간 조회
        var elapsed = _activeManager.GetElapsedSeconds();
        var remain = _activeManager.TimeLimitSeconds - elapsed;
        
        if (clampToZero && remain < 0f)
            remain = 0f;

        FormatToText(remain);
    }

    private void FormatToText(float remainSeconds)
    {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(remainSeconds));
        int minutes = seconds / 60;
        int sec = seconds % 60;
        timerText.text = $"{minutes:00}:{sec:00}";
    }
}
