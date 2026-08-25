using System.Collections.Generic;
using UnityEngine;

// 차량 타입 × 업그레이드 레벨 → 비주얼 모델(프리팹/FBX) 매핑을 소유하는 카탈로그.
// CSV는 레벨·스탯만 담고(읽기 전용), 레벨→외형 연결은 이 SO가 유일한 소유자다.
// 개발자 툴(TrainUpgradeVisualToolWindow)이 이 에셋에 바인딩을 저장하고,
// 런타임 TrainSectionVisual이 Resolve로 조회해 외형을 교체한다.
[CreateAssetMenu(fileName = "TrainVisualCatalog", menuName = "Train/Visual Catalog")]
public class TrainVisualCatalog : ScriptableObject
{
    // 특정 레벨에 대응하는 비주얼 프리팹.
    [System.Serializable]
    public class LevelVisual
    {
        public int level = 1;
        public GameObject prefab;
    }

    // 한 차량 타입의 레벨별 비주얼 목록.
    [System.Serializable]
    public class SectionBinding
    {
        public SectionType sectionType;
        public List<LevelVisual> levels = new List<LevelVisual>();
    }

    [SerializeField] private List<SectionBinding> bindings = new List<SectionBinding>();

    // (타입, 레벨) → 프리팹. 미등록이면 null.
    public GameObject Resolve(SectionType type, int level)
    {
        SectionBinding binding = FindBinding(type);
        if (binding == null) return null;

        LevelVisual entry = binding.levels.Find(l => l.level == level);
        return entry != null ? entry.prefab : null;
    }

    // 툴에서 호출: (타입, 레벨)에 프리팹을 배정한다. 없으면 항목을 새로 만든다.
    // prefab이 null이면 해당 레벨 매핑을 비운다(항목 자체는 유지).
    public void SetVisual(SectionType type, int level, GameObject prefab)
    {
        SectionBinding binding = FindBinding(type);
        if (binding == null)
        {
            binding = new SectionBinding { sectionType = type };
            bindings.Add(binding);
        }

        LevelVisual entry = binding.levels.Find(l => l.level == level);
        if (entry == null)
        {
            entry = new LevelVisual { level = level };
            binding.levels.Add(entry);
        }

        entry.prefab = prefab;
    }

    private SectionBinding FindBinding(SectionType type)
        => bindings.Find(b => b.sectionType == type);
}
