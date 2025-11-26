using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class TempUIInputMultiPanel : MonoBehaviour
{
    [SerializeField] private string gamePlaySceneName = "DefaultGameScene";
    [SerializeField] private TMP_Dropdown setCountDropdown;
    [SerializeField] private Slider balloonCountSlider;
    [SerializeField] private Slider timeLimitSlider;
    [SerializeField] private int timeStepSeconds = 30;
    [SerializeField] private Button createRoomButton;
    private int[] setCountValues = new int[] { 1, 3, 5 }; // Best-of-one, Best-of-three, Best-of-five 순서
    
    [Header("References")]
    [SerializeField] private TempLocalMatchmaker localMatchmaker; // 임시 로컬 매칭 컨트롤러 (Temp 버전)

    int setCount; // 맵 세트 개수
    int balloonCount; // 풍선 개수
    int timeLimit; // 제한 시간 (초)
    
    /// <summary>
    /// UI 초기값을 읽어와 내부 상태를 세팅한다.
    /// </summary>
    void Start()
    {
        setCount = ResolveSetCount(setCountDropdown.value);
        balloonCount = (int)balloonCountSlider.value;
        timeLimit = (int)(timeLimitSlider.value * timeStepSeconds);
        
    }

    void OnEnable()
    {
        createRoomButton.onClick.AddListener(CreateRoom);
    }

    void OnDestroy()
    {
        createRoomButton.onClick.RemoveListener(CreateRoom);
    }

    /// <summary>
    /// 룸 생성/매칭 시작 엔트리. 호스트/클라 시작 후 ReadyServerRpc 호출.
    /// </summary>
    public void CreateRoom()
    {
        // 입력값 새로 읽기
        setCount = ResolveSetCount(setCountDropdown.value);
        balloonCount = (int)balloonCountSlider.value;
        timeLimit = (int)(timeLimitSlider.value * timeStepSeconds);
      
        var config = new RoomConfigDto
        {   
            gamePlaySceneName = gamePlaySceneName,
            balloonCount = balloonCount,
            timeLimitSeconds = timeLimit,
            setCount = setCount
        };

        // 네트워크 시작 및 Ready 전송을 LocalMatchmaker가 처리
        if (localMatchmaker == null)
        {
            Debug.LogError("TempUIInputMultiPanel: localMatchmaker 참조가 없습니다. 인스펙터에서 할당해 주세요.", this);
            return;
        }

        StartCoroutine(localMatchmaker.BeginLocalMatch(config));
    }

    /// <summary>
    /// 드롭다운 인덱스를 실제 세트 수로 매핑한다.
    /// </summary>
    private int ResolveSetCount(int dropdownIndex)
    {
        if (setCountValues != null && dropdownIndex >= 0 && dropdownIndex < setCountValues.Length)
            return setCountValues[dropdownIndex];

        // fallback: 드롭다운 인덱스+1을 사용(최소 1세트)
        return Mathf.Max(1, dropdownIndex + 1);
    }
}
