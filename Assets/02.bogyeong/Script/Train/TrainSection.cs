using UnityEngine;

public enum SectionType
{
    None,
    Engine,
    WaterTank,
    Cargo,
    Production,
    
}

// 열차 차량 1칸의 베이스. 파생 클래스가 타입별 역할과 기능을 부여한다.
public abstract class TrainSection : MonoBehaviour
{
    public abstract SectionType SectionType { get; }

    // 열차 초기화 시 1회 호출. 이후 역할별 데이터 테이블 바인딩이 여기 연결된다.
    public virtual void Initialize(TrainBehaviour train) { }

    // 열차 진행 중 매 틱 호출되는 실행 훅.
    public virtual void OnTrainTick() { }
}
