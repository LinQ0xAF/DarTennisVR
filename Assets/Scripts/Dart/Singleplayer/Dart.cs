using UnityEngine;

/// <summary>
/// 싱글플레이용 다트
/// </summary>
public class Dart : ThrowingDartBase
{
    private DartPoolManager _PoolManager;

    public void SetPoolManager(DartPoolManager poolManager)
    {
        _PoolManager = poolManager;
    }

    protected override void OnHitEnvironment(Collision collision)
    {
        // 벽에 박힘 (물리 정지)
        rb.isKinematic = true;

        // 최대 수명 타이머 중지 (정상 충돌했으므로)
        StopMaxLifetimeCoroutine();

        // 2초 후 풀에 반납
        StartReturnCoroutine(2.0f);
    }

    public override void ReturnToPool()
    {
        if (_PoolManager != null && gameObject.activeInHierarchy)
        {
            _PoolManager.ReleaseDart(gameObject);
        }
    }
}