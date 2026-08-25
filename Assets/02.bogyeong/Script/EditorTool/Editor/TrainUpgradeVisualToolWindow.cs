using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 개발자 툴: 업그레이드 데이터 테이블(CSV)의 레벨을 읽어와,
// 차량 타입별 각 레벨에 어떤 비주얼 모델을 쓸지 한 곳에서 바인딩한다.
//   - CSV(레벨·스탯)는 읽기 전용 소스
//   - 레벨→비주얼 매핑은 TrainVisualCatalog(SO)에 저장(소유)
//   - 씬의 TrainSectionVisual로 즉시 미리보기 가능
// EditorWindow이므로 반드시 Editor 폴더에 위치해야 컴파일/빌드제외된다.
public class TrainUpgradeVisualToolWindow : EditorWindow
{
    [SerializeField] private TextAsset csvAsset;
    [SerializeField] private TrainVisualCatalog catalog;

    private UpgradeTable _table;
    private Vector2 _scroll;
    private readonly Dictionary<SectionType, bool> _foldouts = new Dictionary<SectionType, bool>();

    [MenuItem("Tools/Train/Upgrade Visual Tool")]
    private static void Open()
    {
        GetWindow<TrainUpgradeVisualToolWindow>("기차 업그레이드 비주얼");
    }

    private void OnGUI()
    {
        DrawSourcePanel();
        EditorGUILayout.Space();

        if (_table == null)
        {
            EditorGUILayout.HelpBox("CSV와 카탈로그를 지정한 뒤 '불러오기'를 누르세요.", MessageType.Info);
            return;
        }
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("비주얼을 저장할 TrainVisualCatalog를 지정하세요.", MessageType.Warning);
            return;
        }

        DrawBindingPanel();
    }

    // ── 상단: 소스 지정 + 불러오기/생성 헬퍼 ─────────────────────────
    private void DrawSourcePanel()
    {
        EditorGUILayout.LabelField("데이터 소스", EditorStyles.boldLabel);

        csvAsset = (TextAsset)EditorGUILayout.ObjectField(
            "업그레이드 CSV", csvAsset, typeof(TextAsset), false);
        catalog = (TrainVisualCatalog)EditorGUILayout.ObjectField(
            "비주얼 카탈로그", catalog, typeof(TrainVisualCatalog), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(csvAsset == null))
            {
                if (GUILayout.Button("불러오기")) LoadTable();
            }
            if (GUILayout.Button("카탈로그 생성")) CreateCatalogAsset();
        }
    }

    private void LoadTable()
    {
        IUpgradeTableLoader loader = new CsvUpgradeTableLoader();
        _table = loader.Load(csvAsset.text);
        Debug.Log($"[UpgradeVisualTool] {_table.Rows.Count}개 행 로드.");
    }

    // ── 본문: 차량 타입별 레벨 목록 ↔ 비주얼 바인딩 ───────────────────
    private void DrawBindingPanel()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (SectionType type in _table.Sections)
        {
            if (!_foldouts.TryGetValue(type, out bool open)) open = true;
            open = EditorGUILayout.Foldout(open, type.ToString(), true);
            _foldouts[type] = open;
            if (!open) continue;

            EditorGUI.indentLevel++;
            foreach (UpgradeRow row in _table.ForSection(type))
                DrawLevelRow(type, row);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawLevelRow(SectionType type, UpgradeRow row)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Lv {row.Level}", GUILayout.Width(50));

            GameObject current = catalog.Resolve(type, row.Level);
            GameObject next = (GameObject)EditorGUILayout.ObjectField(
                current, typeof(GameObject), false);

            if (next != current)
            {
                Undo.RecordObject(catalog, "Bind Train Visual");
                catalog.SetVisual(type, row.Level, next);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }

            using (new EditorGUI.DisabledScope(current == null))
            {
                if (GUILayout.Button("미리보기", GUILayout.Width(70)))
                    Preview(type, row.Level);
            }
        }

        // 스탯 컬럼 요약(있을 때만).
        if (row.Stats.Count > 0)
        {
            string stats = string.Join("  ", row.Stats.Select(kv => $"{kv.Key}={kv.Value}"));
            EditorGUILayout.LabelField(" ", stats, EditorStyles.miniLabel);
        }
    }

    // 씬에서 해당 타입의 TrainSectionVisual을 찾아 레벨을 적용해 외형을 즉시 확인.
    private void Preview(SectionType type, int level)
    {
        TrainSectionVisual target = FindObjectsOfType<TrainSectionVisual>()
            .FirstOrDefault(v => v.SectionType == type);

        if (target == null)
        {
            Debug.LogWarning($"[UpgradeVisualTool] 씬에 {type} 타입 TrainSectionVisual이 없습니다.");
            return;
        }

        int group = Undo.GetCurrentGroup();
        Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Preview Train Visual");
        target.ApplyLevel(level);
        Undo.CollapseUndoOperations(group);

        EditorUtility.SetDirty(target);
    }

    // 카탈로그 SO를 파일로 생성(없을 때 빠르게 만들기).
    private void CreateCatalogAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "비주얼 카탈로그 생성", "TrainVisualCatalog", "asset",
            "카탈로그 에셋을 저장할 위치를 선택하세요.");
        if (string.IsNullOrEmpty(path)) return;

        var asset = CreateInstance<TrainVisualCatalog>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        catalog = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
