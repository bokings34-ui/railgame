using UnityEngine;

public enum RailType
{
    Straight, // 직선: 서로 반대인 두 포트
    Curve90,  // 90° 곡선: 서로 인접한(직교) 두 포트
}

// 레일 블럭 1칸(타일). 그리드 셀 중심(=transform.position)에 놓이는 "노드"다.
// 직선/곡선 타입은 저자가 지정하지 않고, 인접 블럭 연결 위치에 따라 RailPath가 자동 결정한다.
// 연결은 직교(N/E/S/W)만 지원한다(사선 연결 없음).
public class RailBlock : MonoBehaviour
{
    // 사분원을 큐빅 베지어로 근사할 때의 컨트롤 핸들 계수: 4/3 * (sqrt(2) - 1).
    private const float ArcHandle = 0.5522847498f;

    [Header("비주얼(선택) — 연결에 따라 자동 토글/회전")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private GameObject straightVisual;
    [SerializeField] private GameObject curveVisual;

    // 런타임 상태(직렬화 안 함).
    [System.NonSerialized] private bool _locked;
    [System.NonSerialized] private RailType _derivedType;

    // 밟고 지나간 블럭은 영구 고정(탈착 불가).
    public bool IsLocked => _locked;

    // 연결로부터 자동 결정된 타입(BuildPath의 ApplyShape 이후 유효).
    public RailType DerivedType => _derivedType;

    public void Lock() => _locked = true;

    // BuildPath가 이 타일의 진입/진출 포트를 확정한 뒤 호출.
    // 진입·진출이 반대면 직선, 직교면 곡선으로 타입을 자동 결정하고 비주얼을 갱신한다.
    public void ApplyShape(GridDir entryPort, GridDir exitPort)
    {
        _derivedType = entryPort == exitPort.Opposite() ? RailType.Straight : RailType.Curve90;

        if (straightVisual != null) straightVisual.SetActive(_derivedType == RailType.Straight);
        if (curveVisual != null) curveVisual.SetActive(_derivedType == RailType.Curve90);

        if (visualRoot != null)
        {
            // 진입 시 안쪽 이동 방향으로 정렬(곡선 메쉬의 정확한 회전은 저자가 조정).
            Vector3 inDir = -entryPort.ToWorld();
            if (inDir.sqrMagnitude > 1e-6f)
                visualRoot.rotation = Quaternion.LookRotation(inDir, Vector3.up);
        }
    }

    // 타일 로컬 경로 평가. entryPort/exitPort는 이 타일의 두 포트(바깥 방향),
    // t는 진입점(0) → 진출점(1)의 정규화 파라미터. 반환 Pose는 월드 위치·접선 회전.
    public Pose EvaluateLocal(GridDir entryPort, GridDir exitPort, float t, float cellSize)
    {
        float half = cellSize * 0.5f;
        Vector3 center = transform.position;
        Vector3 entryPoint = center + entryPort.ToWorld() * half;
        Vector3 exitPoint = center + exitPort.ToWorld() * half;

        // 접선(이동 방향): 진입은 안쪽(-entryPort), 진출은 바깥쪽(+exitPort).
        Vector3 inDir = -entryPort.ToWorld();
        Vector3 outDir = exitPort.ToWorld();

        Vector3 pos;
        Vector3 tangent;
        if (entryPort == exitPort.Opposite())
        {
            // 직선: 두 에지 중점을 잇는 직선(셀 중심 통과).
            pos = Vector3.LerpUnclamped(entryPoint, exitPoint, t);
            tangent = outDir;
        }
        else
        {
            // 90° 곡선: 큐빅 베지어로 사분원 근사.
            float handle = ArcHandle * half;
            Vector3 p0 = entryPoint;
            Vector3 p1 = entryPoint + inDir * handle;
            Vector3 p2 = exitPoint - outDir * handle;
            Vector3 p3 = exitPoint;
            pos = CubicBezier(p0, p1, p2, p3, t);
            tangent = CubicBezierTangent(p0, p1, p2, p3, t);
        }

        if (tangent.sqrMagnitude < 1e-6f) tangent = outDir;
        Quaternion rot = Quaternion.LookRotation(tangent.normalized, Vector3.up);
        return new Pose(pos, rot);
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0
             + 3f * u * u * t * p1
             + 3f * u * t * t * p2
             + t * t * t * p3;
    }

    private static Vector3 CubicBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return 3f * u * u * (p1 - p0)
             + 6f * u * t * (p2 - p1)
             + 3f * t * t * (p3 - p2);
    }
}
