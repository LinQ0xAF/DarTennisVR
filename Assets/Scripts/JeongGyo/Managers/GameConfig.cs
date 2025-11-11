using UnityEngine;

public class GameConfig : MonoBehaviour
{
    [Header("Game Configurations")]
    [SerializeField] private GameDefaultSetting DefaultData;  
    private GamePersonalData PlayerData { get; set; }

    void Awake()
    {
        PlayerData = new GamePersonalData();

        // 1) 개인 설정 로드(없으면 기본값으로 초기화)  
        PlayerData.LoadFromJsonOrDefaults(DefaultData);

        // 2) 로드된 개인 설정을 기본 설정(SO 복제본 또는 원본)에 적용
        PlayerData.ApplyTo(DefaultData);

    }




    

}
