using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// 업그레이드 데이터 테이블의 순수 C# 표현(포맷 독립).
// CSV/JSON 등 어떤 소스로 로드하든 이 모델로 귀결된다. UnityEngine 외 의존 없음.

// 업그레이드 테이블 1행: (차량 타입, 레벨) + 나머지 스탯 컬럼들.
// 필수 컬럼은 SectionType/Level뿐이고, 그 외 컬럼은 Stats에 문자열 그대로 담아 확장에 열어둔다.
public class UpgradeRow
{
    public SectionType SectionType;
    public int Level;

    // 헤더명 → 셀 문자열(원본). 타입 변환은 접근자로 수행한다.
    public readonly Dictionary<string, string> Stats = new Dictionary<string, string>();

    public string GetString(string column, string fallback = "")
        => Stats.TryGetValue(column, out var v) ? v : fallback;

    public float GetFloat(string column, float fallback = 0f)
        => Stats.TryGetValue(column, out var v)
           && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)
            ? f : fallback;

    public int GetInt(string column, int fallback = 0)
        => Stats.TryGetValue(column, out var v)
           && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
            ? i : fallback;
}

// 업그레이드 테이블 전체. 로더가 채우고, 툴/런타임이 조회한다.
public class UpgradeTable
{
    public readonly List<UpgradeRow> Rows = new List<UpgradeRow>();

    // 이 테이블에 실제로 등장하는 차량 타입들(행 순서 기준, 중복 제거).
    public IEnumerable<SectionType> Sections
        => Rows.Select(r => r.SectionType).Distinct();

    // 특정 차량 타입의 행들(레벨 오름차순).
    public IEnumerable<UpgradeRow> ForSection(SectionType type)
        => Rows.Where(r => r.SectionType == type).OrderBy(r => r.Level);

    // 특정 차량 타입의 레벨 목록(오름차순, 중복 제거).
    public IEnumerable<int> LevelsOf(SectionType type)
        => ForSection(type).Select(r => r.Level).Distinct();

    // (타입, 레벨) 행 조회. 없으면 null.
    public UpgradeRow Find(SectionType type, int level)
        => Rows.FirstOrDefault(r => r.SectionType == type && r.Level == level);
}
