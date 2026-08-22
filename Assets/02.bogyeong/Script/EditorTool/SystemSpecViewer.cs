using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// CPU / GPU / RAM / VRAM 사양을 확인하는 스크립트.
/// - F1 키(또는 인스펙터에서 지정한 키)를 누르면 화면에 사양 창(UI)이 뜨고, 버튼으로 조작 가능.
/// - "콘솔에 출력" 버튼을 누르면 Debug.Log로 한눈에 출력됨.
/// - 빈 GameObject에 이 스크립트를 붙이기만 하면 바로 동작함 (Canvas/UI 프리팹 불필요, OnGUI 사용).
/// </summary>
public class SystemSpecViewer : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이 키를 누르면 사양 창을 열고 닫습니다.")]
    public KeyCode toggleKey = KeyCode.F1;

    [Tooltip("시작할 때 자동으로 창을 표시할지 여부")]
    public bool showOnStart = true;

    [Tooltip("매 프레임 정보를 갱신할지 여부 (RAM 사용량 등은 실시간으로 변함)")]
    public bool autoRefresh = true;

    // GUI.Window에 쓸 고정 ID (GetInstanceID()는 값이 겹치거나 음수/런타임에 따라
    // window가 제대로 안 뜨는 경우가 있어서, 이 스크립트 전용 고정 값으로 대체)
    private const int WindowId = 928374;

    private bool isVisible;
    private Rect windowRect = new Rect(20, 20, 480, 420);
    private Vector2 scrollPos;
    private string cachedInfo = "";
    private string lastClipboardMessage = "";
    private float lastClipboardMessageTime;

    private void Start()
    {
        isVisible = showOnStart;
        RefreshInfo();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }

        if (isVisible && autoRefresh)
        {
            RefreshInfo();
        }
    }

    private void OnGUI()
    {
        if (!isVisible) return;

        windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "시스템 사양 (CPU / GPU / RAM / VRAM)");
    }

    private void DrawWindow(int id)
    {
        // 상단 버튼 영역
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("새로고침", GUILayout.Height(28)))
        {
            RefreshInfo();
        }
        if (GUILayout.Button("콘솔에 출력", GUILayout.Height(28)))
        {
            RefreshInfo();
            Debug.Log(cachedInfo);
        }
        if (GUILayout.Button("클립보드 복사", GUILayout.Height(28)))
        {
            RefreshInfo();
            GUIUtility.systemCopyBuffer = cachedInfo;
            lastClipboardMessage = "클립보드에 복사되었습니다.";
            lastClipboardMessageTime = Time.realtimeSinceStartup;
        }
        if (GUILayout.Button("닫기", GUILayout.Height(28), GUILayout.Width(60)))
        {
            isVisible = false;
        }
        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(lastClipboardMessage) && Time.realtimeSinceStartup - lastClipboardMessageTime < 2f)
        {
            GUILayout.Label(lastClipboardMessage);
        }

        GUILayout.Space(6);

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.TextArea(cachedInfo, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    /// <summary>
    /// SystemInfo / Profiler API로 사양 정보를 문자열로 갱신
    /// </summary>
    private void RefreshInfo()
    {
        var sb = new StringBuilder();

        // ---- CPU ----
        sb.AppendLine("=== CPU ===");
        sb.AppendLine($"모델명       : {SystemInfo.processorType}");
        sb.AppendLine($"코어 수      : {SystemInfo.processorCount}");
        sb.AppendLine($"클럭 속도    : {SystemInfo.processorFrequency} MHz");
        sb.AppendLine();

        // ---- GPU ----
        sb.AppendLine("=== GPU ===");
        sb.AppendLine($"모델명       : {SystemInfo.graphicsDeviceName}");
        sb.AppendLine($"제조사       : {SystemInfo.graphicsDeviceVendor}");
        sb.AppendLine($"그래픽 API   : {SystemInfo.graphicsDeviceType} (Shader Level {SystemInfo.graphicsShaderLevel})");
        sb.AppendLine($"드라이버 버전: {SystemInfo.graphicsDeviceVersion}");
        sb.AppendLine($"멀티스레드 렌더링 : {SystemInfo.graphicsMultiThreaded}");
        sb.AppendLine();

        // ---- RAM ----
        sb.AppendLine("=== RAM (시스템 메모리) ===");
        sb.AppendLine($"총 시스템 메모리 : {SystemInfo.systemMemorySize} MB ({SystemInfo.systemMemorySize / 1024f:F2} GB)");
        // 앱이 실제 사용 중인 메모리 (Mono/IL2CPP 힙 기준, 대략적인 값)
        long totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
        long totalReservedMemory = Profiler.GetTotalReservedMemoryLong();
        long monoHeap = Profiler.GetMonoHeapSizeLong();
        long monoUsed = Profiler.GetMonoUsedSizeLong();
        sb.AppendLine($"앱 할당 메모리    : {BytesToMB(totalAllocatedMemory):F2} MB");
        sb.AppendLine($"앱 예약 메모리    : {BytesToMB(totalReservedMemory):F2} MB");
        sb.AppendLine($"Mono 힙 / 사용량  : {BytesToMB(monoHeap):F2} MB / {BytesToMB(monoUsed):F2} MB");
        sb.AppendLine();

        // ---- VRAM ----
        sb.AppendLine("=== VRAM (그래픽 메모리) ===");
        sb.AppendLine($"총 VRAM      : {SystemInfo.graphicsMemorySize} MB ({SystemInfo.graphicsMemorySize / 1024f:F2} GB)");
        sb.AppendLine();

        // ---- 기타 ----
        sb.AppendLine("=== 기타 ===");
        sb.AppendLine($"운영체제     : {SystemInfo.operatingSystem}");
        sb.AppendLine($"기기 모델    : {SystemInfo.deviceModel}");
        sb.AppendLine($"기기 타입    : {SystemInfo.deviceType}");
        sb.AppendLine($"배터리 상태  : {SystemInfo.batteryStatus} ({SystemInfo.batteryLevel * 100:F0}%)");

        cachedInfo = sb.ToString();
    }

    private float BytesToMB(long bytes)
    {
        return bytes / 1024f / 1024f;
    }

    /// <summary>
    /// 인스펙터의 컨텍스트 메뉴(⋮ 또는 우클릭)에서 바로 콘솔 출력 테스트 가능
    /// </summary>
    [ContextMenu("콘솔에 사양 출력")]
    private void LogToConsole()
    {
        RefreshInfo();
        Debug.Log(cachedInfo);
    }
}
