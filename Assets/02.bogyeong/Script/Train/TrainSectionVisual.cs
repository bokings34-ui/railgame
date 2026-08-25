using UnityEngine;

// 차량 1칸의 "현재 외형"을 담당하는 런타임 컴포넌트.
// 업그레이드 레벨에 대응하는 프리팹을 카탈로그에서 찾아 visualRoot 아래에 1개만 인스턴스화한다.
// (RailBlock의 SetActive 토글 정신을 잇되, N개 상주 대신 인스턴스 교체 방식으로 확장.)
// 에디트 모드: 개발자 툴의 미리보기가 호출 → 프리팹 연결 유지 + Undo 지원.
// 플레이 모드: 업그레이드 시스템이 ApplyLevel을 호출해 외형을 바꾼다.
// (같은 오브젝트의 TrainSection에서 차량 타입을 읽는다. TrainSection은 추상이라
//  RequireComponent는 걸지 않고, 없으면 SectionType.None으로 안전 처리한다.)
public class TrainSectionVisual : MonoBehaviour
{
    [Tooltip("비주얼 인스턴스가 놓일 부모. 비우면 이 오브젝트의 Transform을 사용.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("레벨→프리팹 매핑을 소유하는 카탈로그 에셋.")]
    [SerializeField] private TrainVisualCatalog catalog;

    [Tooltip("현재 적용된 업그레이드 레벨.")]
    [SerializeField] private int currentLevel = 1;

    // 현재 상주 중인 비주얼 인스턴스(교체 시 이것만 제거해 다른 자식은 건드리지 않는다).
    [SerializeField] private GameObject currentInstance;

    public int CurrentLevel => currentLevel;

    // 짝을 이루는 TrainSection에서 차량 타입을 읽는다.
    public SectionType SectionType
    {
        get
        {
            TrainSection section = GetComponent<TrainSection>();
            return section != null ? section.SectionType : SectionType.None;
        }
    }

    // 지정 레벨의 비주얼로 교체한다. 카탈로그에 매핑이 없으면 인스턴스를 비운다.
    public void ApplyLevel(int level)
    {
        currentLevel = level;
        Transform root = visualRoot != null ? visualRoot : transform;
        GameObject prefab = catalog != null ? catalog.Resolve(SectionType, level) : null;

        DestroyCurrent();
        if (prefab == null) return;

        currentInstance = InstantiateVisual(prefab, root);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.name = prefab.name;
    }

    private void DestroyCurrent()
    {
        if (currentInstance == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.DestroyObjectImmediate(currentInstance);
            currentInstance = null;
            return;
        }
#endif
        Destroy(currentInstance);
        currentInstance = null;
    }

    private GameObject InstantiateVisual(GameObject prefab, Transform root)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 에디트 모드: 프리팹 연결을 유지해 씬에 저장 + Undo 등록.
            var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root);
            UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Apply Train Visual");
            return instance;
        }
#endif
        return Instantiate(prefab, root);
    }
}
