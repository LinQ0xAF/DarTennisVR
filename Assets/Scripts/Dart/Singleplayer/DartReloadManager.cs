using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 싱글플레이용 다트 리로드 매니저
/// </summary>
public class DartReloadManager : DartReloadManagerBase
{
    [Header("Pool Reference")]
    [SerializeField] private DartPoolManager _dartPoolManager;

    protected override void Awake()
    {
        base.Awake();
        if (_dartPoolManager == null)
        {
            _dartPoolManager = FindFirstObjectByType<DartPoolManager>();
        }
    }

    protected override void AttemptReload()
    {
        if (_dartPoolManager == null)
        {
            Debug.LogError("DartPoolManager reference is missing!");
            return;
        }

        foreach (var socket in DartSockets)
        {
            if (socket == null || socket.hasSelection) continue;

            // 풀에서 다트 꺼내기
            GameObject newDart = _dartPoolManager.GetDart(
                socket.transform.position,
                socket.transform.rotation
            );

            // 소켓에 장착
            var interactable = newDart.GetComponent<IXRSelectInteractable>();
            if (interactable != null)
            {
                socket.StartManualInteraction(interactable);
            }
        }
    }
}
