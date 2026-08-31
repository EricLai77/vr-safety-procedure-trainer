using System;
using System.IO;
using UnityEngine;

namespace VRTraining
{
    public static class TrainingResultSaver
    {
        public static string Save(SessionResult result)
        {
            if (result == null)
            {
                Debug.LogError(
                    "Cannot save a null SessionResult.");

                return string.Empty;
            }

            try
            {
                var directory = Path.Combine(
                    Application.persistentDataPath,
                    "TrainingResults");

                Directory.CreateDirectory(directory);

                var fileName =
                    $"session_" +
                    $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json";

                var path = Path.Combine(
                    directory,
                    fileName);

                var json = JsonUtility.ToJson(
                    result,
                    true);

                File.WriteAllText(
                    path,
                    json);

                Debug.Log(
                    $"Training result saved to: {path}");

                return path;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Failed to save training result: " +
                    $"{exception.Message}");

                return string.Empty;
            }
        }
    }
}