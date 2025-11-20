using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    
    int setCount; // 맵 세트 개수
    string setLabel; // 맵 세트 라벨
    int balloonCount; // 풍선 개수
    int timeLimit; // 제한 시간 (초)
  
    void Start()
    {
        setCount = setCountDropdown.value;
        setLabel = setCountDropdown.options.Count > setCount ? setCountDropdown.options[setCount].text : string.Empty;// 맵 세트 라벨
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
        if (roomNetController == null || localMatchmaker == null)
        {
            Debug.LogError("UIInputMultiPanel: roomNetController 또는 localMatchmaker 참조가 없습니다. 인스펙터에서 할당해 주세요.", this);
            return;
        }
        
        // 입력값 새로 읽기
        setCount = setCountDropdown.value;
        setLabel = setCountDropdown.options.Count > setCount ? setCountDropdown.options[setCount].text : string.Empty;
        balloonCount = (int)balloonCountSlider.value;
        timeLimit = (int)(timeLimitSlider.value * timeStepSeconds);
      
        var config = new MultiRoomNetController.RoomConfig
        {   
            gamePlaySceneName = gamePlaySceneName,
            balloonCount = balloonCount,
            timeLimitSeconds = timeLimit
        };

        // 매칭 시스템 호출 전, 설정을 보관해 두고 ReadyServerRpc로 대기열에 등록한다.
        pendingConfig = config;
        isMatchmaking = true;
        Debug.Log($"[UIInputMultiPanel] 로컬 매칭 대기 등록 - Scene:{config.gamePlaySceneName}, Balloons:{config.balloonCount}, Time:{config.timeLimitSeconds}s", this);

        // 호스트/서버에 Ready 신호 전송(로컬 두 클라 테스트용 임시 매칭)
        localMatchmaker.ReadyServerRpc(balloonCount, timeLimit, gamePlaySceneName);
    }

    void OnDestroy()
    {
        createRoomButton.onClick.RemoveListener(CreateRoom);
    }
}
