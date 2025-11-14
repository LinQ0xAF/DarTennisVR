using System.IO;
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
public class GamePersonalDataManager : ScriptableObject
{
    const string JsonFileName = "user-settings.json"; // Persistent save file name
    const string JsonFolderRelativePath = "Scripts/JeongGyo/Managers"; // Assets 폴더 기준 상대 경로

    // 제이슨으로 저장할 개인 설정
    [System.Serializable]
    public class PersonalSettings
    {
        public float masterVolume = 1f;       // 전체 볼륨
        public int mainHand = (int)Hand.Right;// 주손 (0=Right,1=Left)
        public bool smoothTurnEnabled = true; // 부드러운 회전 사용 여부

        public void CopyFrom(GamePersonalDataManager source) // ScriptableObject 값 -> 스냅샷 복사
        {
            masterVolume = source.masterVolume;
            mainHand = (int)source.mainHand;
            smoothTurnEnabled = source.smoothTurnEnabled;
        }

        public void ApplyTo(GamePersonalDataManager target) // 현재 값을  -> ScriptableObject 반영
        {
            target.masterVolume = masterVolume;
            target.mainHand = (Hand)mainHand;
            target.smoothTurnEnabled = smoothTurnEnabled;
        }
    }

    [Header("Audio")]
    public AudioClip  backgroundMusic;             // 기본 배경음 클립
    [Range(0f, 1f)] public float masterVolume = 1f;// 전체 볼륨

    [Header("Control Bindings")]
    public HandBindings rightHandMainBindings;     // 오른손 기본 입력
    public HandBindings leftHandMainBindings;      // 왼손 기본 입력

    [Header("Player Settings")]
    public Hand mainHand = Hand.Right;             // 주손 설정
    public bool smoothTurnEnabled = true;          // 부드러운 회전 여부

    [Header("Persisted User Settings")]
    [SerializeField, HideInInspector] PersonalSettings personalSettings = new PersonalSettings(); 

    public HandBindings MainBindings => mainHand == Hand.Right ? rightHandMainBindings : leftHandMainBindings; // 현재 주손 입력
    public HandBindings SubBindings => mainHand == Hand.Right ? leftHandMainBindings : rightHandMainBindings;   // 현재 보조손 입력

    static string GetJsonPath() // Assets 폴더 하위(스크립트 위치)로 저장 경로 설정
    {
        string folder = Path.Combine(Application.dataPath, JsonFolderRelativePath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        return Path.Combine(folder, JsonFileName);
    }

    public void LoadOrInitializePersonalSettings() // 저장본을 불러오거나 없으면 현재 값으로 초기화
    {
        if (!TryLoadPersonalSettings())
        {
            personalSettings.CopyFrom(this);
            SavePersonalSettings();
        }

        personalSettings.ApplyTo(this);
    }

    public void SaveCurrentSettingsAsPersonal() // 현재 ScriptableObject 상태를 저장본으로 기록
    {
        personalSettings.CopyFrom(this);
        SavePersonalSettings();
    }

    bool TryLoadPersonalSettings() // JSON 파일을 읽어 덮어쓰기
    {
        string path = GetJsonPath();
        if (!File.Exists(path))
            return false;

        JsonUtility.FromJsonOverwrite(File.ReadAllText(path), personalSettings);
        return true;
    }

    void SavePersonalSettings() // JSON으로 저장
    {
        string json = JsonUtility.ToJson(personalSettings);
        File.WriteAllText(GetJsonPath(), json);
    }


    
}
