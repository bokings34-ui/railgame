using System.Collections.Generic;
using UnityEngine;

// 그리드에 설치된 RailBlock들을 인접(직교) 연결 순서로 엮어 하나의 경로(열린 경로)를 만들고,
// 타일 파라미터로 경로를 평가·편집(탈착)·잠금하는 매니저.
// 타일의 직선/곡선 타입은 연결 위치로부터 자동 결정된다(사선 연결 없음).
// 배치 규칙/잠금/경로 재구성을 모두 소유하며, 입력 계층은 공개 API에만 의존한다.
public class RailPath : MonoBehaviour
{
    // 직교 4방향(연결 후보). 사선은 포함하지 않는다.
    private static readonly GridDir[] Orthogonal =
        { GridDir.North, GridDir.East, GridDir.South, GridDir.West };

    [SerializeField] private float cellSize = 1f;
    [Tooltip("레일 배치 높이(월드 Y).")]
    [SerializeField] private float railHeight = 0f;

    [Header("경로 시작")]
    [SerializeField] private RailBlock startBlock;
    [Tooltip("시작 블럭에서 처음 향할 직교 방향.")]
    [SerializeField] private GridDir startDir = GridDir.East;

    [Header("종착점(스테이지 끝지점)")]
    [Tooltip("이 블럭까지 레일이 완전히 이어지면 스테이지 종착으로 판정한다. 시작/종착 블럭은 자동 잠금(제거 불가).")]
    [SerializeField] private RailBlock goalBlock;

    [Header("배치 프리팹(타입 구분 없는 단일 블럭)")]
    [SerializeField] private RailBlock blockPrefab;

    // 설치된 블럭(셀 → 블럭).
    private readonly Dictionary<Vector2Int, RailBlock> _grid = new Dictionary<Vector2Int, RailBlock>();

    // 순회 결과: 타일과 그 타일의 진입/진출 포트.
    private struct Segment
    {
        public RailBlock block;
        public GridDir entryPort;
        public GridDir exitPort;
    }
    private readonly List<Segment> _ordered = new List<Segment>();

    // 순회 경로상 종착 블럭의 타일 인덱스(연결 안 되면 -1).
    private int _goalTileIndex = -1;

    public float CellSize => cellSize;
    public float RailHeight => railHeight;
    public int TileCount => _ordered.Count;

    public bool HasGoal => goalBlock != null;
    public int GoalTileIndex => _goalTileIndex;

    // 시작점에서 이어진 경로가 종착 블럭까지 완전히 연결됐는지.
    public bool IsConnectedToGoal => _goalTileIndex >= 0;

    private void Awake()
    {
        RegisterExistingBlocks();
        BuildPath();
        LockFixtures();
    }

    // 시작/종착 블럭은 스테이지 고정물 → 영구 잠금(탈착 불가).
    private void LockFixtures()
    {
        if (startBlock != null) startBlock.Lock();
        if (goalBlock != null) goalBlock.Lock();
    }

    // 자식으로 배치된 RailBlock들을 그리드에 등록한다.
    private void RegisterExistingBlocks()
    {
        _grid.Clear();
        foreach (RailBlock block in GetComponentsInChildren<RailBlock>())
        {
            _grid[WorldToCell(block.transform.position)] = block;
        }

        // 시작/종착 블럭은 명시 참조이므로 자식이 아니어도 반드시 등록한다.
        RegisterFixture(startBlock);
        RegisterFixture(goalBlock);
    }

    private void RegisterFixture(RailBlock block)
    {
        if (block == null) return;
        _grid[WorldToCell(block.transform.position)] = block;
    }

