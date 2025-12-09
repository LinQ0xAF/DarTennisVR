using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 세트 진행/승패 판정/타이머 관리 전담.
/// - 서버에서 세트 흐름을 결정하고 ClientRpc로 동기화한다.
/// - 매치 승패나 다음 세트 진행 여부는 MatchManager가 결정한다.
/// </summary>
public class SetManager : NetworkBehaviour
{
    [Header("Defaults (fallback if MatchManager doesn't override)")]
    [SerializeField, Min(1)] private int initialTotalSets = 1;
    [SerializeField, Min(1)] private int initialBalloonsPerPlayer = 1;
    [SerializeField] private float setEndPauseSeconds = 3f;
    [SerializeField] private int initialTimeLimitSeconds = 60;

    private bool matchEnded;
    private bool setEnding;
    private int currentSetIndex = 1;
    private int totalSets = 1;
    private int balloonsPerPlayer = 1;
    private int configuredTimeLimitSeconds = 60;
    private ulong? lastSetWinnerClientId;
    private ulong? player1ClientId;
    private ulong? player2ClientId;

    private readonly Dictionary<ulong, int> balloonsRemaining = new Dictionary<ulong, int>(); // 서버에서만 사용
    private readonly List<NetworkBalloonManager> allBalloonManagers = new List<NetworkBalloonManager>(); // [Server] 캐싱된 풍선 매니저 목록
    private readonly NetworkVariable<float> networkSetStartTime = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> hasNetworkStartTime = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Server);
    // private readonly Dictionary<ulong, int> setWinCounts = new Dictionary<ulong, int>(); // Removed: MatchManager handles this

    /// <summary>세트 결과를 알리는 이벤트(승자 clientId, 무승부면 null).</summary>
    public event Action<ulong?> OnSetResult;
    /// <summary>세트 수가 설정될 때 알림.</summary>
    public event Action<int> OnSetsConfigured;
    /// <summary>세트 종료 알림(점수 집계, UI 등).</summary>
    public event Action OnSetEnd;
    /// <summary>다음 세트 준비 시 실행(풍선 리셋 등).</summary>
    public event Action OnPrepareNextSet;
    /// <summary>세트 시작 직전(카운트다운 등) 알림.</summary>
    public event Action OnSetPreStart;

    public int TotalSets => totalSets;
    public int TimeLimitSeconds => configuredTimeLimitSeconds;

    private void Awake()
    {
        totalSets = Mathf.Max(1, initialTotalSets);
        balloonsPerPlayer = Mathf.Max(1, initialBalloonsPerPlayer);
        configuredTimeLimitSeconds = initialTimeLimitSeconds;
        currentSetIndex = Mathf.Clamp(currentSetIndex, 1, totalSets);
    }

    /// <summary>매치매니저에서 플레이어 1/2의 clientId를 전달.</summary>
    public void SetPlayerClientIds(ulong? p1ClientId, ulong? p2ClientId)
    {
        player1ClientId = p1ClientId;
        player2ClientId = p2ClientId;
    }

    public void ApplySettings(int totalSetCount, int balloonsPerPlayerCount, float pauseSeconds, int timeLimitSeconds, int startSetIndex = 1) // 서버에서 룸 설정에 따라 세트 매니저 설정 적용
    {
        totalSets = Mathf.Max(1, totalSetCount);
        balloonsPerPlayer = Mathf.Max(1, balloonsPerPlayerCount);
        setEndPauseSeconds = pauseSeconds;
        configuredTimeLimitSeconds = timeLimitSeconds;
        currentSetIndex = Mathf.Clamp(startSetIndex, 1, totalSets);
        OnSetsConfigured?.Invoke(totalSets);
    }

    public void NotifyMatchEnded()
    {
        matchEnded = true;
    }

    /// <summary>씬 로드 완료 후 서버에서 호출: 풍선/시간 등 세트 초기화.</summary>
    public void StartSetFlow()
    {
        if (!IsServer)
            return;

        matchEnded = false;
        setEnding = false;
        CacheBalloonManagers();
        ResetBalloonCountsForSet();
        ResetAllBalloons();
        BroadcastSetPreStart();
        SyncSetStartTime();
    }

    void Update()
    {
        if (!IsServer || matchEnded)
            return;

        if (!hasNetworkStartTime.Value || NetworkManager.Singleton == null)
            return;

        float elapsed = (float)(NetworkManager.Singleton.ServerTime.Time - networkSetStartTime.Value); // 경과 시간
        float remain = configuredTimeLimitSeconds - elapsed; // 남은 시간

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
        OnSetsConfigured?.Invoke(totalSets);
    }

    /// <summary>서버 기준으로 풍선 피격을 처리하고 잔여 수가 0이면 세트 종료를 트리거.</summary>
    public void ProcessBalloonPop(ulong senderClientId)
    {
        if (!IsServer || matchEnded || setEnding)
            return;

        if (!balloonsRemaining.ContainsKey(senderClientId))
            balloonsRemaining[senderClientId] = balloonsPerPlayer;

        if (balloonsRemaining[senderClientId] <= 0)
            return;

        balloonsRemaining[senderClientId] = Mathf.Max(0, balloonsRemaining[senderClientId] - 1);
        Debug.Log($"[SetManager] Server balloon pop | sender:{senderClientId} | remain:{balloonsRemaining[senderClientId]}");

        if (balloonsRemaining[senderClientId] == 0)
        {
            ulong? winnerId = EvaluateWinnerByRemaining();
            StartSetEndSequence("balloons_depleted", winnerId);
        }
    }

    /// <summary>세트 종료 시 공통 처리(서버에서 호출 후 클라에 브로드캐스트).</summary>
    private void StartSetEndSequence(string reason, ulong? winnerClientId)
    {
        if (setEnding || matchEnded)
            return;

        setEnding = true;
        lastSetWinnerClientId = winnerClientId;

        string winnerText = winnerClientId.HasValue ? winnerClientId.Value.ToString() : "draw";
        Debug.Log($"[SetManager] 세트 종료 감지(reason:{reason}) | winner:{winnerText} | current:{currentSetIndex}/{totalSets}");
        
        OnSetEnd?.Invoke(); // 승/패 UI는 여기 이벤트 구독
        OnSetResult?.Invoke(winnerClientId);
        // RecordSetWinner(winnerClientId); // Removed: MatchManager handles this

        // StartCoroutine(SetEndRoutine(hasNextSet, nextSetIndex, winnerClientId)); // Removed: MatchManager handles flow
        if (IsServer)
            SetEndClientRpc(winnerClientId.HasValue, winnerClientId.GetValueOrDefault());
    }

    [ClientRpc]
    private void SetEndClientRpc(bool hasWinner, ulong winnerClientId)
    {
        if (IsServer) return; // Server already fired events locally

        setEnding = true;
        lastSetWinnerClientId = hasWinner ? winnerClientId : (ulong?)null;
        string winnerText = hasWinner ? GetPlayerSlotText(winnerClientId) : "draw";
        Debug.Log($"[SetManager] 클라이언트 세트 종료 수신 | winner:{winnerText}");
        
        OnSetEnd?.Invoke();
        OnSetResult?.Invoke(lastSetWinnerClientId);
    }

    public void PrepareNextSet(int nextSetIndex) // 서버에서 다음 세트 준비 (Called by MatchManager)
    {
        setEnding = false; // Reset flag
        currentSetIndex = nextSetIndex;
        Debug.Log($"[SetManager] 다음 세트 준비 (set {nextSetIndex}/{totalSets})");

        OnPrepareNextSet?.Invoke();
        ResetAllBalloons(); // 모든 플레이어의 풍선 리셋
        ResetBalloonCountsForSet();
        BroadcastSetPreStart();
        SyncSetStartTime();
        PrepareNextSetClientRpc(nextSetIndex);
    }

    [ClientRpc]
    private void PrepareNextSetClientRpc(int nextSetIndex)
    {
        if (IsServer) return; // Server already handled locally

        setEnding = false; // Reset flag on client too
        currentSetIndex = nextSetIndex;
        Debug.Log($"[SetManager] 클라이언트에서 다음 세트 준비 수신 (set {nextSetIndex}/{totalSets})");
        OnPrepareNextSet?.Invoke();
    }

    /// <summary>[서버] 각 클라이언트의 잔여 풍선 수 딕셔너리 초기화.</summary>
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

    /// <summary>[서버] 캐싱된 모든 NetworkBalloonManager에 리셋 명령.</summary>
    private void ResetAllBalloons()
    {
        if (!IsServer) return;

        if (allBalloonManagers.Count == 0)
            CacheBalloonManagers();

        foreach (var bm in allBalloonManagers)
        {
            if (bm != null)
            {
                bm.Server_ResetBalloons(balloonsPerPlayer);
            }
        }
    }

    /// <summary>[서버] 접속한 각 클라이언트 PlayerObject에서 NetworkBalloonManager를 수집.</summary>
    private void CacheBalloonManagers()
    {
        allBalloonManagers.Clear();
        if (NetworkManager == null) return;

        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var bm = client.PlayerObject.GetComponentInChildren<NetworkBalloonManager>();
                if (bm != null)
                {
                    allBalloonManagers.Add(bm);
                }
            }
        }
    }

    /// <summary>[서버] 잔여 풍선 수로 승자 판정. 동점이면 null 반환.</summary>
    private ulong? EvaluateWinnerByRemaining()
    {
        if (!IsServer || balloonsRemaining.Count == 0)
            return null;

        // 1. 플레이어 슬롯(1P/2P) 기준 우선 판정
        if (player1ClientId.HasValue && player2ClientId.HasValue)
        {
            int p1Remain = balloonsRemaining.TryGetValue(player1ClientId.Value, out var r1) ? r1 : 0;
            int p2Remain = balloonsRemaining.TryGetValue(player2ClientId.Value, out var r2) ? r2 : 0;
            if (p1Remain > p2Remain) return player1ClientId.Value;
            if (p2Remain > p1Remain) return player2ClientId.Value;
            return null; // 동점
        }

        // 2. 슬롯 정보가 없으면 기존 방식(최대 잔여 풍선)으로 판정
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
        if (!IsServer || NetworkManager == null)
            return;

        float startTime = (float)NetworkManager.ServerTime.Time;
        networkSetStartTime.Value = startTime;
        hasNetworkStartTime.Value = true;
    }

    public bool HasSetStartTime() => hasNetworkStartTime.Value;
    public float GetSetStartTime() => networkSetStartTime.Value;
    /// <summary>서버 기준 흐른 시간(초). 시작 시각을 못 받았으면 0 반환.</summary>
    public float GetElapsedServerSeconds()
    {
        if (!hasNetworkStartTime.Value || NetworkManager.Singleton == null)
            return 0f;

        return (float)(NetworkManager.Singleton.ServerTime.Time - networkSetStartTime.Value);
    }

    /// <summary>플레이어 슬롯 텍스트(P1/P2 또는 clientId).</summary>
    private string GetPlayerSlotText(ulong clientId)
    {
        if (player1ClientId.HasValue && clientId == player1ClientId.Value)
            return "P1";
        if (player2ClientId.HasValue && clientId == player2ClientId.Value)
            return "P2";
        return clientId.ToString();
    }

    /// <summary>세트 시작 직전 이벤트 브로드캐스트(카운트다운 등).</summary>
    private void BroadcastSetPreStart()
    {
        OnSetPreStart?.Invoke();
        if (IsServer)
            SetPreStartClientRpc();
    }

    [ClientRpc]
    private void SetPreStartClientRpc()
    {
        if (IsServer || matchEnded)
            return;

        OnSetPreStart?.Invoke();
    }
}
