using UnityEngine;

// 그리드 4방향. 레일 포트/순회 계산의 단일 소스.
// North=+Z, East=+X, South=-Z, West=-X (XZ 평면 기준).
public enum GridDir
{
    North,
    East,
    South,
    West,
}

public static class GridDirExtensions
{
    // 방향 → 셀 오프셋(x = 월드 X축, y = 월드 Z축).
    public static Vector2Int ToOffset(this GridDir dir)
    {
        switch (dir)
        {
            case GridDir.North: return new Vector2Int(0, 1);
            case GridDir.East: return new Vector2Int(1, 0);
            case GridDir.South: return new Vector2Int(0, -1);
            default: return new Vector2Int(-1, 0); // West
        }
    }

    // 방향 → 월드 XZ 평면 단위 벡터.
    public static Vector3 ToWorld(this GridDir dir)
    {
        Vector2Int o = dir.ToOffset();
        return new Vector3(o.x, 0f, o.y);
    }

    public static GridDir Opposite(this GridDir dir)
    {
        return (GridDir)(((int)dir + 2) % 4);
    }
}
