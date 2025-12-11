using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 싱글 플레이 세트 스코어 UI.
/// SingleGameManager의 OnSetResult/OnSetsConfigured 이벤트를 구독해 세트 성공/실패를 색상으로 표시한다.
/// </summary>
public class SingleSetScoreUI : MonoBehaviour
{
    [SerializeField] private Image[] roundDots; // 세트 수만큼의 원형 이미지
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private Color emptyColor = Color.white;

    [SerializeField] private SingleMatchManager _MatchManager;

    private int filledRounds = 0;
    private int targetSets = 0;

    private void Awake()
    {
        if (_MatchManager == null)
            _MatchManager = FindFirstObjectByType<SingleMatchManager>();

        InitializeDots();
    }

    private void OnEnable()
    {
        if (_MatchManager != null)
        {
            _MatchManager.OnRoundResult += HandleSetResult;
            _MatchManager.OnRoundsConfigured += HandleSetsConfigured;
        }

        InitializeDots();
    }

    private void OnDisable()
    {
        if (_MatchManager != null)
        {
            _MatchManager.OnRoundResult -= HandleSetResult;
            _MatchManager.OnRoundsConfigured -= HandleSetsConfigured;
        }
    }

    private void ResetDots()
    {
        if (roundDots == null)
            return;

        filledRounds = 0;
        for (int i = 0; i < roundDots.Length; i++)
        {
            if (roundDots[i] == null)
                continue;

            bool active = i < targetSets;
            roundDots[i].gameObject.SetActive(active);
            roundDots[i].color = emptyColor;
        }
    }

    private void InitializeDots()
    {
        int available = roundDots != null ? roundDots.Length : 0;
        int configuredSets = _MatchManager != null ? _MatchManager.TotalRounds : available;
        targetSets = Mathf.Clamp(configuredSets, 0, available);
        ResetDots();
    }

    private void HandleSetResult(bool isSuccess)
    {
        if (roundDots == null || filledRounds >= targetSets || targetSets == 0)
            return;

        Color c = isSuccess ? successColor : failColor;

        if (roundDots[filledRounds] != null)
            roundDots[filledRounds].color = c;

        filledRounds++;
    }

    private void HandleSetsConfigured(int totalSets)
    {
        int available = roundDots != null ? roundDots.Length : 0;
        targetSets = Mathf.Clamp(totalSets, 0, available);
        ResetDots();
    }
}
