using System.Collections;
using System.IO;
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
        [SerializeField] private ShoulderInteractor interactor;
        [SerializeField] private ShoulderShopPanel shopPanel;

        public string LastCapturePath { get; private set; }

        private IEnumerator Start()
        {
            if (!captureOnStart)
                yield break;

            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
            Capture(fileName);
            yield return new WaitForSeconds(0.75f);

            if (runInteractionScenario && interactor != null)
            {
                interactor.ScanForTarget();
                bool opened = interactor.TryInteract();
                Debug.Log($"SHOULDER_VIEW_EVIDENCE_INTERACTION opened={opened}");
                yield return null;
                yield return new WaitForEndOfFrame();
                Capture("ShoulderView_Shop_Open_Evidence.png");
                yield return new WaitForSeconds(0.75f);

                bool purchased = shopPanel != null && shopPanel.TryPurchase(0);
                Debug.Log($"SHOULDER_VIEW_EVIDENCE_PURCHASE purchased={purchased}");
                yield return null;
                yield return new WaitForEndOfFrame();
                Capture("ShoulderView_Shop_Purchased_Evidence.png");
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
    }
}
