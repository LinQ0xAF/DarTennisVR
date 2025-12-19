using System;

namespace Gameplay.Match.Interfaces
{
    /// <summary>
    /// 매치(게임 전체)의 흐름과 설정을 관리하는 인터페이스.
    /// </summary>
    public interface IMatchManager
    {
        /// <summary>총 세트 수</summary>
        int TotalSets { get; }
        
        /// <summary>현재 진행 중인 세트 인덱스 (1-based)</summary>
        int CurrentSetIndex { get; }
        
        /// <summary>세트 당 제한 시간(초)</summary>
        int TimeLimitSeconds { get; }

        /// <summary>세트 시작 전 준비 단계 이벤트</summary>
        event Action OnSetPreStart;
        
        /// <summary>세트 시작 이벤트</summary>
        event Action OnSetStart;
        
        /// <summary>총 세트 수가 설정되었을 때 이벤트</summary>
        event Action<int> OnSetsConfigured;

        /// <summary>현재 진행 시간(초)을 반환</summary>
        float GetElapsedSeconds();

        /// <summary>매치 종료 후 대기 시간(초)</summary>
        float MatchEndWaitSeconds { get; }

        /// <summary>로비로 돌아가기</summary>
        void ReturnToLobby();
    }

    /// <summary>
    /// 결과 타입(TResult)을 포함하는 매치 매니저 인터페이스.
    /// </summary>
    /// <typeparam name="TResult">승패 결과 타입 (예: ulong? for winnerId, bool for success/fail)</typeparam>
    public interface IMatchManager<TResult> : IMatchManager
    {
        /// <summary>매치 최종 결과 이벤트</summary>
        event Action<TResult> OnMatchResult;
    }
}
