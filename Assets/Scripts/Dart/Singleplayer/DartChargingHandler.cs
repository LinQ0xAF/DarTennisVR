using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 싱글플레이용 다트 차징 핸들러
/// </summary>
public class DartChargingHandler : DartThrowHandlerBase
{
    protected override bool IsValidDart(IXRSelectInteractable interactable)
    {
        // 싱글플레이: Dart 컴포넌트가 있는지 확인
        return interactable.transform.gameObject.GetComponent<Dart>() != null;
    }

    protected override void OnChargingStarted()
    {
        // 여기서 차징 이펙트 시작 같은 피드백 삽입 가능
        // ex) StartChargeVFX();
    }

    protected override void OnChargingEnded()
    {
        // 차징 종료 이펙트/사운드 종료 가능
        // ex) StopChargeVFX();
    }
}
