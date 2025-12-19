using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Gameplay.Match.Interfaces;

/// <summary>
/// 세트/라운드 결과를 원형 UI로 표시하는 통합 스크립트.
/// 멀티(승/패/무)와 싱글(성공/실패) 모드를 모두 지원한다.
/// </summary>
public class SetScoreUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MatchManager _multiManager;
    [SerializeField] private SingleMatchManager _singleManager;

    [Header("UI Components")]
    [SerializeField] private Image[] roundDots; // 세트 수만큼의 원형 이미지
    [SerializeField] private Color emptyColor = Color.white;

    [Header("Multiplayer Colors")]
    [SerializeField] private Color winColor = Color.green;
    [SerializeField] private Color loseColor = Color.red;
    [SerializeField] private Color drawColor = Color.yellow;

    [Header("Singleplayer Colors")]
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    private int filledRounds = 0;
    private int targetSets = 0;
    private ulong localClientId;
    private IMatchManager _activeManager;

    private void Awake()
    {
        _activeManager = (_multiManager as IMatchManager) ?? (_singleManager as IMatchManager);

        if (_activeManager == null)
        {
            if (_multiManager == null) _multiManager = FindFirstObjectByType<MatchManager>();
            if (_singleManager == null) _singleManager = FindFirstObjectByType<SingleMatchManager>();
            
            _activeManager = (_multiManager as IMatchManager) ?? (_singleManager as IMatchManager);
        }
        
        InitializeDots();
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            localClientId = NetworkManager.Singleton.LocalClientId;

        if (_multiManager != null)
        {
            _multiManager.OnSetResult += HandleMultiResult;
            _multiManager.OnSetsConfigured += HandleSetsConfigured;
        }
        else if (_singleManager != null)
        {
            _singleManager.OnRoundResult += HandleSingleResult;
            _singleManager.OnRoundsConfigured += HandleSetsConfigured;
        }

        InitializeDots();
    }

    private void OnDisable()
    {
        if (_multiManager != null)
        {
            _multiManager.OnSetResult -= HandleMultiResult;
            _multiManager.OnSetsConfigured -= HandleSetsConfigured;
        }
        else if (_singleManager != null)
        {
            _singleManager.OnRoundResult -= HandleSingleResult;
            _singleManager.OnRoundsConfigured -= HandleSetsConfigured;
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
        int configuredSets = _activeManager != null ? _activeManager.TotalSets : available;
        targetSets = Mathf.Clamp(configuredSets, 0, available);
        ResetDots();
    }

    private void UpdateNextDot(Color color)
    {
        if (roundDots == null || filledRounds >= targetSets || targetSets == 0)
            return;

        if (roundDots[filledRounds] != null)
            roundDots[filledRounds].color = color;

        filledRounds++;
    }

    // 멀티플레이 결과 처리
    private void HandleMultiResult(ulong? winnerClientId)
    {
        Color c = drawColor;
        if (winnerClientId.HasValue)
        {
            c = winnerClientId.Value == localClientId ? winColor : loseColor;
        }
        UpdateNextDot(c);
    }

    // 싱글플레이 결과 처리
    private void HandleSingleResult(bool isSuccess)
    {
        UpdateNextDot(isSuccess ? successColor : failColor);
    }

    private void HandleSetsConfigured(int totalSets)
    {
        int available = roundDots != null ? roundDots.Length : 0;
        targetSets = Mathf.Clamp(totalSets, 0, available);
        ResetDots();
    }
}
