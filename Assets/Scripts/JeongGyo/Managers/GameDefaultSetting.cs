using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Attachment;



[System.Serializable]
public class HandBindings
{
    public InputActionReference shootPrimary;
    public InputActionReference shootSecondary;
    public InputActionReference grab;
}

public enum Hand { Right, Left }

[CreateAssetMenu(fileName = "GameDefaultSetting", menuName = "Scriptable Objects/GameDefaultSetting")]
public class GameDefaultSetting : ScriptableObject
{
    [Header("Audio")]
    public AudioClip  backgroundMusic;
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    public bool muteAll = false;

    [Header("UI Settings")] // UI 관련 기본 설정
    public int targetFPS = 90;
    public float uiScale = 1.0f;
    public float uiDistance = 2.0f;
    public float uiFadeDuration = 0.5f;

    [Header("Pre-MultiGame Options")] // 멀티게임 시작 전 기본 설정
    public int defaultBalloonCount = 3;
    public readonly int defaultMaxBalloonCount = 5;
    public int roundSetting = 3;

    [Header("Control Bindings")]// 컨트롤 바인딩 설정
    public HandBindings rightHandMainBindings;
    public HandBindings leftHandMainBindings;

    [Header("Player Settings")]
    public Hand mainHand = Hand.Right; // 사용자가 선택 가능 (오른/왼)
    public bool smoothTurnEnabled = true; // 부드러운 회전 활성화 여부

    // 현재 선택된 주손/보조손 바인딩 반환
    public HandBindings MainBindings => mainHand == Hand.Right ? rightHandMainBindings : leftHandMainBindings;
    public HandBindings SubBindings => mainHand == Hand.Right ? leftHandMainBindings : rightHandMainBindings;





}