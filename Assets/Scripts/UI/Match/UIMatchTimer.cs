using UnityEngine;
using TMPro;

/// <summary>
/// 서버 시간 기반 남은 시간을 표시하는 간단한 UI 타이머.
/// GameManager에서 제한 시간/시작 시각을 받아 서버 시계 기준으로 동일하게 움직인다.
/// </summary>
public class UIMatchTimer : MonoBehaviour
{
    [SerializeField] private MatchManager gameManager; // 씬에 존재하는 게임 매니저 참조
    [SerializeField] private TMP_Text timerText; // mm:ss 표시용
    [SerializeField] private bool clampToZero = true; // 0 아래로 내려갈지 여부
    // [SerializeField] private GamePersonalDataManager personalDataManager; // 네트워크 매니저가 담긴 오브젝트

    void Awake()
    {
        // 인스펙터에 없으면 씬에서 자동 검색
        if (gameManager == null)
            gameManager = FindFirstObjectByType<MatchManager>();
        // if (personalDataManager == null)
        //     personalDataManager = FindFirstObjectByType<GamePersonalDataManager>();
    }

    void Update()
    {
        if (gameManager == null || timerText == null || !gameManager.HasSetStartTime())
            return;

        var elapsed = gameManager.GetElapsedServerSeconds();
        var remain = gameManager.TimeLimitSeconds - elapsed;
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
