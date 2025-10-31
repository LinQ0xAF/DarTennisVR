using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


[RequireComponent(typeof(XRGrabInteractable))]
public class DartChargeThrow_EventsOnly : MonoBehaviour
{
    [Header("Input Actions")]
    // ▶ 샘플 InputActionAsset에서 RightHand/Trigger, RightHand/B 버튼을 연결하세요.
    [SerializeField] public InputActionReference TriggerButtonAction;   // e.g. RightHand/Select (Trigger)
    [SerializeField] public InputActionReference SecondButtonAction;   // e.g. RightHand/SecondaryButton (B)

    [Header("Grab Interactable")]
    [SerializeField] private XRGrabInteractable grab; // grabinteractable
    
    private IXRSelectInteractor currentInteractor; //최근에 그랩을 한 주최

    private bool isGrabbed = false;
    private bool isTrigger = false;
    private bool isSecond = false;
    private bool charging = false;


    void Awake()
    {
        // if (TriggerButtonAction == null)
        //     TriggerButtonAction = gameObject.GetComponentInParent<InputActionReference>();
            
        // if (SecondButtonAction == null) ;
        //     SecondButtonAction = gameObject.gameObject.GetComponentInParent<InputActionReference>();

    }

    /// <summary>
    /// 오브젝트가 활성화되면 (필요 시) XR 컴포넌트를 찾아 잡기 이벤트를 구독합니다.
    /// </summary>
    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    /// <summary>
    /// 오브젝트가 비활성화될 때 잡기 이벤트와 입력 바인딩을 정리합니다.
    /// </summary>
    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
        UnsubscribeInputs();
    }

    /// <summary>
    /// 상호작용자가 다트를 잡는 순간 상태를 초기화하고 입력 콜백을 연결합니다.
    /// </summary>
    private void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject; //액션을 취한 인터렉터오브젝트 자체가 들어오게 됨(ex: Near-far Interactor)
        
        isGrabbed = true;
        isTrigger = false;
        isSecond = false;
        charging = false;

        SubscribeInputs();
    }

    /// <summary>
    /// 다트가 놓였을 때 상태를 초기화하고 입력 콜백을 해제합니다.
    /// </summary>
    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        currentInteractor = null;

        UnsubscribeInputs();
    }

    /// <summary>
    /// 다트가 잡힌 동안에만 차징 상태를 추적할 수 있도록 트리거와 B 버튼 입력 액션을 구독합니다.
    /// </summary>
    private void SubscribeInputs()
    {
        if (TriggerButtonAction?.action != null)
        {
            TriggerButtonAction.action.performed += OnTriggerPerformed;
            TriggerButtonAction.action.canceled += OnTriggerCanceled;
            TriggerButtonAction.action.Enable();
        }

        if (SecondButtonAction?.action != null)
        {
            SecondButtonAction.action.performed += OnBPerformed;
            SecondButtonAction.action.canceled += OnBCanceled;
            SecondButtonAction.action.Enable();
        }
    }

    /// <summary>
    /// 다트가 더 이상 잡혀 있지 않을 때 입력 액션 이벤트 핸들러를 제거합니다.
    /// </summary>
    private void UnsubscribeInputs()
    {
        if (TriggerButtonAction?.action != null)
        {
            TriggerButtonAction.action.performed -= OnTriggerPerformed;
            TriggerButtonAction.action.canceled -= OnTriggerCanceled;
        }

        if (SecondButtonAction?.action != null)
        {
            SecondButtonAction.action.performed -= OnBPerformed;
            SecondButtonAction.action.canceled -= OnBCanceled;
        }
    }

    // ---------------------- Trigger ----------------------

    /// <summary>
    /// 트리거가 눌린 상태로 표시하고 두 입력이 모두 활성화되었는지 확인하여 차징을 시작합니다.
    /// </summary>
    private void OnTriggerPerformed(InputAction.CallbackContext _)
    {
        isTrigger = true;
        TryStartCharging();
    }

    /// <summary>
    /// 트리거 해제를 감지하고 차징 상태였다면 던지기를 시도합니다.
    /// </summary>
    private void OnTriggerCanceled(InputAction.CallbackContext _)
    {
        isTrigger = false;
        TryThrowOnAnyRelease();
    }

    // ---------------------- B Button ----------------------

    /// <summary>
    /// B 버튼이 눌린 상태로 표시하고 두 입력이 모두 활성화되었는지 확인하여 차징을 시작합니다.
    /// </summary>
    private void OnBPerformed(InputAction.CallbackContext _)
    {
        isSecond = true;
        TryStartCharging();
    }

    /// <summary>
    /// B 버튼 해제를 감지하고 차징 상태였다면 던지기를 시도합니다.
    /// </summary>
    private void OnBCanceled(InputAction.CallbackContext _)
    {
        isSecond = false;
        TryThrowOnAnyRelease();
    }

    // ---------------------- Core Logic ----------------------

    /// <summary>
    /// 트리거와 B 버튼이 동시에 눌린 경우에만 차징 상태를 시작합니다.
    /// </summary>
    private void TryStartCharging()
    {
        if (!charging && isTrigger && isSecond)
        {
            charging = true;
            // 🔸 여기서 “차징 이펙트 시작” 같은 피드백을 넣을 수 있음.
            // ex) StartChargeVFX();
        }
    }

    /// <summary>
    /// 어느 하나의 입력이라도 해제되면 차징을 종료하고 강제로 던지기를 수행합니다.
    /// </summary>
    private void TryThrowOnAnyRelease()
    {
        if (charging && (!isTrigger || !isSecond))
        {
            charging = false;

            if (grab != null && currentInteractor != null)
            {
                // 🔹 Grip을 놓지 않아도 Trigger/B 해제 시 던지기 발생
                grab.interactionManager?.SelectExit(currentInteractor, grab);
            }

            // 🔸 차징 종료 이펙트/사운드 종료 가능
            // ex) StopChargeVFX();
        }
    }
}
