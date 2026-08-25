using System;
using System.Globalization;
using UnityEngine;

// CSV 텍스트 → UpgradeTable 파서.
// 첫 유효 행을 헤더로 보고, 반드시 "SectionType"과 "Level" 컬럼이 있어야 한다.
// 그 외 컬럼은 이름 그대로 UpgradeRow.Stats에 담아 확장에 열어둔다.
//
// v1 한계: 따옴표/셀 내 콤마 이스케이프를 지원하지 않는 단순 콤마 split이다.
// (스탯 값에 콤마가 필요해지면 파서를 교체하거나 JSON 로더로 전환한다.)
public class CsvUpgradeTableLoader : IUpgradeTableLoader
{
    private const string ColSection = "SectionType";
    private const string ColLevel = "Level";

    public UpgradeTable Load(string text)
    {
        var table = new UpgradeTable();
        if (string.IsNullOrWhiteSpace(text)) return table;

        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        string[] header = null;
        int sectionIdx = -1, levelIdx = -1;

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue; // 빈 줄·주석(#) 무시

            string[] cells = line.Split(',');

            if (header == null)
            {
                header = cells;
                for (int i = 0; i < header.Length; i++)
                {
                    string h = header[i].Trim();
                    header[i] = h;
                    if (h.Equals(ColSection, StringComparison.OrdinalIgnoreCase)) sectionIdx = i;
                    else if (h.Equals(ColLevel, StringComparison.OrdinalIgnoreCase)) levelIdx = i;
                }

                if (sectionIdx < 0 || levelIdx < 0)
                {
                    Debug.LogError($"[UpgradeTable] CSV 헤더에 '{ColSection}'/'{ColLevel}' 컬럼이 필요합니다.");
                    return table;
                }
                continue;
            }

            UpgradeRow row = ParseRow(header, cells, sectionIdx, levelIdx);
            if (row != null) table.Rows.Add(row);
        }

        return table;
    }

    private static UpgradeRow ParseRow(string[] header, string[] cells, int sectionIdx, int levelIdx)
    {
        if (cells.Length <= sectionIdx || cells.Length <= levelIdx) return null;

        if (!Enum.TryParse(cells[sectionIdx].Trim(), true, out SectionType section))
        {
            Debug.LogWarning($"[UpgradeTable] 알 수 없는 SectionType '{cells[sectionIdx]}' — 행 무시.");
            return null;
        }

        if (!int.TryParse(cells[levelIdx].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
        {
            Debug.LogWarning($"[UpgradeTable] Level 파싱 실패 '{cells[levelIdx]}' — 행 무시.");
            return null;
        }

        var row = new UpgradeRow { SectionType = section, Level = level };

        // 필수 두 컬럼을 제외한 나머지를 스탯으로 수용.
        for (int i = 0; i < header.Length && i < cells.Length; i++)
        {
            if (i == sectionIdx || i == levelIdx) continue;
            row.Stats[header[i]] = cells[i].Trim();
        }

        return row;
    }
}
