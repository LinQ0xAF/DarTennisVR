using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
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

    [Header("MainHand Dart Charging Handlers")]
    [SerializeField] private DartChargingHandler _LeftHandDartChargingHandler;
    [SerializeField] private DartChargingHandler _RightHandDartChargingHandler;

    [Header("Interaction Layer Masks")]
    [SerializeField] private InteractionLayerMask _MainHandInteractionLayerMask;
    [SerializeField] private InteractionLayerMask _OffHandInteractionLayerMask;

    [SerializeField]
    private GamePersonalDataManager _GameSettings;

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
    }

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

            _RightHandDartChargingHandler.enabled = true;
            _LeftHandDartChargingHandler.enabled = false;
        }
        else
        {
            // 왼손잡이 설정
            _RightHandNearFarInteractor.interactionLayers = _OffHandInteractionLayerMask;
            _LeftHandNearFarInteractor.interactionLayers = _MainHandInteractionLayerMask;

            _RightOffHandObjectGroup.SetActive(true);
            _LeftOffHandObjectGroup.SetActive(false);

            _RightHandDartChargingHandler.enabled = false;
            _LeftHandDartChargingHandler.enabled = true;
        }
    }

    private void OnDestroy()
    {
        if (_GameSettings != null)
        {
            _GameSettings.OnMainHandChanged -= UpdateHandRoles;
        }
    }
}
