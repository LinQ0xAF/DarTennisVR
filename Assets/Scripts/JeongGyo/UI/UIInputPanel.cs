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


    public void CreateRoom()
    {
        if (sceneLoadManager == null)
        {
            Debug.LogError("UIInputPanel: sceneLoadManager 참조가 없습니다. 인스펙터에서 할당해 주세요.", this);
            return;
        }
        
        setCount = setCountDropdown.value;
        setLabel = setCountDropdown.options.Count > setCount ? setCountDropdown.options[setCount].text : string.Empty;
        balloonCount = (int)balloonCountSlider.value;
        timeLimit = (int)(timeLimitSlider.value * timeStepSeconds);
      
        var config = new GameSceneLoadManager.RoomConfig
        {
            setIndex = setCount,
            setLabel = setLabel,
            balloonCount = balloonCount,
            timeLimitSeconds = timeLimit
           
        };
            sceneLoadManager.LoadGameScene(config);
    }
}
