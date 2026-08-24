using UnityEngine;

// 엔진 차량. 공유 과열 수치(TrainBehaviour 소유, 엔진·물탱크 공통 관측)를 읽어
// 화재→내구도 서브시스템을 처리한다.
// - 과열 비율이 화재 임계 이상이면 화재 발생 → 내구도 감소, 0이면 열차 실패.
// - 과열 비율이 소강 임계 이하로 내려가면 화재 종료 → 약간의 대기 후 내구도 재생.
public class EngineSection : TrainSection
{
    public override SectionType SectionType => SectionType.Engine;

    [Header("화재 임계(과열 비율 0~1)")]
    [Tooltip("이 비율 이상이면 화재 발생.")]
    [Range(0f, 1f)]
    [SerializeField] private float fireThresholdRatio = 1f;
    [Tooltip("이 비율 이하로 내려가면 화재 소강.")]
    [Range(0f, 1f)]
    [SerializeField] private float fireCalmRatio = 0.5f;

    [Header("엔진 내구도")]
    [SerializeField] private float durabilityMax = 100f;
    [Tooltip("화재 중 초당 내구도 감소량.")]
    [SerializeField] private float fireDamagePerSec = 25f;
    [Tooltip("화재 소강 후 재생 시작까지 대기(초).")]
    [SerializeField] private float regenDelay = 2f;
    [Tooltip("재생 중 초당 내구도 회복량.")]
    [SerializeField] private float regenPerSec = 15f;

    private TrainBehaviour _train;
    private float _durability;
    private bool _onFire;
    private float _calmTimer;

    // UI/타 시스템 조회용.
    public float DurabilityRatio => durabilityMax > 0f ? _durability / durabilityMax : 0f;
    public bool OnFire => _onFire;

    public override void Initialize(TrainBehaviour train)
    {
        _train = train;
        ResetState();
    }

    // 내구도/화재 상태 초기화(Idle 진입 시 및 초기화 시).
    private void ResetState()
    {
        _durability = durabilityMax;
        _onFire = false;
        _calmTimer = 0f;
    }

    private void Update()
    {
        if (_train == null) return;

        // 중간 저장/정지 구간 대비: Progress에서만 누적, Idle에서 초기화, 그 외 동결.
        switch (_train.Phase)
        {
            case TrainPhase.Idle:
                ResetState();
                return;
            case TrainPhase.Progress:
                break;
            default: // Start / Paused / End
                return;
        }

        float dt = Time.deltaTime;
        float ratio = _train.HeatRatio;

        // 화재 발생/소강 전이(공유 과열 비율 기준).
        if (!_onFire && ratio >= fireThresholdRatio)
        {
            _onFire = true;
            Debug.Log($"[Engine] 화재 발생 (heat={ratio:P0})"); // 임시
        }
        else if (_onFire && ratio <= fireCalmRatio)
        {
            _onFire = false;
            _calmTimer = 0f;
            Debug.Log($"[Engine] 화재 소강 (durability={_durability:F0})"); // 임시
        }

        if (_onFire)
        {
            // 화재 중 내구도 감소 → 0이면 실패.
            _durability -= fireDamagePerSec * dt;
            if (_durability <= 0f)
            {
                _durability = 0f;
                Debug.Log("[Engine] 내구도 0 → 엔진 파손"); // 임시
                _train.Fail(FailReason.EngineDestroyed);
            }
        }
        else if (_durability < durabilityMax)
        {
            // 소강 후 약간의 시간이 지나면 재생.
            _calmTimer += dt;
            if (_calmTimer >= regenDelay)
            {
                _durability = Mathf.Min(durabilityMax, _durability + regenPerSec * dt);
            }
        }
    }
}
