using UnityEngine;

// 기차의 브레인. 외부(게임 흐름·입력·네트워크)의 신호를 받아
// TrainBehaviour(바디)에 명령을 전달하고, 피드백으로 상태를 소유/갱신한다.
public class TrainController : MonoBehaviour
{
    [SerializeField] private TrainBehaviour train;

    // 피드백으로 갱신되는 상태 미러. 타 시스템은 이 값을 조회한다.
    private TrainPhase currentPhase;
    public TrainPhase CurrentPhase => currentPhase;

    // 스테이지 클리어/실패 재전파 신호. 게임 흐름(GameManager/UI/네트워크)이 구독한다.
    public event System.Action OnStageClear;
    public event System.Action<FailReason> OnStageFail;

    private void OnEnable()
    {
        if (train != null)
        {
            train.OnPhaseChanged += HandlePhaseChanged;
            train.OnStageClear += HandleStageClear;
            train.OnStageFail += HandleStageFail;
        }

        // 게임 시작 신호 수신 지점(GameManager 구현 시 연결).
        // GameManager.OnGameStart += StartGame;
    }

    private void OnDisable()
    {
        if (train != null)
        {
            train.OnPhaseChanged -= HandlePhaseChanged;
            train.OnStageClear -= HandleStageClear;
            train.OnStageFail -= HandleStageFail;
        }

        // GameManager.OnGameStart -= StartGame;
    }

    private void Start()
    {
        // 작동 테스트: GameManager 배선 전까지 임시로 직접 시작.
        StartGame();
    }

    // ── 외부 신호 진입점 (입력·게임흐름·네트워크가 호출) ──────────────
    public void StartGame() => train.StartMoving();
    public void StopTrain() => train.Stop();
    public void PauseTrain() => train.Pause();
    public void ResumeTrain() => train.Resume();
    public void AccelerateTrain(float delta = 0.1f) => train.Accelerate(delta);
    public void DecelerateTrain(float delta = 0.1f) => train.Decelerate(delta);

    // ── 피드백 수신 ────────────────────────────────────────────────
    private void HandlePhaseChanged(TrainPhase phase)
    {
        currentPhase = phase;
        Debug.Log($"[Train] Phase → {phase}"); // 임시: 이벤트 출력 대체용
        // TODO: 생존 판정/게임 흐름/네트워크 동기화로 재전파할 확장 지점.
    }

    // 종착점 도착 → 스테이지 클리어를 게임 흐름으로 재전파한다.
    private void HandleStageClear()
    {
        Debug.Log("[Train] 스테이지 클리어"); // 임시: 이벤트 출력 대체용
        OnStageClear?.Invoke();
        // TODO: GameManager.NotifyStageClear() 등 스테이지 진행 시스템으로 연결.
    }

    // 트랙 소진/엔진 파손 → 스테이지 실패를 게임 흐름으로 재전파한다.
    private void HandleStageFail(FailReason reason)
    {
        Debug.Log($"[Train] 스테이지 실패: {reason}"); // 임시: 이벤트 출력 대체용
        OnStageFail?.Invoke(reason);
        // TODO: GameManager.NotifyStageFail(reason) 등 스테이지 진행 시스템으로 연결.
    }
}
