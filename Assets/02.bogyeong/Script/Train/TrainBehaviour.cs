using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 상황 분류용 상태. 이번 범위(이동)에서는 사용하지 않으며,
// 향후 과열/화재 등 열차 컨디션 분류로 재정의 예정(예약).
public enum TrainStateType
{
    Idle,
    Moving,
    Stopped,
    Accelerating,
    Decelerating
}

// 이동 진행 단계. 열차의 이동 상태를 나타내는 권위 있는 값.
public enum TrainPhase
{
    Idle,
    Start,
    Progress,
    Paused,
    End
}
public enum TrainDirection
{
    Forward,
    Left,
    Right,
}

// 실패 사유. 연출 분기(급정차 등)와 게임 흐름 전파에 쓰인다.
public enum FailReason
{
    DeadEnd,          // 트랙 소진(막다른 길) → 급정차 + 폭발
    EngineDestroyed,  // 과열 화재로 엔진 내구도 0 → 폭발(운행 중 추가 연출 고려중)
}

public struct TrainData
{
    public float speed;
    public Vector3 position;
    public Quaternion rotation;

}
public class TrainBehaviour : MonoBehaviour
{
    [SerializeField] private TrainSection[] trainSections;

    [SerializeField] private Transform engineRoom;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float maxSpeed = 1f;
    [Tooltip("출발/정지 시 속도 변화율(타일/초²). 클수록 빠르게 가감속.")]
    [SerializeField] private float accelRate = 2f;
    [SerializeField] private float onceMoveRange = 1f;
    [SerializeField] private float carSpacing = 1f;

    [SerializeField] private TrainPhase trainPhase = TrainPhase.Idle;
    [SerializeField] private TrainDirection trainDirection = TrainDirection.Forward;

    // 경로(레일). 할당되면 forward+turn 대신 이 경로를 추종한다.
    [SerializeField] private RailPath railPath;
    [Tooltip("종착점 연결 시 현재 속도에 곱해지는 대시 배수(예: 3 = 3배 가속).")]
    [SerializeField] private float goalDashMultiplier = 3f;
    [Tooltip("실패 시 엔진 위치에 생성할 폭발 이펙트(선택).")]
    [SerializeField] private GameObject explosionPrefab;

    [Header("과열(엔진·물탱크 공유)")]
    [SerializeField] private float heatMax = 100f;
    [Tooltip("진행 시간에 비례한 초당 과열 상승량.")]
    [SerializeField] private float heatRisePerSec = 5f;
    [Tooltip("이 비율 이상일 때만 냉각이 가능하다(뜨거울 때만 식힐 수 있음).")]
    [Range(0f, 1f)]
    [SerializeField] private float coolEnabledAboveRatio = 0.5f;

    [SerializeField] private TMPro.TextMeshProUGUI countDownText;

    // 피드백 신호(옵저버). 컨트롤러/타 시스템이 폴링 없이 상태를 동기화한다.
    // 네트워크 대비: 상태 전이/스냅샷을 이벤트로만 노출한다.
    public event System.Action<TrainPhase> OnPhaseChanged;
    public event System.Action<TrainData> OnTrainState;

    // 스테이지 클리어 신호. 종착점에 도착하면 1회 발행한다.
    public event System.Action OnStageClear;

    // 스테이지 실패 신호(사유 포함). 트랙 소진 또는 엔진 내구도 0에서 1회 발행한다.
    public event System.Action<FailReason> OnStageFail;

    public TrainPhase Phase => trainPhase;

    // 서브시스템(엔진 과열 등)이 조회하는 이동 상태.
    public bool IsMoving => trainPhase == TrainPhase.Progress;
    public float CurrentSpeed => _currentSpeed;

    // 과열 게이지(엔진·물탱크가 동일하게 관측). 진행 시간에 비례해 상승.
    public float HeatRatio => heatMax > 0f ? _heat / heatMax : 0f;
    // 냉각 가능 여부: 게이지가 임계 비율 이상(뜨거울 때)일 때만 냉각이 통한다.
    public bool CanCool => HeatRatio >= coolEnabledAboveRatio;

    // 냉각(물탱크/입력이 호출). 임계 이상에서만 실제로 낮아진다.
    public void Cool(float amount)
    {
        if (!CanCool) return;
        _heat = Mathf.Max(0f, _heat - Mathf.Abs(amount));
    }

