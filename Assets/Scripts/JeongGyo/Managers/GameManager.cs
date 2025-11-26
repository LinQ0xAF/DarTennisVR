using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 서버 기준으로 세트/매치 흐름을 관리하고, 풍선 잔여 수 기반 승패를 판정해 모든 클라이언트에 브로드캐스트한다.
/// - 풍선이 모두 터지거나 타임업 시 세트 종료
/// - 풍선이 먼저 0이 된 쪽이 패배, 타임업이면 잔여 풍선이 많은 쪽 승리(동점은 무승부)
/// - 세트가 남아 있으면 잠시 멈췄다가 다음 세트 준비, 없으면 매치 종료
/// </summary>
public class GameManager : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private MultiGameplayInitializer gameplayInitializer; // 서버 시간/제한 시간을 제공
    [SerializeField] private NetworkBalloonManager balloonManager; // 풍선 상태 감시용
    [SerializeField] private NetworkBalloonHitChannelSO balloonHitChannel; // 팀원 채널: 서버가 풍선 피격 보고 수신

    [Header("Set/Match Settings")]
    [SerializeField, Min(1)] private int totalSets = 1; // 인스펙터 기본 세트 수(초기화 시 룸 설정으로 대체)
    [SerializeField, Min(1)] private int balloonsPerPlayer = 1; // 플레이어당 시작 풍선 수(씬 설정이 우선)
    [SerializeField] private float setEndPauseSeconds = 3f; // 세트 종료 후 잠시 멈추는 시간

    [Header("Scene Flow")]
    [SerializeField] private string lobbySceneName = "EnteranceCopy"; // 모든 세트 종료 후 돌아갈 씬 이름(네트워크 씬 로드)
    [SerializeField] private bool returnToLobbyOnMatchEnd = true;

    [Header("Events (optional)")]
    [SerializeField] private UnityEvent onSetEnd; // 세트 종료 알림(점수 집계, UI 등)
    [SerializeField] public UnityEvent onPrepareNextSet; // 다음 세트 준비 시 실행(풍선 리셋 등)
    [SerializeField] private UnityEvent onTimeUp; // 매치 종료/타임업 시 실행

    private bool gameEnded;
    private bool setsConfigured;
    private bool setEnding;
    private int currentSetIndex = 1;
    private ulong? lastSetWinnerClientId;
    private readonly Dictionary<ulong, int> balloonsRemaining = new Dictionary<ulong, int>(); // 서버에서만 사용
    private float currentSetStartTime; // 서버 기준 세트 시작 시각

    /// <summary>세트 결과를 알리는 이벤트(승자 clientId, 무승부면 null).</summary>
    public event System.Action<ulong?> OnSetResult;
    /// <summary>세트 수가 설정될 때 알림.</summary>
    public event System.Action<int> OnSetsConfigured;

    void Awake()
    {
        if (gameplayInitializer == null)
            gameplayInitializer = FindObjectOfType<MultiGameplayInitializer>();

        if (balloonManager == null && gameplayInitializer != null)
            balloonManager = gameplayInitializer.LocalBalloonManager;

        if (balloonManager != null)
            balloonsPerPlayer = balloonManager.MaxBalloonCount;
    }

    void OnEnable()
    {
        if (balloonHitChannel != null)
            balloonHitChannel.OnPlayerHit += HandleBalloonHitFromChannel;

        // balloonManager 이벤트 사용 시 복원
        // if (balloonManager != null)
        // {
        //     balloonManager.OnAllBalloonsCleared += HandleAllBalloonsCleared;
        //     balloonManager.OnBalloonPopRequest += HandleBalloonPopRequest;
        // }
    }

    void OnDisable()
    {
        if (balloonHitChannel != null)
            balloonHitChannel.OnPlayerHit -= HandleBalloonHitFromChannel;

        // balloonManager 이벤트 사용 시 복원
        // if (balloonManager != null)
        // {
        //     balloonManager.OnAllBalloonsCleared -= HandleAllBalloonsCleared;
        //     balloonManager.OnBalloonPopRequest -= HandleBalloonPopRequest;
        // }
    }

    void Start()
    {
        TryConfigureSetsFromInitializer();

        if (IsServer)
        {
            ResetBalloonCountsForSet();
            SyncSetStartTime();
        }
    }

    void Update()
    {
        if (gameEnded || gameplayInitializer == null)
            return;

        if (!setsConfigured)
            TryConfigureSetsFromInitializer();

        if (!IsServer)
            return;

        float elapsed = gameplayInitializer.GetElapsedServerSeconds();
        float remain = gameplayInitializer.TimeLimitSeconds - elapsed;

        if (remain <= 0f)
        {
            ulong? winnerId = EvaluateWinnerByRemaining();
            StartSetEndSequence("time_up", winnerId);
        }
    }

    /// <summary>룸 설정에서 세트 수를 전달할 때 사용.</summary>
    public void ConfigureSets(int totalSetCount, int startSetIndex = 1)
    {
        totalSets = Mathf.Max(1, totalSetCount);
        currentSetIndex = Mathf.Clamp(startSetIndex, 1, totalSets);
        setsConfigured = true;
        OnSetsConfigured?.Invoke(totalSets);
    }

    public int TotalSets => totalSets;

    private void TryConfigureSetsFromInitializer()
    {
        if (setsConfigured || gameplayInitializer == null)
            return;

        ConfigureSets(gameplayInitializer.SetCount);

        if (balloonManager != null)
            balloonsPerPlayer = balloonManager.MaxBalloonCount;
    }

    /// <summary>내 풍선이 맞았을 때 로컬 NetworkBalloonManager에서 호출됨.</summary>
    private void HandleBalloonPopRequest(int balloonIndex)
    {
        if (gameEnded || setEnding)
            return;

        if (IsServer)
        {
            ProcessBalloonPop(NetworkManager != null ? NetworkManager.LocalClientId : 0);
        }
        else
        {
            ReportBalloonPoppedServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportBalloonPoppedServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        ProcessBalloonPop(senderId);
    }

    private void ProcessBalloonPop(ulong senderClientId)
    {
        if (!IsServer || gameEnded || setEnding)
            return;

        if (!balloonsRemaining.ContainsKey(senderClientId))
            balloonsRemaining[senderClientId] = balloonsPerPlayer;

        if (balloonsRemaining[senderClientId] <= 0)
            return;

        balloonsRemaining[senderClientId] = Mathf.Max(0, balloonsRemaining[senderClientId] - 1);
        Debug.Log($"[GameManager] Server balloon pop | sender:{senderClientId} | remain:{balloonsRemaining[senderClientId]}");

        if (balloonsRemaining[senderClientId] == 0)
        {
            ulong? winnerId = EvaluateWinnerByRemaining();
            StartSetEndSequence("balloons_depleted", winnerId);
        }
    }

    /// <summary>풍선 모두 터졌을 때(서버만) 호출.</summary>
    private void HandleAllBalloonsCleared()
    {
        if (!IsServer || gameEnded)
            return;

        ulong? winnerId = EvaluateWinnerByRemaining();
        StartSetEndSequence("balloons_cleared", winnerId);
    }

    private void StartSetEndSequence(string reason, ulong? winnerClientId)
    {
        if (setEnding || gameEnded)
            return;

        setEnding = true;
        bool hasNextSet = currentSetIndex < totalSets;
        int nextSetIndex = hasNextSet ? currentSetIndex + 1 : currentSetIndex;
        lastSetWinnerClientId = winnerClientId;

        string winnerText = winnerClientId.HasValue ? winnerClientId.Value.ToString() : "draw";
        Debug.Log($"[GameManager] 세트 종료 감지(reason:{reason}) | winner:{winnerText} | current:{currentSetIndex}/{totalSets} | next:{nextSetIndex} | hasNext:{hasNextSet}");
        onSetEnd?.Invoke(); // 승/패 UI는 여기 이벤트 구독
        OnSetResult?.Invoke(winnerClientId);

        StartCoroutine(SetEndRoutine(hasNextSet, nextSetIndex, winnerClientId));
        if (IsServer)
            SetEndClientRpc(hasNextSet, nextSetIndex, winnerClientId.HasValue, winnerClientId.GetValueOrDefault());
    }

    private System.Collections.IEnumerator SetEndRoutine(bool hasNextSet, int nextSetIndex, ulong? winnerClientId)
    {
        yield return new WaitForSeconds(setEndPauseSeconds);

        setEnding = false;
        if (gameEnded)
            yield break;

        if (hasNextSet)
        {
            PrepareNextSet(nextSetIndex);
        }
        else
        {
            EndGame();
        }
    }

    [ClientRpc]
    private void SetEndClientRpc(bool hasNextSet, int nextSetIndex, bool hasWinner, ulong winnerClientId)
    {
        if (IsServer || gameEnded)
            return;

        setEnding = true;
        lastSetWinnerClientId = hasWinner ? winnerClientId : (ulong?)null;
        string winnerText = hasWinner ? winnerClientId.ToString() : "draw";
        Debug.Log($"[GameManager] 클라이언트 세트 종료 수신 | winner:{winnerText} | next:{nextSetIndex} | hasNext:{hasNextSet}");
        onSetEnd?.Invoke();
        OnSetResult?.Invoke(lastSetWinnerClientId);
        StartCoroutine(SetEndRoutine(hasNextSet, nextSetIndex, lastSetWinnerClientId));
    }

    private void PrepareNextSet(int nextSetIndex) // 서버에서 다음 세트 준비
    {
        currentSetIndex = nextSetIndex;
        Debug.Log($"[GameManager] 다음 세트 준비 (set {nextSetIndex}/{totalSets})");

        onPrepareNextSet?.Invoke();
        ResetBalloonCountsForSet();
        SyncSetStartTime();
        PrepareNextSetClientRpc(nextSetIndex);
    }

    [ClientRpc]
    private void PrepareNextSetClientRpc(int nextSetIndex)
    {
        if (IsServer || gameEnded)
            return;

        currentSetIndex = nextSetIndex;
        Debug.Log($"[GameManager] 클라이언트에서 다음 세트 준비 수신 (set {nextSetIndex}/{totalSets})");
        SyncSetStartTimeClient();
        onPrepareNextSet?.Invoke();
    }

    /// <summary>시간 만료/세트 모두 소진 시 호출. 서버에서 ClientRpc로 알림.</summary>
    public void EndGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        Debug.Log("[GameManager] 매치 종료 - EndGame 실행");

        if (IsServer)
        {
            EndGameClientRpc();
        }

        onTimeUp?.Invoke();

        if (IsServer && returnToLobbyOnMatchEnd && !string.IsNullOrWhiteSpace(lobbySceneName))
        {
            if (NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
            }
            else
            {
                SceneManager.LoadScene(lobbySceneName);
            }
        }
    }

    [ClientRpc]
    private void EndGameClientRpc()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        Debug.Log("[GameManager] 클라이언트에서 매치 종료 수신 - EndGame 실행");
        onTimeUp?.Invoke();
    }

    private void ResetBalloonCountsForSet()
    {
        if (!IsServer || NetworkManager == null)
            return;

        balloonsRemaining.Clear();
        foreach (var clientId in NetworkManager.ConnectedClientsIds)
        {
            balloonsRemaining[clientId] = balloonsPerPlayer;
        }
    }

    private ulong? EvaluateWinnerByRemaining()
    {
        if (!IsServer || balloonsRemaining.Count == 0)
            return null;

        ulong? bestClient = null;
        int bestRemain = int.MinValue;
        bool tie = false;

        foreach (var kv in balloonsRemaining)
        {
            if (kv.Value > bestRemain)
            {
                bestRemain = kv.Value;
                bestClient = kv.Key;
                tie = false;
            }
            else if (kv.Value == bestRemain)
            {
                tie = true;
            }
        }

        if (tie)
            return null; // 무승부

        return bestClient;
    }

    /// <summary>서버: 세트 시작 기준 시각을 동기화.</summary>
    private void SyncSetStartTime()
    {
        if (!IsServer || gameplayInitializer == null || NetworkManager == null)
            return;

        currentSetStartTime = (float)NetworkManager.ServerTime.Time;
        gameplayInitializer.SetServerMatchStartTime(currentSetStartTime); // 서버 자신도 갱신
        SetStartTimeClientRpc(currentSetStartTime);
    }

    [ClientRpc]
    private void SetStartTimeClientRpc(float startTime)
    {
        if (IsServer || gameplayInitializer == null)
            return;

        gameplayInitializer.SetServerMatchStartTime(startTime);
        currentSetStartTime = startTime;
    }

    /// <summary>클라이언트가 다음 세트 준비 시점에 시작 시각을 한 번 더 맞춤.</summary>
    private void SyncSetStartTimeClient()
    {
        if (IsServer || gameplayInitializer == null || NetworkManager == null)
            return;

        currentSetStartTime = (float)NetworkManager.ServerTime.Time; // 클라가 받을 때는 서버 RPC로 이미 전달됨. 이 줄은 안전용.
    }

    /// <summary>팀원 채널에서 풍선 피격 보고를 받았을 때 서버가 남은 풍선을 차감.</summary>
    private void HandleBalloonHitFromChannel(ulong ownerClientId, int balloonIndex)
    {
        if (!IsServer || gameEnded || setEnding)
            return;

        ProcessBalloonPop(ownerClientId);
    }
}
