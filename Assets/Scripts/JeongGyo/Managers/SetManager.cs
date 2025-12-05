using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 세트 진행/승패 판정/타이머 관리 전담.
/// - 서버에서 세트 흐름을 결정하고 ClientRpc로 동기화한다.
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
    private ulong? matchWinnerClientId;

    private readonly Dictionary<ulong, int> balloonsRemaining = new Dictionary<ulong, int>(); // 서버에서만 사용
    private readonly List<NetworkBalloonManager> allBalloonManagers = new List<NetworkBalloonManager>(); // [Server] 캐싱된 풍선 매니저 목록
    private readonly NetworkVariable<float> networkSetStartTime = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> hasNetworkStartTime = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Server);
    private readonly Dictionary<ulong, int> setWinCounts = new Dictionary<ulong, int>(); // 세트별 승리 수 집계

    /// <summary>세트 결과를 알리는 이벤트(승자 clientId, 무승부면 null).</summary>
    public event Action<ulong?> OnSetResult;
    /// <summary>세트 수가 설정될 때 알림.</summary>
    public event Action<int> OnSetsConfigured;
    /// <summary>세트 종료 알림(점수 집계, UI 등).</summary>
    public event Action OnSetEnd;
    /// <summary>다음 세트 준비 시 실행(풍선 리셋 등).</summary>
    public event Action OnPrepareNextSet;
    /// <summary>모든 세트 종료/타임업으로 매치 종료가 필요할 때 알림.</summary>
    public event Action OnMatchEndRequested;
    /// <summary>세트 시작 직전(카운트다운 등) 알림.</summary>
    public event Action OnSetPreStart;
    /// <summary>매치 최종 승자 알림(무승부면 null).</summary>
    public event Action<ulong?> OnMatchResult;

    public int TotalSets => totalSets;
    public int TimeLimitSeconds => configuredTimeLimitSeconds;
    public ulong? MatchWinnerClientId => matchWinnerClientId;

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
        matchWinnerClientId = null;
        setWinCounts.Clear();
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
        bool hasNextSet = currentSetIndex < totalSets;
        int nextSetIndex = hasNextSet ? currentSetIndex + 1 : currentSetIndex;
        lastSetWinnerClientId = winnerClientId;

        string winnerText = winnerClientId.HasValue ? winnerClientId.Value.ToString() : "draw";
        Debug.Log($"[SetManager] 세트 종료 감지(reason:{reason}) | winner:{winnerText} | current:{currentSetIndex}/{totalSets} | next:{nextSetIndex} | hasNext:{hasNextSet}");
        OnSetEnd?.Invoke(); // 승/패 UI는 여기 이벤트 구독
        OnSetResult?.Invoke(winnerClientId);
        RecordSetWinner(winnerClientId);

        StartCoroutine(SetEndRoutine(hasNextSet, nextSetIndex, winnerClientId));
        if (IsServer)
            SetEndClientRpc(hasNextSet, nextSetIndex, winnerClientId.HasValue, winnerClientId.GetValueOrDefault());
    }

    private IEnumerator SetEndRoutine(bool hasNextSet, int nextSetIndex, ulong? winnerClientId)
    {
        yield return new WaitForSeconds(setEndPauseSeconds);

        setEnding = false;
        if (matchEnded)
            yield break;

        if (hasNextSet)
        {
            PrepareNextSet(nextSetIndex);
        }
        else if (IsServer)
        {
            matchWinnerClientId = EvaluateMatchWinnerBySetWins();
            OnMatchResult?.Invoke(matchWinnerClientId);
            RequestMatchEnd();
        }
    }

    [ClientRpc]
    private void SetEndClientRpc(bool hasNextSet, int nextSetIndex, bool hasWinner, ulong winnerClientId)
    {
        if (IsServer || matchEnded)
            return;

        setEnding = true;
        lastSetWinnerClientId = hasWinner ? winnerClientId : (ulong?)null;
        string winnerText = hasWinner ? GetPlayerSlotText(winnerClientId) : "draw";
        Debug.Log($"[SetManager] 클라이언트 세트 종료 수신 | winner:{winnerText} | next:{nextSetIndex} | hasNext:{hasNextSet}");
        OnSetEnd?.Invoke();
        OnSetResult?.Invoke(lastSetWinnerClientId);
        StartCoroutine(SetEndRoutine(hasNextSet, nextSetIndex, lastSetWinnerClientId));
    }

    private void PrepareNextSet(int nextSetIndex) // 서버에서 다음 세트 준비
    {
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
        if (IsServer || matchEnded)
            return;

        currentSetIndex = nextSetIndex;
        Debug.Log($"[SetManager] 클라이언트에서 다음 세트 준비 수신 (set {nextSetIndex}/{totalSets})");
        OnPrepareNextSet?.Invoke();
    }

    private void RequestMatchEnd()
    {
        if (matchEnded)
            return;

        matchEnded = true;
        OnMatchResult?.Invoke(matchWinnerClientId);
        OnMatchEndRequested?.Invoke();
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

    /// <summary>세트 승자 기록(무승부 제외).</summary>
    private void RecordSetWinner(ulong? winnerClientId)
    {
        if (!winnerClientId.HasValue)
            return;

        if (!setWinCounts.ContainsKey(winnerClientId.Value))
            setWinCounts[winnerClientId.Value] = 0;

        setWinCounts[winnerClientId.Value]++;
    }

    /// <summary>세트 승수 기반 최종 승자 계산(동률이면 null).</summary>
    private ulong? EvaluateMatchWinnerBySetWins()
    {
        if (setWinCounts.Count == 0)
            return null;

        // 플레이어 1/2 둘 다 존재하면 둘 사이의 승수를 비교
        if (player1ClientId.HasValue && player2ClientId.HasValue)
        {
            int p1Wins = setWinCounts.TryGetValue(player1ClientId.Value, out var w1) ? w1 : 0;
            int p2Wins = setWinCounts.TryGetValue(player2ClientId.Value, out var w2) ? w2 : 0;
            if (p1Wins > p2Wins) return player1ClientId.Value;
            if (p2Wins > p1Wins) return player2ClientId.Value;
            return null; // 동률
        }

        // 일반 케이스: 가장 승수가 많은 클라이언트
        ulong? bestClient = null;
        int bestWins = int.MinValue;
        bool tie = false;

        foreach (var kv in setWinCounts)
        {
            if (kv.Value > bestWins)
            {
                bestWins = kv.Value;
                bestClient = kv.Key;
                tie = false;
            }
            else if (kv.Value == bestWins)
            {
                tie = true;
            }
        }

        if (tie)
            return null;

        return bestClient;
    }
}