    // 현재 이동 상태 스냅샷. 동기화·타 클래스 조회의 표준 형태.
    public TrainData CurrentState => new TrainData
    {
        speed = speed,
        position = engineRoom != null ? engineRoom.position : transform.position,
        rotation = engineRoom != null ? engineRoom.rotation : transform.rotation,
    };

    private Vector3 moveForward;
    private int _countDown = 5;

    // 경로 추종 상태. _tileProgress는 타일 단위(타일당 길이 1 = 등시간).
    private float _tileProgress;
    private int _lastTileIndex = -1;
    private bool _goalDashApplied;

    // 부드러운 속도 변화용. _currentSpeed가 목표 속도로 서서히 수렴한다.
    private float _currentSpeed;
    private bool _stopRequested;
    private bool _failed;

    // 공유 과열 수치.
    private float _heat;
    private bool _prevCanCool; // 냉각 가능 구간 진입/이탈 로그용(임시)

    // 엔진(리드)의 이동 경로 기록. index 0이 가장 최근 지점.
    private readonly List<Pose> _pathHistory = new List<Pose>();
    private float _maxPathDistance;

    // Rigidbody 물리 이동용 캐시. _bodies는 trainSections와 인덱스 정렬.
    private Rigidbody _engineBody;
    private Rigidbody[] _bodies;

    private void Awake()
    {
        InitializeSections();
    }

    // 섹션을 탐색·정렬(엔진 우선)하고, engineRoom을 엔진 차량으로 지정한 뒤 초기화한다.
    private void InitializeSections()
    {
        if (trainSections == null || trainSections.Length == 0)
        {
            trainSections = GetComponentsInChildren<TrainSection>();
        }

        OrderEngineFirst();

        if (trainSections.Length > 0 && trainSections[0].SectionType == SectionType.Engine)
        {
            engineRoom = trainSections[0].transform;
        }

        CacheBodies();

        foreach (TrainSection section in trainSections)
        {
            section.Initialize(this);
        }

        SnapEngineToRailStart();
        SeedPathHistory();
    }

    // railPath가 할당되면 엔진을 경로 시작 지점에 배치한다(경로기록 시드 이전에 호출).
    private void SnapEngineToRailStart()
    {
        if (railPath == null || engineRoom == null) return;

        _tileProgress = 0f;
        _lastTileIndex = -1;
        _goalDashApplied = false;
        _currentSpeed = 0f;
        _stopRequested = false;
        _failed = false;
        _heat = 0f;
        _prevCanCool = false;

        Pose start = railPath.Evaluate(0f);
        if (_engineBody != null)
        {
            _engineBody.position = start.position;
            _engineBody.rotation = start.rotation;
        }
        engineRoom.position = start.position;
        engineRoom.rotation = start.rotation;
    }

    // 엔진 섹션을 배열 맨 앞(index 0)으로 옮긴다. 나머지 순서는 유지.
    private void OrderEngineFirst()
    {
        int engineIndex = System.Array.FindIndex(
            trainSections, s => s != null && s.SectionType == SectionType.Engine);

        if (engineIndex > 0)
        {
            TrainSection engine = trainSections[engineIndex];
            for (int i = engineIndex; i > 0; i--)
            {
                trainSections[i] = trainSections[i - 1];
            }
            trainSections[0] = engine;
        }
    }

    // 엔진/차량의 Rigidbody를 캐시하고 Kinematic으로 강제한다.
    private void CacheBodies()
    {
        _engineBody = engineRoom != null ? engineRoom.GetComponent<Rigidbody>() : null;
        EnsureKinematic(_engineBody);

        _bodies = new Rigidbody[trainSections.Length];
        for (int i = 0; i < trainSections.Length; i++)
        {
            _bodies[i] = trainSections[i].GetComponent<Rigidbody>();
            EnsureKinematic(_bodies[i]);
        }
    }

