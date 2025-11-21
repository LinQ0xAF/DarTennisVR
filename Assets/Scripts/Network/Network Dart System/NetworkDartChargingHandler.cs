using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class NetworkDartChargingHandler : MonoBehaviour
{
    private XRInteractionManager _InteractionManager;

    [Header("Input Actions(this hand)")]
    [SerializeField] public InputActionReference TriggerButtonAction;   // e.g. RightHand/Activate (Trigger)
    [SerializeField] public InputActionReference SecondButtonAction;   // e.g. RightHand/SecondaryButton (B)

    [Header("XRI Components")]
    [SerializeField] private NearFarInteractor _ThisHandNearFarInteractor; // NearFarInteractor for This Hand

    private NetworkDart _HeldNetworkDart;
    // 내부 상태
    private bool isTriggerPressed = false;
    private bool isSecondPressed = false;
    private bool isCharging = false;

    void Awake()
    {
        _InteractionManager = FindFirstObjectByType<XRInteractionManager>();
    }

    private void OnEnable()
    {
        SubscribeInputs();
        _ThisHandNearFarInteractor.selectEntered.AddListener(OnDartGrab);
        _ThisHandNearFarInteractor.selectExited.AddListener(OnDartRelease);
    }

    private void OnDisable()
    {
        UnsubscribeInputs();
        _ThisHandNearFarInteractor.selectEntered.RemoveListener(OnDartGrab);
        _ThisHandNearFarInteractor.selectExited.RemoveListener(OnDartRelease);

        // 상태 초기화
        isTriggerPressed = false;
        isSecondPressed = false;
        isCharging = false;
        _HeldNetworkDart = null;
    }

    // -- Grab events --
    private void OnDartGrab(SelectEnterEventArgs args)
    {
// 잡은 물체가 NetworkDart인지 확인
        if (args.interactableObject.transform.TryGetComponent<NetworkDart>(out var dart))
        {
            _HeldNetworkDart = dart;
            TryStartCharging();
        }
    }

    private void OnDartRelease(SelectExitEventArgs args)
    {
        _HeldNetworkDart = null;
        isCharging = false;
    }

    // -- Input Subscriptions --
    private void SubscribeInputs()
    {
        if (TriggerButtonAction?.action != null)
        {
            TriggerButtonAction.action.performed += OnTriggerPerformed;
            TriggerButtonAction.action.canceled += OnTriggerCanceled;
        }

        if (SecondButtonAction?.action != null)
        {
            SecondButtonAction.action.performed += OnSecondPerformed;
            SecondButtonAction.action.canceled += OnSecondCanceled;
        }
    }

    private void UnsubscribeInputs()
    {
        if (TriggerButtonAction?.action != null)
        {
            TriggerButtonAction.action.performed -= OnTriggerPerformed;
            TriggerButtonAction.action.canceled -= OnTriggerCanceled;
        }

        if (SecondButtonAction?.action != null)
        {
            SecondButtonAction.action.performed -= OnSecondPerformed;
            SecondButtonAction.action.canceled -= OnSecondCanceled;
        }
    }

    // -- Input Event Handlers --
    private void OnTriggerPerformed(InputAction.CallbackContext _)
    {
        isTriggerPressed = true;
        TryStartCharging();
    }
    private void OnSecondPerformed(InputAction.CallbackContext _)
    {
        isSecondPressed = true;
        TryStartCharging();
    }

    private void OnTriggerCanceled(InputAction.CallbackContext _)
    {
        isTriggerPressed = false;
        TryThrowOnAnyRelease();
    }
    private void OnSecondCanceled(InputAction.CallbackContext _)
    {
        isSecondPressed = false;
        TryThrowOnAnyRelease();
    }
    
    // -- Charging and Throwing Logic --
    /// <summary>
    /// 트리거와 B 버튼이 동시에 눌린 경우에만 차징 상태를 시작
    /// </summary>
    private void TryStartCharging()
    {
        if (_HeldNetworkDart == null)
            return;

        if (!isCharging && isTriggerPressed && isSecondPressed)
        {
            isCharging = true;
            // 🔸 여기서 “차징 이펙트 시작” 같은 피드백 삽입 가능
            // ex) StartChargeVFX();
        }
    }

    /// <summary>
    /// 어느 하나의 입력이라도 해제되면 차징을 종료하고 강제로 던지기를 수행합니다.
    /// </summary>
    private void TryThrowOnAnyRelease()
    {
        if (isCharging && (!isTriggerPressed || !isSecondPressed))
        {
            isCharging = false;

            if (_HeldNetworkDart != null)
            {
                Vector3 vel = _ThisHandNearFarInteractor.transform.forward * 10f; // 예시 속도
                Vector3 angVel = Vector3.zero; // 예시 각속도

                _HeldNetworkDart.ThrowDart_ServerRpc(vel, angVel);
                // 🔹 Grip을 놓지 않아도 Trigger/B 해제 시 던지기 발생
                _InteractionManager?.SelectExit(_ThisHandNearFarInteractor, _HeldNetworkDart.GetComponent<IXRSelectInteractable>());
            }

            // 🔸 차징 종료 이펙트/사운드 종료 가능
            // ex) StopChargeVFX();
        }
    }
}
