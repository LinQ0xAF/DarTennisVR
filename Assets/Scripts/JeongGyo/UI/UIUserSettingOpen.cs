using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.UI;
using TMPro;
using System;

public class UIUserSettingOpen : MonoBehaviour
{

    [Header("Game Configurations")]
    [SerializeField] 
    private GamePersonalDataManager defaultData;
    
    [SerializeField]
    bool UserSettingUIActive = false;
    private ControllerInputActionManager controllerInputActionManager;
    private InputActionReference m_OpenUserSettingUI;
    const float distance = 1.5f;
    const float verticalOffset = 1f;

    [Header("UI Elements")]
    [SerializeField]
    Slider VolumeSlider;
    [SerializeField]
    Toggle SmoothTurnToggle;
    [SerializeField]
    Toggle HandedToggle;
    [SerializeField]
    Button ApplyButton;

    float cachedVolume;
    bool cachedSmoothTurn;
    Hand cachedMainHand;
    bool hasCachedDefaults;

    Camera cam;

    void Awake()
    {        
        controllerInputActionManager = FindFirstObjectByType<ControllerInputActionManager>();
        m_OpenUserSettingUI = controllerInputActionManager.m_OpenUserSettingUI;
        cam = Camera.main;
        if (ApplyButton != null)
        {
            ApplyButton.interactable = false;
        }

        if (defaultData == null)
        {
            Debug.LogWarning("GameConfig : GameDefaultSetting reference is missing.");
            return;
        }
        
        defaultData.TryLoadPersonalSettings(); // 개인 설정 불러오기 또는 디볼트 초기화

    }

    void Start()
    {   ApplyButton.gameObject.SetActive(false);
        InputAction openUserSettingAction = m_OpenUserSettingUI.action;

        if (openUserSettingAction != null)
        {
            openUserSettingAction.performed += OnOpenUserSettingUI;
        }

    }
    void OnOpenUserSettingUI(InputAction.CallbackContext context)
    {
        transform.position = cam.transform.position + cam.transform.forward * distance - Vector3.up * verticalOffset;
        transform.LookAt(cam.transform.position, Vector3.up);
        
        bool shouldEnable = !UserSettingUIActive;
        
        if (shouldEnable) // 열릴 때
        {   
            defaultData.TryLoadPersonalSettings(); // 개인 설정 불러오기 또는 디볼트 초기화
            ApplyDefaultDataToUI(); // UI 에 디폴트 값 적용
        }
    
        foreach (Transform child in transform)
        {   
            child.localPosition = Vector3.zero;
            child.gameObject.SetActive(shouldEnable);
            
            foreach(Transform grandChild in child)
            {
                grandChild.gameObject.SetActive(shouldEnable);
            }
           
        }
        UserSettingUIActive = shouldEnable;

    }

    void ApplyDefaultDataToUI()
    {
        if (defaultData == null)
        {
            Debug.LogWarning("UIUserSettingOpen : GameDefaultSetting reference is missing.");
            return;
        }
            VolumeSlider.SetValueWithoutNotify(defaultData.masterVolume);
            SmoothTurnToggle.SetIsOnWithoutNotify(defaultData.smoothTurnEnabled);
            HandedToggle.SetIsOnWithoutNotify(defaultData.mainHand == (int)Hand.Right);
            
            CacheDefaultValues();

    }

    void CacheDefaultValues()
    {
        cachedVolume = this.VolumeSlider.value;
        cachedSmoothTurn = this.SmoothTurnToggle.isOn;
        cachedMainHand = this.HandedToggle.isOn ? Hand.Right : Hand.Left;
        hasCachedDefaults = true;
        EvaluateApplyButtonState();
    }

    public void OnVolumeSliderChanged(float _)
    {
        EvaluateApplyButtonState();
    }

    public void OnToggleChanged(bool _)
    {
        EvaluateApplyButtonState();
    }

    void EvaluateApplyButtonState()
    {
        if (ApplyButton == null)
        {
            return;
        }

        if (!hasCachedDefaults)
        {
            ApplyButton.interactable = false;
            return;
        }

        bool volumeChanged = VolumeSlider != null && Mathf.Abs(VolumeSlider.value - cachedVolume) > 0.001f;
        bool smoothTurnChanged = SmoothTurnToggle != null && SmoothTurnToggle.isOn != cachedSmoothTurn;
        bool handedChanged = HandedToggle != null && HandedToggle.isOn != (cachedMainHand == Hand.Right);

        ApplyButton.gameObject.SetActive(true);
        ApplyButton.interactable = volumeChanged || smoothTurnChanged || handedChanged;
    }


    public void OnApplyButtonClicked()
    {
        if (defaultData == null)
        {
            Debug.LogWarning("UIUserSettingOpen : GameDefaultSetting reference is missing.");
            return;
        }

        defaultData.masterVolume = VolumeSlider.value;
        defaultData.smoothTurnEnabled = SmoothTurnToggle.isOn;
        defaultData.mainHand = HandedToggle.isOn ? Hand.Right : Hand.Left;

        defaultData.SaveCurrentSettingsAsPersonal(); // 현재 설정을 개인 설정으로 저장

        CacheDefaultValues();
    }
}
