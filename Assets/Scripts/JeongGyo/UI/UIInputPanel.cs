using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIInputPanel : MonoBehaviour
{
    [SerializeField] private string gamePlaySceneName = "DefaultGameScene";
    [SerializeField] private TMP_Dropdown setCountDropdown;
    [SerializeField] private Slider balloonCountSlider;
    [SerializeField] private Slider timeLimitSlider;
    [SerializeField] private int timeStepSeconds = 30;
    [SerializeField] private GameSceneLoadManager sceneLoadManager;
    [SerializeField] private Button createRoomButton;
    private int[] setCountValues = new int[] { 1, 3, 5 }; // Best-of-one, Best-of-three, Best-of-five 순서
   
    int setCount; // 맵 세트 개수
    int balloonCount; // 풍선 개수
    int timeLimit; // 제한 시간 (초)
  
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

    public void CreateRoom()
    {
        if (sceneLoadManager == null)
        {
            Debug.LogError("UIInputPanel: sceneLoadManager 참조가 없습니다. 인스펙터에서 할당해 주세요.", this);
            return;
        }
        
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
        sceneLoadManager.LoadGameScene(config);
    }

    void OnDestroy()
    {
        createRoomButton.onClick.RemoveListener(CreateRoom);
    }

    private int ResolveSetCount(int dropdownIndex)
    {
        if (setCountValues != null && dropdownIndex >= 0 && dropdownIndex < setCountValues.Length)
            return setCountValues[dropdownIndex];

        // fallback: 드롭다운 인덱스+1을 사용(최소 1세트)
        return Mathf.Max(1, dropdownIndex + 1);
    }
}
