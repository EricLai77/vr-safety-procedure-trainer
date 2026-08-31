using System;
using UnityEngine;

namespace VRTraining
{
    [Serializable]
    public sealed class TrainingStepDefinition
    {
        public EquipmentId equipment;

        [TextArea]
        public string instruction;
    }

    public sealed class TrainingManager : MonoBehaviour
    {
        [SerializeField]
        private TrainingStepDefinition[] steps;

        [SerializeField]
        private bool startOnPlay = true;

        private TrainingSession session;
        private float startTime;
        private float finalElapsedTime;

        public event Action StateChanged;

        public event Action<string, bool> FeedbackRaised;

        public event Action<SessionResult> SessionCompleted;

        public bool IsRunning { get; private set; }

        public int CurrentStepIndex =>
            session?.CurrentIndex ?? 0;

        public int TotalSteps =>
            steps?.Length ?? 0;

        public int ErrorCount =>
            session?.ErrorCount ?? 0;

        public float ElapsedSeconds
        {
            get
            {
                if (IsRunning)
                {
                    return Time.unscaledTime - startTime;
                }

                return finalElapsedTime;
            }
        }

        public string CurrentInstruction
        {
            get
            {
                if (steps == null || steps.Length == 0)
                {
                    return "No training steps configured.";
                }

                if (session == null)
                {
                    return "Press Start to begin.";
                }

                if (session.IsComplete)
                {
                    return "Training complete.";
                }

                return steps[session.CurrentIndex].instruction;
            }
        }

        private void Start()
        {
            if (startOnPlay)
            {
                BeginTraining();
            }
        }

        public void BeginTraining()
        {
            if (steps == null || steps.Length == 0)
            {
                Debug.LogError(
                    "TrainingManager has no training steps.");

                return;
            }

            var order = new EquipmentId[steps.Length];

            for (var i = 0; i < steps.Length; i++)
            {
                order[i] = steps[i].equipment;
            }

            session = new TrainingSession(order);

            startTime = Time.unscaledTime;
            finalElapsedTime = 0f;
            IsRunning = true;

            RaiseFeedback(
                "Training started.",
                true);

            StateChanged?.Invoke();
        }

        public void Submit(
            EquipmentId itemId,
            EquipmentId socketId)
        {
            if (!IsRunning || session == null)
            {
                Debug.LogWarning(
                    "Training is not currently running.");

                return;
            }

            var result =
                session.Submit(itemId, socketId);

            switch (result)
            {
                case SubmissionStatus.Correct:
                    RaiseFeedback(
                        $"{itemId} inspected correctly.",
                        true);
                    break;

                case SubmissionStatus.WrongSocket:
                    RaiseFeedback(
                        $"{itemId} was placed in the wrong socket.",
                        false);
                    break;

                case SubmissionStatus.WrongOrder:
                    RaiseFeedback(
                        $"Wrong order. Current task: " +
                        $"{session.CurrentExpectedItem}.",
                        false);
                    break;

                case SubmissionStatus.Duplicate:
                    RaiseFeedback(
                        $"{itemId} has already been inspected.",
                        false);
                    break;

                case SubmissionStatus.AlreadyComplete:
                    return;
            }

            StateChanged?.Invoke();

            if (session.IsComplete)
            {
                CompleteTraining();
            }
        }

        private void CompleteTraining()
        {
            IsRunning = false;

            finalElapsedTime =
                Time.unscaledTime - startTime;

            var timePenalty =
                Mathf.FloorToInt(finalElapsedTime / 30f) * 2;

            var score = Mathf.Clamp(
                100 - ErrorCount * 10 - timePenalty,
                0,
                100);

            var result = new SessionResult
            {
                completedAtUtc =
                    DateTime.UtcNow.ToString("O"),

                durationSeconds =
                    finalElapsedTime,

                errorCount =
                    ErrorCount,

                score =
                    score
            };

            TrainingResultSaver.Save(result);

            RaiseFeedback(
                "Training complete.",
                true);

            StateChanged?.Invoke();

            SessionCompleted?.Invoke(result);
        }

        private void RaiseFeedback(
            string message,
            bool success)
        {
            if (success)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(message);
            }

            FeedbackRaised?.Invoke(
                message,
                success);
        }
    }
}