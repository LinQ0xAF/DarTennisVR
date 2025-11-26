using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode; // Bridge 찾기 위해 필요

public class NetworkDartThrowHandler : MonoBehaviour
{
    [Header("Inputs")]
    public InputActionReference TriggerAction;
    public InputActionReference SecondAction;

    [Header("References")]
    public NearFarInteractor HandInteractor; // 주손 인터랙터

    // 외부(LocalDart)에서 읽을 수 있는 차징 상태
    public bool IsCharging { get; private set; }

    private bool _isTrigger, _isSecond;
    private LocalDart _heldDart; // 현재 잡고 있는 다트
    private PlayerNetworkBridge _bridge;

    void Start()
    {
        InitializeBridge();
    }

    private void InitializeBridge()
    {
        if (_bridge == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            _bridge = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkBridge>();
        }
    }

    void OnEnable()
    {
        // 입력 이벤트 구독
        if (TriggerAction != null)
        {
            TriggerAction.action.performed += _ => CheckChargeState(true, _isSecond);
            TriggerAction.action.canceled += _ => CheckChargeState(false, _isSecond);
        }
        if (SecondAction != null)
        {
            SecondAction.action.performed += _ => CheckChargeState(_isTrigger, true);
            SecondAction.action.canceled += _ => CheckChargeState(_isTrigger, false);
        }

        // XRI 이벤트 구독
        if (HandInteractor != null)
        {
            HandInteractor.selectEntered.AddListener(OnGrab);
            HandInteractor.selectExited.AddListener(OnRelease);
        }
    }

    void OnDisable()
    {
        if (TriggerAction != null)
        {
            TriggerAction.action.performed -= _ => CheckChargeState(true, _isSecond);
            TriggerAction.action.canceled -= _ => CheckChargeState(false, _isSecond);
        }
        if (SecondAction != null)
        {
            SecondAction.action.performed -= _ => CheckChargeState(_isTrigger, true);
            SecondAction.action.canceled -= _ => CheckChargeState(_isTrigger, false);
        }

        if (HandInteractor != null)
        {
            HandInteractor.selectEntered.RemoveListener(OnGrab);
            HandInteractor.selectExited.RemoveListener(OnRelease);
        }
    }

    // --- 차징 상태 관리 ---
    private void CheckChargeState(bool trig, bool sec)
    {
        bool wasCharging = IsCharging;
        _isTrigger = trig;
        _isSecond = sec;

        // 두 버튼이 다 눌렸고 + 다트를 잡고 있다면 -> 차징 시작
        if (_isTrigger && _isSecond && _heldDart != null)
        {
            IsCharging = true;
            // TODO: 차징 이펙트/사운드 재생
            if (!wasCharging) Debug.Log("Charging Started");
        }
        else
        {
            // 차징 중이었는데 버튼 하나라도 떼면 -> 강제 투척
            if (wasCharging)
            {
                ForceThrow();
            }
            IsCharging = false;
        }
    }

    private void ForceThrow()
    {
        if (_heldDart != null && HandInteractor != null)
        {
            // XRI Interaction Manager를 통해 강제로 놓게 만듦
            var manager = HandInteractor.interactionManager;
            if (manager != null)
            {
                manager.SelectExit(HandInteractor, _heldDart.GetComponent<IXRSelectInteractable>());
            }
        }
    }

    // --- 잡기 (Grab) ---
    private void OnGrab(SelectEnterEventArgs args)
    {
        // 잡은 물체가 LocalDart인지 확인
        if (args.interactableObject.transform.TryGetComponent<LocalDart>(out var dart))
        {
            _heldDart = dart;
            
            // 다트에게 "나(주손)에게 잡혔음"을 알림
            dart.SetMainHandHandler(this);

            // 네트워크 상태 업데이트: 부손 개수 -1, 손에 듦 = true
            if (_bridge != null) 
            {
                _bridge.UpdateOffHandDarts(-1);
                _bridge.UpdateHoldingState(true);
            }
        }
    }

    // --- 놓기 (Release) ---
    private void OnRelease(SelectExitEventArgs args)
    {
        // 놓은 물체가 잡고 있던 그 다트인지 확인
        if (_heldDart != null && args.interactableObject.transform == _heldDart.transform)
        {
            // 다트가 손을 떠났으므로, 이후 처리는 LocalDart.OnThrown에서 수행됨
            // (HandleDartRelease가 호출될 것임)
            
            _heldDart = null;
            IsCharging = false;
            
            // 버튼 상태 초기화 (안전장치)
            _isTrigger = false;
            _isSecond = false;
        }
    }

    // --- LocalDart가 호출하는 결과 처리 ---
    public void HandleDartRelease(LocalDart dart, Vector3 vel, Vector3 angVel)
    {
        if(_bridge == null) InitializeBridge();

        // 네트워크 상태 업데이트: 손에 듦 = false
        if (_bridge != null) 
        {
            _bridge.UpdateHoldingState(false);
            _bridge.RequestSpawnProjectile(dart.transform.position, dart.transform.rotation, vel, angVel);

        }
    }
}