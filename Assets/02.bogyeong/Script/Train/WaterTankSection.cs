using UnityEngine;

// 물탱크 차량. 엔진과 동일한 공유 과열 수치를 관측하고,
// 냉각 가능 구간(과열 임계 이하)에서 냉각을 수행한다.
public class WaterTankSection : TrainSection
{
    public override SectionType SectionType => SectionType.WaterTank;

    [Tooltip("냉각 출력(초당 과열 감소량).")]
    [SerializeField] private float coolPerSecond = 20f;

    private TrainBehaviour _train;

    // 공유 과열 관측(엔진과 동일 수치).
    public float HeatRatio => _train != null ? _train.HeatRatio : 0f;
    public bool CanCool => _train != null && _train.CanCool;

    public override void Initialize(TrainBehaviour train)
    {
        _train = train;
    }

    // 지속 냉각 1틱. 냉각 가능 구간(과열 임계 이상)에서만 실제로 낮아진다.
    // 입력 어댑터가 매 프레임 호출한다. 물 소비 모델은 후속 범위.
    public void CoolTick(float dt)
    {
        if (_train == null) return;
        _train.Cool(coolPerSecond * dt);
    }
}
