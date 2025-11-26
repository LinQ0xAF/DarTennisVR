using UnityEngine;
using TMPro;

/// <summary>
/// 서버 시간 기반 남은 시간을 표시하는 간단한 UI 타이머.
/// MultiGameplayInitializer로부터 제한 시간/시작 시각을 받아 서버 시계 기준으로 동일하게 움직인다.
/// </summary>
public class UIMatchTimer : MonoBehaviour
{
    [SerializeField] private MultiGameplayInitializer gameplayInitializer; // 씬에 존재하는 초기화 스크립트 참조
    [SerializeField] private TMP_Text timerText; // mm:ss 표시용
    [SerializeField] private bool clampToZero = true; // 0 아래로 내려갈지 여부

    void Awake()
    {
        // 인스펙터에 없으면 씬에서 자동 검색
        if (gameplayInitializer == null)
            gameplayInitializer = FindObjectOfType<MultiGameplayInitializer>();
    }

    void Update()
    {
        if (gameplayInitializer == null || timerText == null)
            return;

        var elapsed = gameplayInitializer.GetElapsedServerSeconds();
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