    private void EnsureKinematic(Rigidbody body)
    {
        if (body == null) return;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // 생성 직후 차량 간격을 즉시 배치하고, 엔진 뒤로 직선 경로를 미리 채운다.
    private void SeedPathHistory()
    {
        if (engineRoom == null) return;

        int followerCount = 0;
        foreach (TrainSection section in trainSections)
        {
            if (section.transform != engineRoom) followerCount++;
        }

        _maxPathDistance = carSpacing * followerCount + carSpacing;

        _pathHistory.Clear();
        _pathHistory.Add(new Pose(engineRoom.position, engineRoom.rotation));
        _pathHistory.Add(new Pose(
            engineRoom.position - engineRoom.forward * _maxPathDistance, engineRoom.rotation));

        ArrangeSections();
    }

    // 각 뒤 차량을 엔진 뒤로 carSpacing 간격만큼 즉시 배치(스냅)한다.
    // 생성 직후 호출되며, 스폰 후 열차를 재배치했다면 다시 호출할 수 있다.
    public void ArrangeSections()
    {
        if (engineRoom == null) return;

        Vector3 enginePos = engineRoom.position;
        Quaternion engineRot = engineRoom.rotation;
        Vector3 back = -engineRoom.forward;

        int carIndex = 0;
        for (int i = 0; i < trainSections.Length; i++)
        {
            TrainSection section = trainSections[i];
            if (section.transform == engineRoom) continue;

            carIndex++;
            section.transform.SetPositionAndRotation(
                enginePos + back * (carSpacing * carIndex), engineRot);
        }
    }

    private void FixedUpdate()
    {
        // 페이즈별 과열 처리(Progress 누적 / Idle 초기화 / 그 외 동결).
        UpdateHeatByPhase();

        // Progress에서만 이동한다(Idle/Start/Paused/End는 정지).
        if (trainPhase != TrainPhase.Progress) return;

        foreach (TrainSection section in trainSections)
        {
            section.OnTrainTick();
        }

        MoveEngine();
        UpdateFollowers(false);

        OnTrainState?.Invoke(CurrentState);
    }

    // 페이즈별 공유 과열 처리. 중간 저장/정지 구간을 위해 비활성 페이즈에서는 누적하지 않는다.
    // Progress: 진행 시간에 비례 상승 / Idle: 초기화 / Start·Paused·End: 동결.
    private void UpdateHeatByPhase()
    {
        if (trainPhase == TrainPhase.Idle)
        {
            _heat = 0f;
        }
        else if (trainPhase == TrainPhase.Progress)
        {
            _heat = Mathf.Min(heatMax, _heat + heatRisePerSec * Time.fixedDeltaTime);
        }
        // Start / Paused / End: 동결(변경 없음).

        // 임시: 냉각 가능 구간 진입/이탈 로그.
        if (CanCool != _prevCanCool)
        {
            _prevCanCool = CanCool;
            Debug.Log(CanCool
                ? $"[Train] 냉각 가능 구간 진입 (heat={HeatRatio:P0})"
                : $"[Train] 냉각 불가 구간 (heat={HeatRatio:P0})");
        }
    }

    // 엔진을 Kinematic Rigidbody로 이동/회전시키고, 그 목표 Pose를 경로에 기록한다.
    private void MoveEngine()
    {
        // railPath가 있으면 경로 추종, 없으면 기존 forward+turn 폴백.
        if (railPath != null)
        {
            MoveEngineAlongRail();
            return;
        }

        Vector3 currentPos = _engineBody != null ? _engineBody.position : engineRoom.position;
        Quaternion currentRot = _engineBody != null ? _engineBody.rotation : engineRoom.rotation;

        moveForward = transform.forward * speed;
        Vector3 nextPos = currentPos + moveForward;

        Vector3 euler = currentRot.eulerAngles;
        if (trainDirection == TrainDirection.Left)
        {
            euler += new Vector3(0, -1, 0);
            if (euler.y <= -90)
            {
                trainDirection = TrainDirection.Forward;
            }
        }
        else if (trainDirection == TrainDirection.Right)
        {
            euler += new Vector3(0, 1, 0);
            if (euler.y >= 90)
            {
                trainDirection = TrainDirection.Forward;
            }
        }
        Quaternion nextRot = Quaternion.Euler(euler);

        if (_engineBody != null)
        {
            _engineBody.MovePosition(nextPos);
            _engineBody.MoveRotation(nextRot);
        }
        else
        {
            engineRoom.position = nextPos;
            engineRoom.rotation = nextRot;
        }

        RecordEnginePose(nextPos, nextRot);
    }

    // 레일 경로를 타일 단위로 추종한다. speed = 타일/초 → 타일당 등시간(직선=곡선).
    private void MoveEngineAlongRail()
    {
        int tileCount = railPath.TileCount;
        if (tileCount == 0) return;

        // 레일이 종착점에 완전히 이어지면 빠르게 이동(대시). 1회만 적용.
        // 현재 순항 속도에 배수를 곱해, 기준 속도가 얼마든 항상 눈에 띄게 가속한다.
        if (!_goalDashApplied && railPath.IsConnectedToGoal)
        {
            speed *= goalDashMultiplier;
            _goalDashApplied = true;
            Debug.Log($"[Train] 종착 연결 → 대시 (speed={speed:F2})"); // 임시
        }

        // 종점 판정: 종착 연결 시 goal 타일 통과 지점, 아니면 놓인 레일 끝.
        float endProgress = tileCount;
        if (railPath.IsConnectedToGoal)
        {
            endProgress = Mathf.Min(endProgress, railPath.GoalTileIndex + 1);
        }

        // 목표 속도 결정 → 부드러운 가감속. 순항 속도 speed로 수렴하되,
        // 정지 요청 또는 '종착(성공) 임박' 시 0으로 감속(정지 지점에 맞춰 브레이킹).
        // 미연결 레일 끝은 감속하지 않는다(추후 실패 처리에서 사용).
        float target = speed;
        if (_stopRequested)
        {
            target = 0f;
        }
        else if (railPath.IsConnectedToGoal)
        {
            float remaining = endProgress - _tileProgress;
            float brakingDist = (_currentSpeed * _currentSpeed) / (2f * Mathf.Max(accelRate, 0.0001f));
            if (remaining <= brakingDist) target = 0f;
        }
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, target, accelRate * Time.fixedDeltaTime);

        // 수동 정지(Stop) 완료: 속도가 0에 수렴하면 종료.
        if (_stopRequested && _currentSpeed <= 0.0001f)
        {
            SetPhase(TrainPhase.End);
            return;
        }

        _tileProgress += _currentSpeed * Time.fixedDeltaTime;

        // 밟은 타일 영구 고정.
        int idx = Mathf.FloorToInt(_tileProgress);
        if (idx > _lastTileIndex)
        {
            railPath.LockUpTo(Mathf.Min(idx, tileCount - 1));
            _lastTileIndex = idx;
        }

        if (_tileProgress >= endProgress)
        {
            _tileProgress = endProgress;
            _currentSpeed = 0f;
            Pose endPose = railPath.Evaluate(_tileProgress);
            ApplyEnginePose(endPose);
            RecordEnginePose(endPose.position, endPose.rotation);

            // 종착점 도착 → 클리어. 연결 없이 레일 끝(막다른 길) → 트랙 소진 실패.
            if (railPath.IsConnectedToGoal)
            {
                OnStageClear?.Invoke();
                SetPhase(TrainPhase.End);
            }
            else
            {
                Fail(FailReason.DeadEnd);
            }
            return;
        }

        Pose pose = railPath.Evaluate(_tileProgress);
        ApplyEnginePose(pose);
        RecordEnginePose(pose.position, pose.rotation);
    }

