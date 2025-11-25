using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class UIInputMultiPanel : MonoBehaviour
{
    [SerializeField] private string gamePlaySceneName = "DefaultGameScene";
    [SerializeField] private TMP_Dropdown setCountDropdown;
    [SerializeField] private Slider balloonCountSlider;
    [SerializeField] private Slider timeLimitSlider;
    [SerializeField] private int timeStepSeconds = 30;
    [SerializeField] private MultiRoomNetController roomNetController; // 서버/호스트에 붙은 컨트롤러
    [SerializeField] private LocalMatchmaker localMatchmaker; // 임시 로컬 매칭 컨트롤러
    [SerializeField] private Button createRoomButton;
    [Header("Set count mapping (dropdown index -> set count)")]
    [SerializeField] private int[] setCountValues = new int[] { 1, 3, 5 }; // Best-of-one, Best-of-three, Best-of-five 순서

    int setCount; // 맵 세트 개수
    string setLabel; // 맵 세트 라벨
    int balloonCount; // 풍선 개수
    int timeLimit; // 제한 시간 (초)
  
    void Start()
    {
        setCount = ResolveSetCount(setCountDropdown.value);
        setLabel = setCountDropdown.options.Count > setCountDropdown.value ? setCountDropdown.options[setCountDropdown.value].text : string.Empty;// 맵 세트 라벨
        balloonCount = (int)balloonCountSlider.value;
        timeLimit = (int)(timeLimitSlider.value * timeStepSeconds);
        
    }

    void OnEnable()
    {
        createRoomButton.onClick.AddListener(CreateRoom);
    }

    private MultiRoomNetController.RoomConfig pendingConfig; // 매칭 완료 후 StartMatch에 넘길 설정
    private bool isMatchmaking;

    public void CreateRoom()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("UIInputMultiPanel: NetworkManager Singleton을 찾을 수 없습니다.", this);
            return;
        }

        if (roomNetController == null || localMatchmaker == null)
        {
            Debug.LogError("UIInputMultiPanel: roomNetController 또는 localMatchmaker 참조가 없습니다. 인스펙터에서 할당해 주세요.", this);
            return;
        }

        // 첫 번째 진입자는 Host 시도, 실패 시 Client로 자동 전환
        if (!nm.IsListening && !nm.IsServer && !nm.IsClient)
        {
            if (!nm.StartHost())
            {
                Debug.LogWarning("[UIInputMultiPanel] StartHost 실패, Client로 재시도", this);
                if (!nm.StartClient())
                {
                    Debug.LogError("UIInputMultiPanel: StartClient까지 실패", this);
                    return;
                }
                Debug.Log("[UIInputMultiPanel] Client로 시작합니다.", this);
            }
            else
            {
                Debug.Log("[UIInputMultiPanel] Host로 시작합니다.", this);
            }
        }
        else if (!nm.IsServer && !nm.IsClient)
        {
            if (!nm.StartClient())
            {
                Debug.LogError("UIInputMultiPanel: StartClient 실패", this);
                return;
            }
            Debug.Log("[UIInputMultiPanel] Client로 시작합니다.", this);
        }

        // 입력값 새로 읽기
        setCount = ResolveSetCount(setCountDropdown.value);
        setLabel = setCountDropdown.options.Count > setCountDropdown.value ? setCountDropdown.options[setCountDropdown.value].text : string.Empty;
        balloonCount = (int)balloonCountSlider.value;
        timeLimit = (int)(timeLimitSlider.value * timeStepSeconds);
      
        var config = new MultiRoomNetController.RoomConfig
        {   
            gamePlaySceneName = gamePlaySceneName,
            balloonCount = balloonCount,
            timeLimitSeconds = timeLimit,
            setCount = setCount
        };

        // LocalMatchmaker가 아직 스폰되지 않았다면 스폰될 때까지 대기 후 Ready 전송
        if (localMatchmaker != null && !localMatchmaker.IsSpawned)
        {
            Debug.LogWarning("UIInputMultiPanel: LocalMatchmaker NetworkObject 스폰 대기 중입니다. 스폰 이후 Ready를 전송합니다.", this);
            StartCoroutine(WaitAndSendReady(config));
            return;
        }

        SendReady(config);
    }

    void OnDestroy()
    {
        createRoomButton.onClick.RemoveListener(CreateRoom);
    }

    private void SendReady(MultiRoomNetController.RoomConfig config)
    {
        // 내 Ready만 취소하고 새 설정으로 등록
        localMatchmaker.CancelReadyServerRpc();

        // 매칭 시스템 호출 전, 설정을 보관해 두고 ReadyServerRpc로 대기열에 등록한다.
        pendingConfig = config;
        isMatchmaking = true;
        Debug.Log($"[UIInputMultiPanel] 로컬 매칭 대기 등록 - Scene:{config.gamePlaySceneName}, Balloons:{config.balloonCount}, Time:{config.timeLimitSeconds}s", this);

        // 호스트/서버에 Ready 신호 전송(로컬 두 클라 테스트용 임시 매칭)
        localMatchmaker.ReadyServerRpc(config.balloonCount, config.timeLimitSeconds, config.gamePlaySceneName, config.setCount);
    }

    private IEnumerator WaitAndSendReady(MultiRoomNetController.RoomConfig config)
    {
        while (localMatchmaker != null && !localMatchmaker.IsSpawned)
        {
            yield return null;
        }

        if (localMatchmaker == null || !localMatchmaker.IsSpawned)
        {
            Debug.LogWarning("UIInputMultiPanel: LocalMatchmaker 스폰을 기다렸지만 실패했습니다.", this);
            yield break;
        }

        SendReady(config);
    }

    private int ResolveSetCount(int dropdownIndex)
    {
        if (setCountValues != null && dropdownIndex >= 0 && dropdownIndex < setCountValues.Length)
            return setCountValues[dropdownIndex];

        // fallback: 드롭다운 인덱스+1을 사용(최소 1세트)
        return Mathf.Max(1, dropdownIndex + 1);
    }
}
