using System.Collections;
using System.IO;
using System;
using UnityEngine;

namespace Railgame.Hansol.ShoulderView
{
    public sealed class ShoulderViewEvidenceCapture : MonoBehaviour
    {
        [SerializeField] private bool captureOnStart = true;
        [SerializeField] private bool runInteractionScenario;
        [SerializeField] private bool quitAfterCapture;
        [SerializeField] private string outputDirectory;
        [SerializeField] private string fileName = "ShoulderView_World_Evidence.png";
        [SerializeField] private int captureWidth = 1920;
        [SerializeField] private int captureHeight = 1080;
        [SerializeField] private ShoulderInteractor interactor;
        [SerializeField] private ShoulderShopPanel shopPanel;

        public string LastCapturePath { get; private set; }

        private IEnumerator Start()
        {
            if (!captureOnStart)
                yield break;

            ApplyRequestedResolution();
            for (int frame = 0; frame < 5; frame++)
                yield return null;
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
            Capture(SeasonFileName(fileName));
            yield return new WaitForSeconds(0.75f);

            if (runInteractionScenario && interactor != null)
            {
                interactor.ScanForTarget();
                bool opened = interactor.TryInteract();
                Debug.Log($"SHOULDER_VIEW_EVIDENCE_INTERACTION opened={opened}");
                yield return null;
                yield return new WaitForEndOfFrame();
                Capture(SeasonFileName("ShoulderView_Shop_Open_Evidence.png"));
                yield return new WaitForSeconds(0.75f);

                bool purchased = shopPanel != null && shopPanel.TryPurchase(0);
                Debug.Log($"SHOULDER_VIEW_EVIDENCE_PURCHASE purchased={purchased}");
                yield return null;
                yield return new WaitForEndOfFrame();
                Capture(SeasonFileName("ShoulderView_Shop_Purchased_Evidence.png"));
                yield return new WaitForSeconds(1f);
            }

            if (quitAfterCapture)
                Application.Quit();
        }

        public void Capture()
        {
            Capture(fileName);
        }

        public void Initialize(ShoulderInteractor targetInteractor, ShoulderShopPanel panel, string directory,
            bool runScenario, bool quitWhenDone)
        {
            interactor = targetInteractor;
            shopPanel = panel;
            outputDirectory = directory;
            runInteractionScenario = runScenario;
            quitAfterCapture = quitWhenDone;
        }

        private void Capture(string targetFileName)
        {
            string logsDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"))
                : Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(logsDirectory);
            LastCapturePath = Path.Combine(logsDirectory, targetFileName);
            ScreenCapture.CaptureScreenshot(LastCapturePath, 1);
            Debug.Log($"SHOULDER_VIEW_EVIDENCE_CAPTURED path={LastCapturePath}");
        }

        private void ApplyRequestedResolution()
        {
            int width = ReadIntArgument("-evidence-width", captureWidth);
            int height = ReadIntArgument("-evidence-height", captureHeight);
            width = Mathf.Max(640, width);
            height = Mathf.Max(360, height);
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            Debug.Log($"SHOULDER_VIEW_EVIDENCE_RESOLUTION requested={width}x{height}");
        }

        private static int ReadIntArgument(string key, int fallback)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
                if (string.Equals(arguments[index], key, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(arguments[index + 1], out int value))
                    return value;
            return fallback;
        }

        private static string SeasonFileName(string original)
        {
            string label = ShoulderSeasonPreview.ReadSeasonLabel();
            return $"{Path.GetFileNameWithoutExtension(original)}_{label}{Path.GetExtension(original)}";
        }
    }
}
