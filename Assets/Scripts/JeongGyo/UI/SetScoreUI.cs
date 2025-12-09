using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// 세트 결과를 원형 UI로 표시한다. GameManager.OnSetResult를 구독해 승/패/무승부 색상을 갱신한다.
/// </summary>
public class SetScoreUI : MonoBehaviour
{
    [SerializeField] private Image[] roundDots; // 세트 수만큼의 원형 이미지
    [SerializeField] private Color winColor = Color.green;
    [SerializeField] private Color loseColor = Color.red;
    [SerializeField] private Color drawColor = Color.yellow;
    [SerializeField] private Color emptyColor = Color.white;

    [SerializeField] private MatchManager gameManager;

    private int filledRounds = 0;
    private ulong localClientId;
    private int targetSets = 0;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<MatchManager>();

        InitializeDots();
    }

    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            localClientId = NetworkManager.Singleton.LocalClientId;

        if (gameManager != null)
        {
            gameManager.OnSetResult += HandleSetResult;
            gameManager.OnSetsConfigured += HandleSetsConfigured;
        }

        InitializeDots();
    }

    void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnSetResult -= HandleSetResult;
            gameManager.OnSetsConfigured -= HandleSetsConfigured;
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
        int configuredSets = gameManager != null ? gameManager.TotalSets : available;
        targetSets = Mathf.Clamp(configuredSets, 0, available);
        ResetDots();
    }

    private void HandleSetResult(ulong? winnerClientId)
    {
        if (roundDots == null || filledRounds >= targetSets || targetSets == 0)
            return;

        Color c = drawColor; // default draw
        if (winnerClientId.HasValue)
        {
            c = winnerClientId.Value == localClientId ? winColor : loseColor;
        }

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