    public Vector2Int WorldToCell(Vector3 world)
    {
        return new Vector2Int(
            Mathf.RoundToInt(world.x / cellSize),
            Mathf.RoundToInt(world.z / cellSize));
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, railHeight, cell.y * cellSize);
    }

    // 시작 블럭+방향에서 직교 이웃 연결을 따라 순회하며 순서 경로를 만든다(열린 경로).
    // 각 타일의 타입(직선/곡선)은 진입·진출 포트로부터 자동 결정된다.
    public void BuildPath()
    {
        _ordered.Clear();
        _goalTileIndex = -1;
        if (startBlock == null) return;

        RailBlock current = startBlock;
        GridDir exitPort = startDir;                 // 시작 타일이 향할 방향
        GridDir entryPort = startDir.Opposite();     // 시작 타일 진입 = 반대편 에지
        HashSet<RailBlock> visited = new HashSet<RailBlock>();

        while (current != null && !visited.Contains(current))
        {
            visited.Add(current);
            current.ApplyShape(entryPort, exitPort); // 연결로 타입 자동 결정
            _ordered.Add(new Segment { block = current, entryPort = entryPort, exitPort = exitPort });

            if (current == goalBlock) _goalTileIndex = _ordered.Count - 1; // 종착 도달

            Vector2Int nextCell = WorldToCell(current.transform.position) + exitPort.ToOffset();
            if (!_grid.TryGetValue(nextCell, out RailBlock next)) break; // 진출 방향에 블럭 없음 → 종점

            entryPort = exitPort.Opposite();
            exitPort = ResolveExit(nextCell, entryPort);
            current = next;
        }
    }

    // 연결된 직교 이웃 중 entryPort가 아닌 방향을 진출 포트로 고른다.
    // 직진(반대편) 우선. 진출 이웃이 없으면 종점 → 직선 통과(entryPort의 반대편).
    private GridDir ResolveExit(Vector2Int cell, GridDir entryPort)
    {
        GridDir straightThrough = entryPort.Opposite();
        bool hasStraight = false;
        bool hasOther = false;
        GridDir other = straightThrough;

        foreach (GridDir dir in Orthogonal)
        {
            if (dir == entryPort) continue;
            if (!_grid.ContainsKey(cell + dir.ToOffset())) continue;

            if (dir == straightThrough) hasStraight = true;
            else if (!hasOther) { hasOther = true; other = dir; }
        }

        if (hasStraight) return straightThrough; // 직진 우선(교차 시 곧게 통과)
        if (hasOther) return other;              // 곡선(직교 연결)
        return straightThrough;                  // 종점: 직선으로 통과 후 다음 루프에서 종료
    }

    // 타일 파라미터(0 ~ TileCount)로 경로 위 Pose를 평가한다. 타일당 길이 1(등시간).
    public Pose Evaluate(float tileProgress)
    {
        if (_ordered.Count == 0)
        {
            // Awake 순서에 안전하도록 지연 빌드.
            RegisterExistingBlocks();
            BuildPath();
        }
        if (_ordered.Count == 0) return new Pose(transform.position, Quaternion.identity);

        float clamped = Mathf.Clamp(tileProgress, 0f, _ordered.Count - 0.0001f);
        int i = Mathf.FloorToInt(clamped);
        if (i >= _ordered.Count) i = _ordered.Count - 1;
        float t = clamped - i;

        Segment s = _ordered[i];
        return s.block.EvaluateLocal(s.entryPort, s.exitPort, t, cellSize);
    }

    // 순회상 index까지의 타일을 영구 고정한다(밟은 타일).
    public void LockUpTo(int index)
    {
        int max = Mathf.Min(index, _ordered.Count - 1);
        for (int i = 0; i <= max; i++)
        {
            _ordered[i].block.Lock();
        }
    }

    // ── 탈착 API (입력 계층은 이 셀 기반 메서드만 호출) ──────────────────
    // 타입은 연결로 자동 결정되므로 배치는 타입 인자 없이 셀 지정만 한다.

    public bool TryPlaceAt(Vector2Int cell)
    {
        if (_grid.ContainsKey(cell)) return false; // 이미 점유된 셀
        if (blockPrefab == null)
        {
            Debug.LogWarning("[RailPath] blockPrefab이 지정되지 않았습니다.", this);
            return false;
        }

        RailBlock block = Instantiate(blockPrefab, CellToWorld(cell), Quaternion.identity, transform);
        _grid[cell] = block;
        BuildPath();
        return true;
    }

    public bool TryRemoveAt(Vector2Int cell)
    {
        if (!_grid.TryGetValue(cell, out RailBlock block)) return false;
        if (block.IsLocked) return false; // 밟고 지나간 블럭은 영구 고정

        _grid.Remove(cell);
        if (Application.isPlaying) Destroy(block.gameObject);
        else DestroyImmediate(block.gameObject);
        BuildPath();
        return true;
    }

    private void OnDrawGizmos()
    {
        // 에디트 모드 미리보기: 현재 배치로 경로를 재구성해 그린다.
        if (!Application.isPlaying)
        {
            RegisterExistingBlocks();
            BuildPath();
        }
        if (_ordered.Count == 0) return;

        Gizmos.color = Color.cyan;
        int samples = _ordered.Count * 12;
        Vector3 prev = Evaluate(0f).position;
        for (int i = 1; i <= samples; i++)
        {
            float p = (float)i / samples * _ordered.Count;
            Vector3 cur = Evaluate(p).position;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
}
