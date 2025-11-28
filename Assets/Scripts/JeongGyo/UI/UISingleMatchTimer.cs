using UnityEngine;
using TMPro;

/// <summary>
/// 로컬 시간 기반 싱글 플레이 매치 타이머 UI.
/// SingleGameplayInitializer에서 제한 시간과 시작 시각을 받아 로컬 시간 기준으로 표시한다.
/// </summary>
public class UISingleMatchTimer : MonoBehaviour
{
    [SerializeField] private SingleGameplayInitializer gameplayInitializer; // 씬에 존재하는 초기화 스크립트 참조
    [SerializeField] private TMP_Text timerText; // mm:ss 표시용
    [SerializeField] private bool clampToZero = true; // 0 아래로 내려갈지 여부

    private void Awake()
    {
        // 인스펙터에 없으면 씬에서 자동 검색
        if (gameplayInitializer == null)
            gameplayInitializer = FindObjectOfType<SingleGameplayInitializer>();
    }

    private void Update()
    {
        if (gameplayInitializer == null || timerText == null || !gameplayInitializer.IsInitialized)
            return;

        var elapsed = gameplayInitializer.GetElapsedLocalSeconds();
        var remain = gameplayInitializer.TimeLimitSeconds - elapsed;
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
