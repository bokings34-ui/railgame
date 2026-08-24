using UnityEngine;
using UnityEngine.InputSystem;

// 테스트용 냉각 입력 어댑터(저결합). 지정 키를 누르는 동안 물탱크 냉각을 호출한다.
// 오직 WaterTankSection.CoolTick에만 의존 → 입력 방식이 바뀌어도 냉각 로직은 무변경.
public class CoolingInput : MonoBehaviour
{
    [SerializeField] private WaterTankSection waterTank;
    [Tooltip("누르는 동안 냉각할 키.")]
    [SerializeField] private Key coolKey = Key.Space;

    private void Update()
    {
        if (waterTank == null || Keyboard.current == null) return;

        if (Keyboard.current[coolKey].isPressed)
        {
            waterTank.CoolTick(Time.deltaTime);
        }
    }
}
