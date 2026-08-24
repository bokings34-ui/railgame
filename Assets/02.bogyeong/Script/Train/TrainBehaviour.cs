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
    [SerializeField] private float speed = 0.1f;
    [SerializeField] private float maxSpeed = 1f;
    [SerializeField] private float onceMoveRange = 1f;
    [SerializeField] private float carSpacing = 1f;

    [SerializeField] private TrainPhase trainPhase = TrainPhase.Idle;
    [SerializeField] private TrainDirection trainDirection = TrainDirection.Forward;

    [SerializeField] private TMPro.TextMeshProUGUI countDownText;

    // 피드백 신호(옵저버). 컨트롤러/타 시스템이 폴링 없이 상태를 동기화한다.
    // 네트워크 대비: 상태 전이/스냅샷을 이벤트로만 노출한다.
    public event System.Action<TrainPhase> OnPhaseChanged;
    public event System.Action<TrainData> OnTrainState;

    public TrainPhase Phase => trainPhase;

    // 현재 이동 상태 스냅샷. 동기화·타 클래스 조회의 표준 형태.
    public TrainData CurrentState => new TrainData
    {
        speed = speed,
        position = engineRoom != null ? engineRoom.position : transform.position,
        rotation = engineRoom != null ? engineRoom.rotation : transform.rotation,
    };

    private Vector3 moveForward;
    private int _countDown = 5;

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

        SeedPathHistory();
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

    // 시작 시 엔진 뒤로 직선 경로를 미리 채워 뒤 차량이 곧바로 일정 간격으로 정렬되게 한다.
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

        UpdateFollowers(true);
    }

    private void FixedUpdate()
    {
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

    // 엔진을 Kinematic Rigidbody로 이동/회전시키고, 그 목표 Pose를 경로에 기록한다.
    private void MoveEngine()
    {
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
        SetPhase(TrainPhase.End);
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