    private void ApplyEnginePose(Pose pose)
    {
        if (_engineBody != null)
        {
            _engineBody.MovePosition(pose.position);
            _engineBody.MoveRotation(pose.rotation);
        }
        else
        {
            engineRoom.position = pose.position;
            engineRoom.rotation = pose.rotation;
        }
    }

    // 엔진 목표 Pose를 경로 기록 맨 앞에 추가하고 오래된 기록을 잘라낸다.
    private void RecordEnginePose(Vector3 position, Quaternion rotation)
    {
        _pathHistory.Insert(0, new Pose(position, rotation));
        TrimHistory(_maxPathDistance);
    }

    private void TrimHistory(float maxDistance)
    {
        float accumulated = 0f;
        for (int i = 0; i < _pathHistory.Count - 1; i++)
        {
            accumulated += Vector3.Distance(_pathHistory[i].position, _pathHistory[i + 1].position);
            if (accumulated >= maxDistance)
            {
                // 경계 지점(i+1)은 유지해야 SamplePath가 maxDistance까지 보간할 수 있다.
                int keepCount = i + 2;
                if (keepCount < _pathHistory.Count)
                {
                    _pathHistory.RemoveRange(keepCount, _pathHistory.Count - keepCount);
                }
                return;
            }
        }
    }

    // 각 뒤 차량을 엔진 경로상 carSpacing 간격만큼 뒤 지점에 배치한다.
    // snap=true면 초기 정렬(즉시 이동), false면 Rigidbody 물리 이동.
    private void UpdateFollowers(bool snap)
    {
        int carIndex = 0;
        for (int i = 0; i < trainSections.Length; i++)
        {
            TrainSection section = trainSections[i];
            if (section.transform == engineRoom) continue;

            carIndex++;
            Pose pose = SamplePath(carSpacing * carIndex);
            ApplyPose(_bodies[i], section.transform, pose, snap);
        }
    }

