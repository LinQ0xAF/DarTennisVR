using System;
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
public class MatchManager : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private RoomConfigSO roomConfig; // 룸 설정(Runtime 포함)
    [SerializeField] private NetworkBalloonManager balloonManager; // 로컬 풍선 매니저 캐싱용(옵션)
    [SerializeField] private NetworkBalloonHitChannelSO balloonHitChannel; // 팀원 채널: 서버가 풍선 피격 보고 수신
    [SerializeField] private SpawnManager spawnManager; // 플레이어 아바타 스폰 담당
    [SerializeField] private SetManager setManager; // 세트 흐름 전담

    [Header("Set/Match Settings")]
    [SerializeField, Min(1)] private int totalSets = 1; // 인스펙터 기본 세트 수(초기화 시 룸 설정으로 대체)
    [SerializeField, Min(1)] private int balloonsPerPlayer = 1; // 플레이어당 시작 풍선 수(씬 설정이 우선)
    [SerializeField] private float setEndPauseSeconds = 3f; // 세트 종료 후 잠시 멈추는 시간
    [SerializeField] private float matchEndWaitSeconds = 5f; // 매치 종료 후 로비 이동 전 대기 시간
    [SerializeField] private int configuredTimeLimitSeconds = 60; // 세트 제한 시간(초)

    [Header("Scene Flow")]
    [SerializeField] private string lobbySceneName = "Entrance_Alpha"; // 모든 세트 종료 후 돌아갈 씬 이름(로비 씬 로드)
    [SerializeField] private bool returnToLobbyOnMatchEnd = true;

    [Header("Events (optional)")]
    [SerializeField] private UnityEvent onSetEnd; // 세트 종료 알림(점수 집계, UI 등)
    [SerializeField] public UnityEvent onPrepareNextSet; // 다음 세트 준비 시 실행(풍선 리셋 등)
    [SerializeField] private UnityEvent onTimeUp; // 매치 종료/타임업 시 실행

    private bool gameEnded;
    private ulong? player1ClientId;
    private ulong? player2ClientId;
    private readonly List<ulong> playerClientIds = new List<ulong>();
    private bool setEventsWired;
    private int currentSetIndex = 1; // Track current set index in MatchManager
    private readonly Dictionary<ulong, int> matchScore = new Dictionary<ulong, int>(); // Track match score

    /// <summary>세트 결과를 알리는 이벤트(승자 clientId, 무승부면 null).</summary>
    public event Action<ulong?> OnSetResult;
    /// <summary>세트 수가 설정될 때 알림.</summary>
    public event Action<int> OnSetsConfigured;
    /// <summary>매치 최종 결과 알림(무승부면 null).</summary>
    public event Action<ulong?> OnMatchResult;
    /// <summary>세트 시작 직전(카운트다운 등) 알림.</summary>
    public event Action OnSetPreStart;
    public int TimeLimitSeconds => setManager != null ? setManager.TimeLimitSeconds : configuredTimeLimitSeconds;
    public int TotalSets => setManager != null ? setManager.TotalSets : totalSets;

    private void Awake()
    {
        // 세트 매니저 컴포넌트 확보(인스펙터 미지정 시 자동 할당)
        if (setManager == null)
            setManager = GetComponent<SetManager>();
    }
    

    // 참고: 풍선 피격 채널 및 클라이언트 접속 이벤트 구독
    void OnEnable()
    {
        // 풍선 피격 이벤트 구독
        if (balloonHitChannel != null)
            balloonHitChannel.OnPlayerHit += HandleBalloonHitFromChannel; //풍선이 다트 맞았을때 터지느 로직 부여

        // 세트 매니저 이벤트 구독
        WireSetManagerEvents();
    }

    void OnDisable()
    {
        // 풍선 피격 이벤트 해제
        if (balloonHitChannel != null)
            balloonHitChannel.OnPlayerHit -= HandleBalloonHitFromChannel;

        // 세트 매니저 이벤트 해제
        UnwireSetManagerEvents();
    }

    public override void OnNetworkSpawn()
    { 
        // 가능한 빨리 설정 반영 시도
        ConfigureFromRoomConfig();

        // 씬 로드 완료 시 서버가 접속 클라이언트 목록을 기반으로 스폰 처리
        if (IsServer && NetworkManager != null)
        {
            if (NetworkManager.SceneManager != null)
                NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;

            NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    public override void OnNetworkDespawn()
    {
        // 씬 로드 완료 콜백 해제
        if (IsServer && NetworkManager != null)
        {
            if (NetworkManager.SceneManager != null)
                NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;

            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer || gameEnded) return;

        // 플레이어 중 한 명이 나갔는지 확인
        if (player1ClientId.HasValue && clientId == player1ClientId.Value)
        {
            Debug.Log($"[MatchManager] P1({clientId}) Disconnected. P2 Wins.");
            EndGame(player2ClientId);
        }
        else if (player2ClientId.HasValue && clientId == player2ClientId.Value)
        {
            Debug.Log($"[MatchManager] P2({clientId}) Disconnected. P1 Wins.");
            EndGame(player1ClientId);
        }
    }

    // [Server] 씬 로드 완료 시 서버가 접속 클라이언트 목록을 기반으로 스폰 처리
    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        if (roomConfig != null && roomConfig.runtimeConfig == null) 
            return;

        if (spawnManager == null)
            spawnManager = FindFirstObjectByType<SpawnManager>();

        playerClientIds.Clear();
        
        if (clientsCompleted != null)
            playerClientIds.AddRange(clientsCompleted);
       
        else if (NetworkManager != null)
            playerClientIds.AddRange(NetworkManager.ConnectedClientsIds);


        player1ClientId = playerClientIds[0]; // 첫 접속자가 1P
        player2ClientId = playerClientIds[1]; // 두 번째 접속자가 2P
        setManager?.SetPlayerClientIds(player1ClientId, player2ClientId);

        for (int i = 0; i < playerClientIds.Count; i++) // 모든 접속 클라이언트에 대해 스폰
        {
            var clientId = playerClientIds[i];

            // 접속 완료 순서를 스폰 순서로 사용
            spawnManager.SpawnForClient(clientId, i);
        }
        // 서버 측 준비 완료 시점 확인 및 풍선 초기화/동기화
        StartGameRoutine();
    }

    /// <summary>초기 세트/풍선 상태를 맞추기 위해 설정과 PlayerObject 스폰을 기다렸다가 설정.</summary>
    private void StartGameRoutine()
    {
        // 서버에서만 세트 흐름 시작
        if (IsServer)
        {
            setManager.StartSetFlow();
        }
    }

    private void ConfigureFromRoomConfig() // 룸 설정이 있으면 세트 수/풍선 수/타임리밋 반영
    {
        if (roomConfig != null && roomConfig.runtimeConfig != null)
        {
            var cfg = roomConfig.runtimeConfig;
            configuredTimeLimitSeconds = cfg.timeLimitSeconds;
            totalSets = cfg.setCount; // Update local field
            balloonsPerPlayer = cfg.balloonCount; // Update local field
        }

        setManager.ApplySettings(totalSets, balloonsPerPlayer, setEndPauseSeconds, configuredTimeLimitSeconds);
    }

    /// <summary>팀원 채널에서 풍선 피격 보고를 받았을 때 서버가 남은 풍선을 차감.</summary>
    private void HandleBalloonHitFromChannel(ulong ownerClientId, int balloonIndex)
    {
        // 서버에서 세트 매니저에 전달해 풍선 카운트 감소
        if (setManager != null)
            setManager.ProcessBalloonPop(ownerClientId);
    }

    /// <summary>시간 만료/세트 모두 소진 시 호출. 서버에서 ClientRpc로 알림.</summary>
    public void EndGame(ulong? matchWinner = null)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        Debug.Log($"[GameManager] 매치 종료 - EndGame 실행 (Winner: {matchWinner})");

        // 세트 매니저에 종료 알림
        setManager?.NotifyMatchEnded();
        
        OnMatchResult?.Invoke(matchWinner); // Local event

        if (IsServer)
        {
            EndGameClientRpc(matchWinner.HasValue, matchWinner.GetValueOrDefault());
        }

        onTimeUp?.Invoke();

        // 매치 종료 후 잠시 대기했다가 로비로 이동
        StartCoroutine(EndGameRoutine());
    }

    /// <summary>
    /// 로비로 즉시 이동한다.
    /// - Server: 모든 클라이언트를 데리고 씬 이동
    /// - Client: 네트워크 연결을 끊고 로컬 씬 이동
    /// </summary>
    public void ReturnToLobby()
    {
        if (IsServer)
        {
            if (!string.IsNullOrWhiteSpace(lobbySceneName))
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
        else
        {
            // Client: Disconnect and load local
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    private IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(matchEndWaitSeconds);

        if (IsServer && returnToLobbyOnMatchEnd)
        {
            ReturnToLobby();
        }
    }


    [ClientRpc]
    private void EndGameClientRpc(bool hasWinner, ulong winnerId)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        ulong? matchWinner = hasWinner ? winnerId : (ulong?)null;
        
        // 클라이언트에서도 세트 매니저 종료 플래그 동기화
        setManager?.NotifyMatchEnded();
        
        OnMatchResult?.Invoke(matchWinner); // Local event
        
        Debug.Log("[GameManager] 클라이언트에서 매치 종료 수신 - EndGame 실행");
        onTimeUp?.Invoke();
    }

    private void WireSetManagerEvents()
    {
        if (setManager == null || setEventsWired)
            return;

        // 세트 이벤트를 매치 매니저/UnityEvent로 릴레이
        setManager.OnSetEnd += HandleSetEnd;
        setManager.OnPrepareNextSet += HandlePrepareNextSet;
        setManager.OnSetResult += HandleSetResult; // Changed from RelaySetResult
        setManager.OnSetsConfigured += RelaySetsConfigured;
        // setManager.OnMatchEndRequested += HandleMatchEndRequested; // Removed
        setManager.OnSetPreStart += RelaySetPreStart;
        setEventsWired = true;
    }

    private void UnwireSetManagerEvents()
    {
        if (setManager == null || !setEventsWired)
            return;

        // 세트 이벤트 구독 해제
        setManager.OnSetEnd -= HandleSetEnd;
        setManager.OnPrepareNextSet -= HandlePrepareNextSet;
        setManager.OnSetResult -= HandleSetResult; // Changed from RelaySetResult
        setManager.OnSetsConfigured -= RelaySetsConfigured;
        // setManager.OnMatchEndRequested -= HandleMatchEndRequested; // Removed
        setManager.OnSetPreStart -= RelaySetPreStart;
        setEventsWired = false;
    }
    
    // 세트 종료 UnityEvent 호출
    private void HandleSetEnd() => onSetEnd?.Invoke();
    // 다음 세트 준비 UnityEvent 호출
    private void HandlePrepareNextSet() => onPrepareNextSet?.Invoke();
    
    // 세트 결과 처리 (Server Logic + Relay)
    private void HandleSetResult(ulong? winnerClientId)
    {
        OnSetResult?.Invoke(winnerClientId); // Relay to public event

        if (IsServer)
        {
            ProcessSetResultServer(winnerClientId);
        }
    }

    private void ProcessSetResultServer(ulong? winnerClientId)
    {
        if (winnerClientId.HasValue)
        {
            if (!matchScore.ContainsKey(winnerClientId.Value))
                matchScore[winnerClientId.Value] = 0;
            matchScore[winnerClientId.Value]++;
        }

        StartCoroutine(MatchFlowRoutine());
    }

    private IEnumerator MatchFlowRoutine()
    {
        yield return new WaitForSeconds(setEndPauseSeconds);

        if (gameEnded) yield break;

        // Check Win Condition
        bool matchOver = false;
        ulong? matchWinner = null;

        // Condition 1: All sets played
        if (currentSetIndex >= totalSets)
        {
            matchOver = true;
        }
        
        // Condition 2: Majority win (Best of N)
        int majority = (totalSets / 2) + 1;
        foreach(var kv in matchScore) 
        { 
            if(kv.Value >= majority) 
            { 
                matchOver = true; 
                matchWinner = kv.Key; 
                break;
            } 
        }

        if (matchOver)
        {
             if (matchWinner == null) matchWinner = EvaluateMatchWinner();
             EndGame(matchWinner);
        }
        else
        {
             currentSetIndex++;
             setManager.PrepareNextSet(currentSetIndex);
        }
    }

    private ulong? EvaluateMatchWinner()
    {
        ulong? bestClient = null;
        int bestWins = int.MinValue;
        bool tie = false;

        foreach (var kv in matchScore)
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

        if (tie) return null;
        return bestClient;
    }

    // 세트 수 설정 C# 이벤트 릴레이
    private void RelaySetsConfigured(int setCount) => OnSetsConfigured?.Invoke(setCount);
    // 세트 시작 직전 C# 이벤트 릴레이
    private void RelaySetPreStart() => OnSetPreStart?.Invoke();

    // UI에서 세트 시작 시각 수신 여부 확인
    public bool HasSetStartTime() => setManager != null && setManager.HasSetStartTime();
    // UI에서 세트 시작 시각 조회
    public float GetSetStartTime() => setManager != null ? setManager.GetSetStartTime() : 0f;
    /// <summary>서버 기준 흐른 시간(초). 시작 시각을 못 받았으면 0 반환.</summary>
    public float GetElapsedServerSeconds()
    {
        // 세트 매니저 없으면 0 반환
        if (setManager == null)
            return 0f;

        return setManager.GetElapsedServerSeconds();
    }

}
