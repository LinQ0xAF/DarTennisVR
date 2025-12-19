using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Dart Reload Manager for SinglePlayer match
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

            // Get dart from pool
            GameObject newDart = _dartPoolManager.GetDart(
                socket.transform.position,
                socket.transform.rotation
            );

            // Attach to socket
            var interactable = newDart.GetComponent<IXRSelectInteractable>();
            if (interactable != null)
            {
                socket.StartManualInteraction(interactable);
            }
        }
    }
}
