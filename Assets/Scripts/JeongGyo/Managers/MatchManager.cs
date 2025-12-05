using System;
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
[RequireComponent(typeof(SetManager))]
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
    [SerializeField] private int configuredTimeLimitSeconds = 60; // 세트 제한 시간(초)

    [Header("Scene Flow")]
    [SerializeField] private string lobbySceneName = "EnteranceTemp"; // 모든 세트 종료 후 돌아갈 씬 이름(네트워크 씬 로드)
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
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    public override void OnNetworkDespawn()
    {
        // 씬 로드 완료 콜백 해제
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
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
            setManager.ApplySettings(cfg.setCount, cfg.balloonCount, setEndPauseSeconds, configuredTimeLimitSeconds);
        }
        else
        {
            setManager.ApplySettings(totalSets, balloonsPerPlayer, setEndPauseSeconds, configuredTimeLimitSeconds);
        }
    }

    /// <summary>팀원 채널에서 풍선 피격 보고를 받았을 때 서버가 남은 풍선을 차감.</summary>
    private void HandleBalloonHitFromChannel(ulong ownerClientId, int balloonIndex)
    {
        // 서버에서 세트 매니저에 전달해 풍선 카운트 감소
        if (setManager != null)
            setManager.ProcessBalloonPop(ownerClientId);
    }

    /// <summary>시간 만료/세트 모두 소진 시 호출. 서버에서 ClientRpc로 알림.</summary>
    public void EndGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        Debug.Log("[GameManager] 매치 종료 - EndGame 실행");

        // 세트 매니저에 종료 알림
        setManager?.NotifyMatchEnded();

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
        // 클라이언트에서도 세트 매니저 종료 플래그 동기화
        setManager?.NotifyMatchEnded();
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
        setManager.OnSetResult += RelaySetResult;
        setManager.OnSetsConfigured += RelaySetsConfigured;
        setManager.OnMatchEndRequested += HandleMatchEndRequested;
        setManager.OnMatchResult += RelayMatchResult;
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
        setManager.OnSetResult -= RelaySetResult;
        setManager.OnSetsConfigured -= RelaySetsConfigured;
        setManager.OnMatchEndRequested -= HandleMatchEndRequested;
        setManager.OnMatchResult -= RelayMatchResult;
        setManager.OnSetPreStart -= RelaySetPreStart;
        setEventsWired = false;
    }

    private void HandleMatchEndRequested()
    {
        // 세트 매니저로부터 매치 종료 요청 수신
        EndGame();
    }

    // 세트 종료 UnityEvent 호출
    private void HandleSetEnd() => onSetEnd?.Invoke();
    // 다음 세트 준비 UnityEvent 호출
    private void HandlePrepareNextSet() => onPrepareNextSet?.Invoke();
    // 세트 결과 C# 이벤트 릴레이
    private void RelaySetResult(ulong? winnerClientId) => OnSetResult?.Invoke(winnerClientId);
    // 세트 수 설정 C# 이벤트 릴레이
    private void RelaySetsConfigured(int setCount) => OnSetsConfigured?.Invoke(setCount);
    // 매치 최종 결과 C# 이벤트 릴레이
    private void RelayMatchResult(ulong? winnerClientId) => OnMatchResult?.Invoke(winnerClientId);
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