    private void ApplyPose(Rigidbody body, Transform target, Pose pose, bool snap)
    {
        if (body != null && !snap)
        {
            body.MovePosition(pose.position);
            body.MoveRotation(pose.rotation);
        }
        else
        {
            target.position = pose.position;
            target.rotation = pose.rotation;
        }
    }

    // 경로 기록을 거슬러 distanceBack 만큼 뒤의 위치·회전을 보간해 반환한다.
    private Pose SamplePath(float distanceBack)
    {
        if (_pathHistory.Count == 0) return new Pose(engineRoom.position, engineRoom.rotation);

        float accumulated = 0f;
        for (int i = 0; i < _pathHistory.Count - 1; i++)
        {
            Vector3 a = _pathHistory[i].position;
            Vector3 b = _pathHistory[i + 1].position;
            float segment = Vector3.Distance(a, b);

            if (accumulated + segment >= distanceBack)
            {
                float t = segment > 0f ? (distanceBack - accumulated) / segment : 0f;
                return new Pose(
                    Vector3.Lerp(a, b, t),
                    Quaternion.Slerp(_pathHistory[i].rotation, _pathHistory[i + 1].rotation, t));
            }
            accumulated += segment;
        }

        return _pathHistory[_pathHistory.Count - 1];
    }

    // ── 명령 API (TrainController만 호출) ─────────────────────────────
    // 외부는 이 메서드들로만 상태를 바꾼다. 필드 직접 접근 금지.

    public void StartMoving()
    {
        if (trainPhase != TrainPhase.Idle) return;
        SetPhase(TrainPhase.Start);
        StartCoroutine(StartCountDown());
    }

    // 이동 종료. 진행/일시정지 중 어느 쪽에서도 정지시킨다.
    public void Stop()
    {
        if (trainPhase == TrainPhase.Idle || trainPhase == TrainPhase.End) return;

        // 레일 주행 중이면 부드럽게 감속 후 정지(FixedUpdate에서 0 수렴 시 End).
        if (railPath != null && trainPhase == TrainPhase.Progress)
        {
            _stopRequested = true;
            return;
        }
        SetPhase(TrainPhase.End);
    }

    // 스테이지 실패. 트랙 소진 또는 엔진 내구도 0에서 호출된다(중복 방지).
    // 폭발은 공통, 급정차는 막다른 길(DeadEnd)에서만.
    public void Fail(FailReason reason)
    {
        if (_failed || trainPhase == TrainPhase.End) return;
        _failed = true;

        SpawnExplosion(); // 폭발: 모든 실패 공통

        if (reason == FailReason.DeadEnd)
        {
            _currentSpeed = 0f;   // 급정차: 막다른 길에서만 즉시 정지
            _stopRequested = false;
        }
        // else EngineDestroyed: 운행 중 폭발 추가 연출은 고려중(TODO).

        OnStageFail?.Invoke(reason); // 신호(사유 포함)
        SetPhase(TrainPhase.End);
    }

    private void SpawnExplosion()
    {
        if (explosionPrefab == null) return;
        Vector3 at = engineRoom != null ? engineRoom.position : transform.position;
        Quaternion rot = engineRoom != null ? engineRoom.rotation : transform.rotation;
        Instantiate(explosionPrefab, at, rot);
    }

    // 진행 중일 때만 일시정지.
    public void Pause()
    {
        if (trainPhase != TrainPhase.Progress) return;
        SetPhase(TrainPhase.Paused);
    }

    // 일시정지 상태에서만 재개.
    public void Resume()
    {
        if (trainPhase != TrainPhase.Paused) return;
        SetPhase(TrainPhase.Progress);
    }

    // 가속/감속은 phase가 아니라 speed 파라미터로 처리한다.
    public void Accelerate(float delta)
    {
        speed = Mathf.Clamp(speed + Mathf.Abs(delta), 0f, maxSpeed);
    }

    public void Decelerate(float delta)
    {
        speed = Mathf.Clamp(speed - Mathf.Abs(delta), 0f, maxSpeed);
    }

    // 이동 phase 전이의 단일 창구. 여기서만 값을 바꾸고 이벤트를 발행한다.
    private void SetPhase(TrainPhase next)
    {
        if (trainPhase == next) return;
        trainPhase = next;
        OnPhaseChanged?.Invoke(next);
    }

    IEnumerator StartCountDown()
    {
        while (_countDown > 0)
        {
            countDownText.text = _countDown.ToString();
            yield return new WaitForSeconds(1f);
            _countDown--;
        }
        countDownText.text = "";
        SetPhase(TrainPhase.Progress);
    }
}
