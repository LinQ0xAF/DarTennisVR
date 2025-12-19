using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System;

public class UISingleMatchInputPanel : MonoBehaviour
{
    [SerializeField] public string gamePlaySceneName = "DefaultGameScene";
    [SerializeField] private TMP_Dropdown setCountDropdown;
    [SerializeField] private Slider balloonCountSlider;
    [SerializeField] private Slider timeLimitSlider;
    [SerializeField] private int timeStepSeconds = 30;
    [SerializeField] private Button createRoomButton;
    private int[] setCountValues = new int[] { 1, 3, 5 }; // Best-of-one, Best-of-three, Best-of-five 순서

    [Header("References")]
    [SerializeField] private RoomConfigSO presetConfig; // 프리셋 SO에서 기본값을 읽어온다
    [SerializeField] private SingleMatchLoader singleMatchLoader; // 임시 로컬 매칭 컨트롤러


    /// <summary>
    /// UI 초기값을 읽어와 내부 상태를 세팅한다.
    /// </summary>
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
        singleMatchLoader.LoadSingleGame(UpdateConfigFromInputs(), gamePlaySceneName);
    }

    private RoomConfigDto UpdateConfigFromInputs()
    {
        RoomConfigDto configBuffer = presetConfig.ToDtoFromPreset(); // UI 입력을 담아 전송할 DTO 초기값 넣어서 생성

        configBuffer.setCount = setCountValues[setCountDropdown.value];
        configBuffer.balloonCount = (int)balloonCountSlider.value;
        configBuffer.timeLimitSeconds = (int)(timeLimitSlider.value * timeStepSeconds);

        Debug.Log($"[UIInputSinglePanel] RoomConfig DTO -> Sets:{configBuffer.setCount}, Balloons:{configBuffer.balloonCount}, Time:{configBuffer.timeLimitSeconds}s", this);

        return configBuffer;
    }
}
