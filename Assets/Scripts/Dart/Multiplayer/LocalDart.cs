using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 멀티플레이용 로컬 다트 (네트워크 동기화 전용)
/// </summary>
public class LocalDart : ThrowingDartBase
{
    private LocalDartPool _PoolManager;

    protected override void Awake()
    {
        base.Awake();
        // AudioSource 초기화는 부모에서 수행
    }

    public void SetPoolManager(LocalDartPool manager)
    {
        _PoolManager = manager;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // _CurrentDartThrowHandler 초기화 불필요 (부모의 currentThrowHandler 사용)
    }

    // SetMainHandHandler 제거됨 (부모의 SetThrowHandler 사용)

    protected override void OnReleased(SelectExitEventArgs args)
    {
        // 주손에서 놓여진 경우에만 네트워크 처리
        // 부모의 currentThrowHandler가 null이 되기 전에 캐스팅해서 사용
        if (currentThrowHandler is NetworkDartThrowHandler netHandler)
        {
            StartCoroutine(CaptureVelocityNextFrame((vel, angVel) =>
            {
                netHandler.HandleDartRelease(this, vel, angVel);
            }));
        }

        // 부모 호출 (여기서 소리 재생 및 currentThrowHandler 초기화 수행됨)
        base.OnReleased(args);
    }

    protected override void OnHitEnvironment(Collision collision)
    {
        // 물리 정지
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        StopFlyingSound();

        // 즉시 반납 (LocalDart는 벽에 박히면 바로 사라짐)
        ForceReturnToPool();
    }

    /// <summary>
    /// 외부(NetworkDart 등)에서 강제로 반납시키기 위한 함수
    /// </summary>
    public void ForceReturnToPool()
    {
        StopFlyingSound();
        StopAllReturnCoroutines();
        ReturnToPool();
    }

    public override void ReturnToPool()
    {
        StopFlyingSound();
        if (_PoolManager != null)
        {
            _PoolManager.ReleaseDart(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator CaptureVelocityNextFrame(System.Action<Vector3, Vector3> callback)
    {
        yield return new WaitForFixedUpdate();

        Vector3 vel = rb.linearVelocity;
        Vector3 angVel = rb.angularVelocity;

        callback?.Invoke(vel, angVel);
    }
}