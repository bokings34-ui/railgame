using UnityEngine;
using UnityEngine.InputSystem;

// 테스트용 마우스 레일 배치기(저결합 입력 어댑터).
// 오직 RailPath의 공개 API(TryPlaceAt/TryRemoveAt/WorldToCell/RailHeight)에만 의존하며,
// 배치 규칙·타입 결정·잠금·경로 재구성은 전혀 알지 못한다(모두 RailPath 소유).
// 타입(직선/곡선)은 연결로 자동 결정되므로 이 어댑터는 셀만 지정한다.
// 추후 마우스 대신 UI·터치·네트워크 등 다른 입력 소스로 교체해도 RailPath는 무변경.
public class RailPlacerInput : MonoBehaviour
{
    [SerializeField] private RailPath railPath;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayDistance = 500f;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (railPath == null || cam == null) return;
        if (Mouse.current == null) return; // 마우스 미연결(터치 전용 등) 환경 보호

        if (Mouse.current.leftButton.wasPressedThisFrame && TryGetCell(out Vector2Int placeCell))
        {
            railPath.TryPlaceAt(placeCell); // 좌클릭: 배치(타입은 연결로 자동 결정)
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame && TryGetCell(out Vector2Int removeCell))
        {
            railPath.TryRemoveAt(removeCell); // 우클릭: 제거(잠긴 블럭은 거부)
        }
    }

    // 마우스 → 월드 좌표 → 그리드 셀. 지면 콜라이더가 없으면 배치 높이 평면과 교차.
    private bool TryGetCell(out Vector2Int cell)
    {
        cell = default;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundMask))
        {
            cell = railPath.WorldToCell(hit.point);
            return true;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, railPath.RailHeight, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            cell = railPath.WorldToCell(ray.GetPoint(enter));
            return true;
        }
        return false;
    }
}
