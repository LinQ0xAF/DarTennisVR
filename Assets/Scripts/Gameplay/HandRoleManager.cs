using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.UI;
using UnityEngine.XR;

public class HandRoleManager : MonoBehaviour
{
    [Header("NearFar Interactors for Each Hand")]
    [SerializeField] private NearFarInteractor _LeftHandNearFarInteractor;
    [SerializeField] private NearFarInteractor _RightHandNearFarInteractor;

    [Header("Off-hand Object Group")]
    [SerializeField] private GameObject _LeftOffHandObjectGroup;
    [SerializeField] private GameObject _RightOffHandObjectGroup;

    [Header("MainHand Dart Throw Handlers")]
    [SerializeField] private DartThrowHandlerBase _LeftDartThrowHandler;
    [SerializeField] private DartThrowHandlerBase _RightDartThrowHandler;

    [Header("Interaction Layer Masks")]
    [SerializeField] private InteractionLayerMask _MainHandInteractionLayerMask;
    [SerializeField] private InteractionLayerMask _OffHandInteractionLayerMask;

    [Header("References")]
    [SerializeField] private GamePersonalDataManager _GameSettings;
    [SerializeField] private SetManager _SetManager;
    [SerializeField] private SingleRoundManager _RoundManager;
#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool _testHandsActive = true;
    private bool _lastTestHandsActive = true;
#endif

    private void Awake()
    {
        if (_GameSettings != null)
        {
            _GameSettings.OnMainHandChanged += UpdateHandRoles;
        }
    }

    private void Start()
    {
        UpdateHandRoles(_GameSettings.mainHand);

        if (_SetManager == null && _RoundManager != null)
        {
            _SetManager = FindFirstObjectByType<SetManager>();
            _RoundManager = FindFirstObjectByType<SingleRoundManager>();
        }

        if (_SetManager != null)
        {
            _SetManager.OnSetPreStart += HandleSetPreStart;
            _SetManager.OnSetStart += HandleSetStart;
        }
        if (_RoundManager != null)
        {
            _RoundManager.OnRoundPreStart += HandleSetPreStart;
            _RoundManager.OnRoundStart += HandleSetStart;
        }
    }

    private void HandleSetPreStart() => SetHandsActive(false);
    private void HandleSetStart() => SetHandsActive(true);

    /// <summary>
    /// 손의 상호작용 및 다트 던지기 기능을 활성화/비활성화한다.
    /// </summary>
    private void SetHandsActive(bool active)
    {
#if UNITY_EDITOR
        _testHandsActive = active;
        _lastTestHandsActive = active;
#endif

        if (active)
        {
            // 설정에 따라 올바른 손 역할 복구
            UpdateHandRoles(_GameSettings.mainHand);
        }
        else
        {
            // 모든 상호작용 비활성화
            _LeftHandNearFarInteractor.interactionLayers = _OffHandInteractionLayerMask;
            _RightHandNearFarInteractor.interactionLayers = _OffHandInteractionLayerMask;

            _LeftOffHandObjectGroup.SetActive(false);
            _RightOffHandObjectGroup.SetActive(false);

            _LeftDartThrowHandler.enabled = false;
            _RightDartThrowHandler.enabled = false;
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        // 에디터 테스트용: _testHandsActive 값이 변경되면 SetHandsActive 호출
        if (_lastTestHandsActive != _testHandsActive)
        {
            SetHandsActive(_testHandsActive);
            _lastTestHandsActive = _testHandsActive;
        }
    }
#endif

    private void UpdateHandRoles(Hand mainHand)
    {
        bool isRightHanded = (mainHand == Hand.Right);
        if (isRightHanded)
        {
            // 오른손잡이 설정
            _RightHandNearFarInteractor.interactionLayers = _MainHandInteractionLayerMask;
            _LeftHandNearFarInteractor.interactionLayers = _OffHandInteractionLayerMask;

            _RightOffHandObjectGroup.SetActive(false);
            _LeftOffHandObjectGroup.SetActive(true);

            _RightDartThrowHandler.enabled = true;
            _LeftDartThrowHandler.enabled = false;
        }
        else
        {
            // 왼손잡이 설정
            _RightHandNearFarInteractor.interactionLayers = _OffHandInteractionLayerMask;
            _LeftHandNearFarInteractor.interactionLayers = _MainHandInteractionLayerMask;

            _RightOffHandObjectGroup.SetActive(true);
            _LeftOffHandObjectGroup.SetActive(false);

            _RightDartThrowHandler.enabled = false;
            _LeftDartThrowHandler.enabled = true;
        }
    }

    private void OnDestroy()
    {
        if (_GameSettings != null)
        {
            _GameSettings.OnMainHandChanged -= UpdateHandRoles;
        }

        if (_SetManager != null)
        {
            _SetManager.OnSetPreStart -= HandleSetPreStart;
            _SetManager.OnSetStart -= HandleSetStart;
        }
    }
}
