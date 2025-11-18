using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

[RequireComponent(typeof(ControllerInputActionManager))]
public class SmoothRotationController : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField]
    private GamePersonalDataManager _GameSettings;

    private ControllerInputActionManager _RightControllerManager;

    private void Awake()
    {
        // Search ControllerInputActionManager component on the same GameObject
        _RightControllerManager = GetComponent<ControllerInputActionManager>();

        if (_GameSettings == null)
        {
            Debug.LogError("ControllerSettingsBinder: GamePersonalDataManager 참조가 인스펙터에 할당되지 않았습니다!", this);
            return;
        }

        // Subscribe to setting change event
        _GameSettings.OnSmoothTurnEnabledChanged += OnSmoothTurnSettingChanged;
    }

    private void Start()
    {
        if (_GameSettings != null)
        {
            OnSmoothTurnSettingChanged(_GameSettings.smoothTurnEnabled);
        }
    }

    private void OnSmoothTurnSettingChanged(bool isEnabled)
    {
        if (_RightControllerManager != null)
        {
            _RightControllerManager.smoothTurnEnabled = isEnabled;
        }
    }

    private void OnDestroy()
    {
        if (_GameSettings != null)
        {
            _GameSettings.OnSmoothTurnEnabledChanged -= OnSmoothTurnSettingChanged;
        }
    }
}