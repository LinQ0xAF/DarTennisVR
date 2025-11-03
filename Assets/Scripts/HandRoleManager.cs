using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandRoleManager : MonoBehaviour
{
    [Header("NearFar Interactors for Each Hand")]
    [SerializeField] private NearFarInteractor _LeftHandNearFarInteractor;
    [SerializeField] private NearFarInteractor _RightHandNearFarInteractor;

    [Header("Off-hand Object Group")]
    [SerializeField] private GameObject _LeftOffHandObjectGroup;
    [SerializeField] private GameObject _RightOffHandObjectGroup;

    [Header("Interaction Layer Masks")]
    [SerializeField] private LayerMask _MainHandInteractionLayerMask;
    [SerializeField] private LayerMask _OffHandInteractionLayerMask;

    
}
