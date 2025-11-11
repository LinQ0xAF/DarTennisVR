using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;

// 개인 진행도/설정 저장 모델
// JSON 파일(persistentDataPath)에 저장/로드합니다. 

[System.Serializable]
public class GamePersonalData
{
    // JSON 저장 파일명
    private const string JsonFileName = "user-settings.json";
    private const string path = "/Users/deepfine.app/Documents/Corporate_Training_Project/DarTennisVR/Assets/Scripts/JeongGyo/Managers";

    // 사용자 설정 (GameDefaultSetting에서 가져와 저장/로드)
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public bool muteAll = true;

    public float uiScale = 1.0f;
    public float uiDistance = 2.0f;
    public float uiFadeDuration = 0.5f;

    public int defaultBalloonCount = 3;
    public int roundSetting = 3;
    public int mainHand = 0; // Hand.Right=0, Hand.Left=1 가정
    public bool smoothTurnEnabled = true;


    // 기본 설정 에셋으로부터 사용자 데이터 초기화
    public void InitFromDefaults(GameDefaultSetting defaults)
    {
        // 오디오
        masterVolume = defaults.masterVolume;
        musicVolume = defaults.musicVolume;
        sfxVolume = defaults.sfxVolume;
        muteAll = defaults.muteAll;

        // UI/시스템
        uiScale = defaults.uiScale;
        uiDistance = defaults.uiDistance;
        uiFadeDuration = defaults.uiFadeDuration;

        // 게임 옵션
        defaultBalloonCount = defaults.defaultBalloonCount;
        roundSetting = defaults.roundSetting;

        // 플레이어
        mainHand = (int)defaults.mainHand;
        smoothTurnEnabled = defaults.smoothTurnEnabled;

    }

    // 런타임 설정 적용(원본 에셋 또는 복제본에 반영)
    public void ApplyTo(GameDefaultSetting target)
    {
        target.masterVolume = masterVolume;
        target.musicVolume = musicVolume;
        target.sfxVolume = sfxVolume;
        target.muteAll = muteAll;
     
        target.uiScale = uiScale;
        target.uiDistance = uiDistance;
        target.uiFadeDuration = uiFadeDuration;

        target.defaultBalloonCount = defaultBalloonCount;
        target.roundSetting = roundSetting;

        target.mainHand = (Hand)mainHand;
        target.smoothTurnEnabled = smoothTurnEnabled;
    }

    // ---------------------- JSON 저장/로드 ----------------------
    private static string GetJsonPath()
    {
        return Path.Combine(path, JsonFileName);
    }

    // JSON으로 영구 저장
    public void SaveToJson()
    {   Debug.Log("Saving Personal Data to " + GetJsonPath());
        var json = JsonUtility.ToJson(this);
        File.WriteAllText(GetJsonPath(), json);
    }

    // JSON에서 로드 (존재하지 않으면 false)
    public bool TryLoadFromJson()
    {   Debug.Log("Trying to Load Personal Data from " + GetJsonPath());
        var path = GetJsonPath();
        if (!File.Exists(path)) return false;
        var json = File.ReadAllText(path);
        JsonUtility.FromJsonOverwrite(json, this);
        Debug.Log("Loaded JSON: " + json);
        return true;
    }

    // JSON이 없으면 기본값을 복사한 뒤 저장하고, 있으면 로드
    public GamePersonalData LoadFromJsonOrDefaults(GameDefaultSetting defaults)
    {   Debug.Log("Loading Personal Data from " + GetJsonPath());
        if (!TryLoadFromJson())
        {
            Debug.Log("No existing personal data found. Initializing from defaults.");
            InitFromDefaults(defaults);
            SaveToJson();
        }
        Debug.Log("Personal Data Loaded: " + JsonUtility.ToJson(this));
        return this;
    }
}
