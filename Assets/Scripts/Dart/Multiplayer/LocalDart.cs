using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 멀티플레이용 로컬 다트 (네트워크 동기화 전용)
/// </summary>
public class LocalDart : ThrowingDartBase
{
    private LocalDartPool _PoolManager;
    private NetworkDartThrowHandler _CurrentDartThrowHandler;

    public void SetPoolManager(LocalDartPool manager)
    {
        _PoolManager = manager;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _CurrentDartThrowHandler = null;
    }

    /// <summary>
    /// 주손 핸들러 등록 (주손이 잡았을 때 DartThrowHandler가 호출)
    /// </summary>
    public void SetMainHandHandler(NetworkDartThrowHandler handler)
    {
        _CurrentDartThrowHandler = handler;
    }

    protected override void OnReleased(SelectExitEventArgs args)
    {
        // 주손에서 놓여진 경우에만 네트워크 처리
        if (_CurrentDartThrowHandler != null)
        {
            var handlerTemp = _CurrentDartThrowHandler;
            _CurrentDartThrowHandler = null;
            StartCoroutine(CaptureVelocityNextFrame((vel, angVel) =>
            {
                handlerTemp.HandleDartRelease(this, vel, angVel);
            }));
        }

        // 부모의 최대 수명 타이머 시작
        base.OnReleased(args);
    }

    protected override void OnHitEnvironment(Collision collision)
    {
        // 물리 정지
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 즉시 반납 (LocalDart는 벽에 박히면 바로 사라짐)
        ForceReturnToPool();
    }

    /// <summary>
    /// 외부(NetworkDart 등)에서 강제로 반납시키기 위한 함수
    /// </summary>
    public void ForceReturnToPool()
    {
        StopAllReturnCoroutines();
        ReturnToPool();
    }

    public override void ReturnToPool()
    {
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