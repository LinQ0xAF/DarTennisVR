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

    [Header("Handedness Setting UI")]
    public UnityEngine.UI.Toggle HandRoleToggle;

    private bool _IsRightHanded = true;

    public bool IsRightHanded
    {
        get => _IsRightHanded;
        set
        {
            if (_IsRightHanded != value)
            {
                _IsRightHanded = value;
                UpdateHandRoles();
            }
        }
    }

    private void Start()
    {
        if (HandRoleToggle != null)
        {
            HandRoleToggle.onValueChanged.AddListener(value =>
            {
                IsRightHanded = value;
            });
        }

        UpdateHandRoles();
        if (HandRoleToggle != null)
        {
            HandRoleToggle.isOn = _IsRightHanded;
        }
    }

    private void UpdateHandRoles()
    {
        if (_IsRightHanded)
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
}
