using System;

namespace VRTraining
{
    [Serializable]
    public sealed class SessionResult
    {
        public string completedAtUtc;
        public float durationSeconds;
        public int errorCount;
        public int score;
    }
}