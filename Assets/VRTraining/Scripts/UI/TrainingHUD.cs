using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRTraining
{
    [DisallowMultipleComponent]
    public sealed class TrainingHUD : MonoBehaviour
    {
        [Header("Training Manager")]

        [SerializeField]
        private TrainingManager trainingManager;

        [Header("Training UI")]

        [SerializeField]
        private GameObject trainingBackground;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text instructionText;

        [SerializeField]
        private TMP_Text progressText;

        [SerializeField]
        private TMP_Text timerText;

        [SerializeField]
        private TMP_Text errorText;

        [SerializeField]
        private TMP_Text feedbackText;

        [Header("Results UI")]

        [SerializeField]
        private GameObject resultsPanel;

        [SerializeField]
        private TMP_Text resultText;

        [SerializeField]
        private Button restartButton;

        [Header("View Following")]

        [SerializeField]
        private bool followView = true;

        [SerializeField]
        [Tooltip(
            "Camera-local HUD offset: X moves right, " +
            "Y moves up, and Z changes viewing distance.")]
        private Vector3 viewOffset =
            new Vector3(0f, 0.05f, 1.6f);

        [Header("Feedback Colours")]

        [SerializeField]
        private Color successColour = Color.green;

        [SerializeField]
        private Color failureColour = Color.red;

        [SerializeField]
        [Min(0f)]
        private float feedbackDuration = 2.5f;

        private Canvas worldCanvas;
        private Camera uiCamera;
        private Coroutine feedbackClearRoutine;
        private XRSimpleInteractable restartInteractable;
        private bool isRestarting;

        private void Awake()
        {
            ResolveReferences();
            worldCanvas = GetComponent<Canvas>();
            CacheUiCamera();
            SetTrainingUiActive(true);

            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            // Only actual controls should take part in UI raycasts.
            // Text graphics otherwise become invisible raycast targets.
            foreach (var text in
                     GetComponentsInChildren<TMP_Text>(true))
            {
                text.raycastTarget = false;
            }

            ConfigureRestartXrFallback();
        }

        private void OnEnable()
        {
            if (trainingManager == null)
            {
                return;
            }

            trainingManager.StateChanged += Refresh;

            trainingManager.FeedbackRaised +=
                ShowFeedback;

            trainingManager.SessionCompleted +=
                ShowResults;
        }

        private void OnDisable()
        {
            CancelFeedbackClear();

            if (trainingManager == null)
            {
                return;
            }

            trainingManager.StateChanged -= Refresh;

            trainingManager.FeedbackRaised -=
                ShowFeedback;

            trainingManager.SessionCompleted -=
                ShowResults;
        }

        private void OnDestroy()
        {
            if (restartInteractable != null)
            {
                restartInteractable.selectEntered.RemoveListener(
                    OnRestartSelected);
            }
        }

        private void Start()
        {
            ResolveReferences();

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            Refresh();
            StartCoroutine(RebindSimulatorAfterSceneLoad());
        }

        private IEnumerator RebindSimulatorAfterSceneLoad()
        {
            // The automatically-created simulator is DontDestroyOnLoad.
            // Wait for the new XR Origin to initialize, then replace the
            // camera/controller references left over from the previous scene.
            yield return null;

            var simulator = XRInteractionSimulator.instance;

            if (simulator == null || !simulator.enabled)
            {
                yield break;
            }

            simulator.enabled = false;
            simulator.cameraTransform = null;
            simulator.leftControllerTransform = null;
            simulator.rightControllerTransform = null;
            simulator.leftHandAimTransform = null;
            simulator.rightHandAimTransform = null;
            simulator.targetedDeviceInput =
                TargetedDevices.FPS |
                TargetedDevices.RightDevice;
            simulator.enabled = true;
        }

        private void LateUpdate()
        {
            if (!followView)
            {
                return;
            }

            if (uiCamera == null)
            {
                CacheUiCamera();
            }

            if (uiCamera == null)
            {
                return;
            }

            var cameraTransform = uiCamera.transform;

            transform.SetPositionAndRotation(
                cameraTransform.TransformPoint(viewOffset),
                cameraTransform.rotation);
        }

        private void Update()
        {
            if (!trainingManager.IsRunning)
            {
                return;
            }

            timerText.text =
                $"Time: " +
                $"{trainingManager.ElapsedSeconds:F1}s";
        }

        private void Refresh()
        {
            instructionText.text =
                trainingManager.CurrentInstruction;

            var completedSteps = Mathf.Min(
                trainingManager.CurrentStepIndex,
                trainingManager.TotalSteps);

            progressText.text =
                $"Progress: {completedSteps}/" +
                $"{trainingManager.TotalSteps}";

            errorText.text =
                $"Errors: {trainingManager.ErrorCount}";

            timerText.text =
                $"Time: " +
                $"{trainingManager.ElapsedSeconds:F1}s";
        }

        private void ShowFeedback(
            string message,
            bool success)
        {
            CancelFeedbackClear();
            feedbackText.text = message;

            feedbackText.color =
                success
                    ? successColour
                    : failureColour;

            if (feedbackDuration > 0f)
            {
                feedbackClearRoutine = StartCoroutine(
                    ClearFeedbackAfterDelay());
            }
        }

        private void ShowResults(
            SessionResult result)
        {
            CancelFeedbackClear();
            SetTrainingUiActive(false);
            resultsPanel.SetActive(true);

            resultText.text =
                $"Score: {result.score}\n" +
                $"Time: {result.durationSeconds:F1}s\n" +
                $"Errors: {result.errorCount}";
        }

        private IEnumerator ClearFeedbackAfterDelay()
        {
            yield return new WaitForSeconds(feedbackDuration);

            feedbackText.text = string.Empty;
            feedbackClearRoutine = null;
        }

        private void CancelFeedbackClear()
        {
            if (feedbackClearRoutine == null)
            {
                return;
            }

            StopCoroutine(feedbackClearRoutine);
            feedbackClearRoutine = null;
        }

        private void CacheUiCamera()
        {
            if (worldCanvas != null &&
                worldCanvas.worldCamera != null)
            {
                uiCamera = worldCanvas.worldCamera;
                return;
            }

            uiCamera = Camera.main;
        }

        private void SetTrainingUiActive(bool isActive)
        {
            if (trainingBackground != null)
            {
                trainingBackground.SetActive(isActive);
            }

            SetTextActive(titleText, isActive);
            SetTextActive(instructionText, isActive);
            SetTextActive(progressText, isActive);
            SetTextActive(timerText, isActive);
            SetTextActive(errorText, isActive);
            SetTextActive(feedbackText, isActive);
        }

        private static void SetTextActive(
            TMP_Text text,
            bool isActive)
        {
            if (text != null)
            {
                text.gameObject.SetActive(isActive);
            }
        }

        private void ConfigureRestartXrFallback()
        {
            if (restartButton == null)
            {
                return;
            }

            var buttonObject = restartButton.gameObject;
            var hitbox = buttonObject.GetComponent<BoxCollider>();

            if (hitbox == null)
            {
                hitbox = buttonObject.AddComponent<BoxCollider>();
            }

            var buttonRect = restartButton.transform as RectTransform;

            if (buttonRect != null)
            {
                hitbox.center = Vector3.zero;
                hitbox.size = new Vector3(
                    buttonRect.rect.width,
                    buttonRect.rect.height,
                    10f);
            }

            hitbox.isTrigger = false;

            restartInteractable =
                buttonObject.GetComponent<XRSimpleInteractable>();

            if (restartInteractable == null)
            {
                restartInteractable =
                    buttonObject.AddComponent<XRSimpleInteractable>();
            }

            restartInteractable.colliders.Clear();
            restartInteractable.colliders.Add(hitbox);
            restartInteractable.selectEntered.AddListener(
                OnRestartSelected);
        }

        private void OnRestartSelected(
            SelectEnterEventArgs eventArgs)
        {
            RestartScene();
        }

        public void RestartScene()
        {
            if (isRestarting)
            {
                return;
            }

            isRestarting = true;

            var currentScene =
                SceneManager.GetActiveScene();

            Debug.LogWarning(
                    $"{currentScene.buildIndex} scene is restarting.");
            SceneManager.LoadScene(
                currentScene.buildIndex);
        }

        private void ResolveReferences()
        {
            if (trainingManager == null)
            {
                trainingManager =
                    FindAnyObjectByType<TrainingManager>();
            }

            if (trainingBackground == null)
            {
                var backgroundTransform = transform.Find("Background");

                if (backgroundTransform != null)
                {
                    trainingBackground =
                        backgroundTransform.gameObject;
                }
            }

            if (titleText == null)
            {
                titleText = FindChildComponent<TMP_Text>(
                    "TitleText");
            }

            if (instructionText == null)
            {
                instructionText = FindChildComponent<TMP_Text>(
                    "InstructionText");
            }

            if (progressText == null)
            {
                progressText = FindChildComponent<TMP_Text>(
                    "ProgressText");
            }

            if (timerText == null)
            {
                timerText = FindChildComponent<TMP_Text>(
                    "TimerText");
            }

            if (errorText == null)
            {
                errorText = FindChildComponent<TMP_Text>(
                    "ErrorText");
            }

            if (feedbackText == null)
            {
                feedbackText = FindChildComponent<TMP_Text>(
                    "FeedbackText");
            }

            if (resultsPanel == null)
            {
                var panelTransform = transform.Find("ResultPanel");

                if (panelTransform != null)
                {
                    resultsPanel = panelTransform.gameObject;
                }
            }

            if (resultText == null)
            {
                resultText = FindChildComponent<TMP_Text>(
                    "ResultText");
            }

            if (restartButton == null)
            {
                restartButton = FindChildComponent<Button>(
                    "RestartButton");
            }
        }

        private T FindChildComponent<T>(string objectName)
            where T : Component
        {
            foreach (var component in
                     GetComponentsInChildren<T>(true))
            {
                if (component.gameObject.name == objectName)
                {
                    return component;
                }
            }

            return null;
        }

        private bool ValidateReferences()
        {
            var isValid =
                trainingManager != null &&
                trainingBackground != null &&
                titleText != null &&
                instructionText != null &&
                progressText != null &&
                timerText != null &&
                errorText != null &&
                feedbackText != null &&
                resultsPanel != null &&
                resultText != null &&
                restartButton != null;

            if (!isValid)
            {
                var missing = string.Empty;
                AppendMissingReference(
                    ref missing,
                    trainingManager,
                    nameof(trainingManager));
                AppendMissingReference(
                    ref missing,
                    trainingBackground,
                    nameof(trainingBackground));
                AppendMissingReference(
                    ref missing,
                    titleText,
                    nameof(titleText));
                AppendMissingReference(
                    ref missing,
                    instructionText,
                    nameof(instructionText));
                AppendMissingReference(
                    ref missing,
                    progressText,
                    nameof(progressText));
                AppendMissingReference(
                    ref missing,
                    timerText,
                    nameof(timerText));
                AppendMissingReference(
                    ref missing,
                    errorText,
                    nameof(errorText));
                AppendMissingReference(
                    ref missing,
                    feedbackText,
                    nameof(feedbackText));
                AppendMissingReference(
                    ref missing,
                    resultsPanel,
                    nameof(resultsPanel));
                AppendMissingReference(
                    ref missing,
                    resultText,
                    nameof(resultText));
                AppendMissingReference(
                    ref missing,
                    restartButton,
                    nameof(restartButton));

                Debug.LogError(
                    "TrainingHUD could not resolve: " +
                    missing + ".",
                    this);
            }

            return isValid;
        }

        private static void AppendMissingReference(
            ref string missing,
            Object reference,
            string referenceName)
        {
            if (reference != null)
            {
                return;
            }

            if (missing.Length > 0)
            {
                missing += ", ";
            }

            missing += referenceName;
        }
    }
}
