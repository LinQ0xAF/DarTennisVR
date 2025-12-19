using System;

namespace Gameplay.Match.Interfaces
{
    /// <summary>
    /// 개별 세트의 진행과 상태를 관리하는 인터페이스.
    /// </summary>
    public interface ISetManager
    {
        /// <summary>세트 제한 시간(초)</summary>
        int TimeLimitSeconds { get; }

        /// <summary>세트 시작 전 준비 단계 이벤트</summary>
        event Action OnSetPreStart;
        
        /// <summary>세트 시작 이벤트</summary>
        event Action OnSetStart;
        
        /// <summary>세트 종료 이벤트</summary>
        event Action OnSetEnd;

        /// <summary>매치가 종료되었음을 알림</summary>
        void NotifyMatchEnded();
    }

    /// <summary>
    /// 결과 타입(TResult)을 포함하는 세트 매니저 인터페이스.
    /// </summary>
    /// <typeparam name="TResult">세트 결과 타입</typeparam>
    public interface ISetManager<TResult> : ISetManager
    {
        /// <summary>세트 결과 이벤트</summary>
        event Action<TResult> OnSetResult;
    }
}
