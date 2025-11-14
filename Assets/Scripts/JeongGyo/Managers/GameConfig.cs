using UnityEngine;

public class GameConfig : MonoBehaviour
{
    [Header("Game Configurations")]
    [SerializeField] private GamePersonalDataManager defaultData;

    void Awake()
    {
        if (defaultData == null)
        {
            Debug.LogWarning("GameConfig : GameDefaultSetting reference is missing.");
            return;
        }
        
        defaultData.LoadOrInitializePersonalSettings(); // 개인 설정 불러오기 또는 디볼트 초기화


    }
}
