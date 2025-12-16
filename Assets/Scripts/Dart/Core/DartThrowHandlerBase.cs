using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Dart Charging(+ Throwing) Handler의 공통 기능을 제공하는 추상 클래스
/// </summary>
public abstract class DartThrowHandlerBase : MonoBehaviour
{
    [Header("Input Actions (this hand)")]
    [SerializeField] public InputActionReference TriggerButtonAction;
    [SerializeField] public InputActionReference SecondButtonAction;

    [Header("XRI Components")]
    [SerializeField] protected NearFarInteractor ThisHandNearFarInteractor;

    // 외부에서 읽을 수 있는 차징 상태
    public bool IsCharging { get; protected set; }

    // 내부 상태
    protected bool isTriggerPressed = false;
    protected bool isSecondPressed = false;
    protected IXRSelectInteractable currentGrabbedInteractable;

    // 이벤트 해제를 위해 델리게이트를 필드로 캐싱
    private System.Action<InputAction.CallbackContext> _onTriggerPerformed;
    private System.Action<InputAction.CallbackContext> _onTriggerCanceled;
    private System.Action<InputAction.CallbackContext> _onSecondPerformed;
    private System.Action<InputAction.CallbackContext> _onSecondCanceled;

    protected virtual void Awake()
    {
        // 람다를 필드에 저장해 나중에 해제 가능하게 함
        _onTriggerPerformed = OnTriggerPerformed;
        _onTriggerCanceled = OnTriggerCanceled;
        _onSecondPerformed = OnSecondPerformed;
        _onSecondCanceled = OnSecondCanceled;
    }

    protected virtual void OnEnable()
    {
        SubscribeInputs();
        if (ThisHandNearFarInteractor != null)
        {
            ThisHandNearFarInteractor.selectEntered.AddListener(OnDartGrab);
            ThisHandNearFarInteractor.selectExited.AddListener(OnDartRelease);
        }
    }

    protected virtual void OnDisable()
    {
        UnsubscribeInputs();
        if (ThisHandNearFarInteractor != null)
        {
            ThisHandNearFarInteractor.selectEntered.RemoveListener(OnDartGrab);
            ThisHandNearFarInteractor.selectExited.RemoveListener(OnDartRelease);
        }

        // 상태 초기화
        ResetState();
    }

    protected void ResetState()
    {
        isTriggerPressed = false;
        isSecondPressed = false;
        IsCharging = false;
        currentGrabbedInteractable = null;
    }

    #region Input Subscriptions
    private void SubscribeInputs()
    {
        if (TriggerButtonAction?.action != null)
        {
            TriggerButtonAction.action.performed += _onTriggerPerformed;
            TriggerButtonAction.action.canceled += _onTriggerCanceled;
        }

        if (SecondButtonAction?.action != null)
        {
            SecondButtonAction.action.performed += _onSecondPerformed;
            SecondButtonAction.action.canceled += _onSecondCanceled;
        }
    }

    private void UnsubscribeInputs()
    {
        if (TriggerButtonAction?.action != null)
        {
            TriggerButtonAction.action.performed -= _onTriggerPerformed;
            TriggerButtonAction.action.canceled -= _onTriggerCanceled;
        }

        if (SecondButtonAction?.action != null)
        {
            SecondButtonAction.action.performed -= _onSecondPerformed;
            SecondButtonAction.action.canceled -= _onSecondCanceled;
        }
    }
    #endregion

    #region Input Event Handlers
    private void OnTriggerPerformed(InputAction.CallbackContext _)
    {
        isTriggerPressed = true;
        TryStartCharging();
    }

    private void OnTriggerCanceled(InputAction.CallbackContext _)
    {
        isTriggerPressed = false;
        TryThrowOnAnyRelease();
    }

    private void OnSecondPerformed(InputAction.CallbackContext _)
    {
        isSecondPressed = true;
        TryStartCharging();
    }

    private void OnSecondCanceled(InputAction.CallbackContext _)
    {
        isSecondPressed = false;
        TryThrowOnAnyRelease();
    }
    #endregion

    #region Grab Events
    private void OnDartGrab(SelectEnterEventArgs args)
    {
        // 자식 클래스에서 유효한 다트인지 확인
        if (!IsValidDart(args.interactableObject))
            return;

        currentGrabbedInteractable = args.interactableObject;

        // [공통] ThrowingDartBase라면 핸들러 등록
        if (args.interactableObject.transform.TryGetComponent<ThrowingDartBase>(out var dartBase))
        {
            dartBase.SetThrowHandler(this);
        }

        OnValidDartGrabbed(args);
        TryStartCharging();
    }

    private void OnDartRelease(SelectExitEventArgs args)
    {
        if (args.interactableObject != currentGrabbedInteractable)
            return;

        OnValidDartReleased(args);
        currentGrabbedInteractable = null;
        IsCharging = false;
    }
    #endregion

    #region Charging and Throwing Logic
    /// <summary>
    /// 트리거와 Second 버튼이 동시에 눌린 경우에만 차징 상태를 시작
    /// </summary>
    private void TryStartCharging()
    {
        if (currentGrabbedInteractable == null)
            return;

        if (!IsCharging && isTriggerPressed && isSecondPressed)
        {
            IsCharging = true;
            OnChargingStarted();
        }
    }

    /// <summary>
    /// 어느 하나의 입력이라도 해제되면 차징을 종료하고 강제로 던지기를 수행합니다.
    /// </summary>
    private void TryThrowOnAnyRelease()
    {
        if (IsCharging && (!isTriggerPressed || !isSecondPressed))
        {
            IsCharging = false;

            if (currentGrabbedInteractable != null)
            {
                ForceThrow();
            }

            OnChargingEnded();
        }
    }

    /// <summary>
    /// Grip을 놓지 않아도 Trigger/B 해제 시 던지기 발생
    /// </summary>
    protected virtual void ForceThrow()
    {
        var manager = ThisHandNearFarInteractor?.interactionManager;
        if (manager != null && currentGrabbedInteractable != null)
        {
            manager.SelectExit(ThisHandNearFarInteractor, currentGrabbedInteractable);
        }
    }
    #endregion

    #region Abstract / Virtual Methods (자식 클래스에서 구현)
    /// <summary>
    /// 잡은 물체가 이 핸들러에서 처리할 유효한 다트인지 확인
    /// </summary>
    protected abstract bool IsValidDart(IXRSelectInteractable interactable);

    /// <summary>
    /// 유효한 다트를 잡았을 때 호출 (자식 클래스에서 추가 로직 구현)
    /// </summary>
    protected virtual void OnValidDartGrabbed(SelectEnterEventArgs args) { }

    /// <summary>
    /// 다트를 놓았을 때 호출 (자식 클래스에서 추가 로직 구현)
    /// </summary>
    protected virtual void OnValidDartReleased(SelectExitEventArgs args) { }

    /// <summary>
    /// 차징이 시작되었을 때 호출 (이펙트/사운드 등)
    /// </summary>
    protected virtual void OnChargingStarted() { }

    /// <summary>
    /// 차징이 종료되었을 때 호출 (이펙트/사운드 등)
    /// </summary>
    protected virtual void OnChargingEnded() { }
    #endregion
}
