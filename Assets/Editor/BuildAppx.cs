using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MRReP.Editor
{
    public static class BuildAppx
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";
        private const string BuildFolder = "Build/HoloLens";

        [MenuItem("Build/Build HoloLens APPX")]
        public static void Build()
        {
            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                target = BuildTarget.WSAPlayer,
                targetGroup = BuildTargetGroup.WSA,
                locationPathName = BuildFolder,
                scenes = new[] { ScenePath },
                options = BuildOptions.None
            };

            ConfigureWSASettings();

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildAppx] Build succeeded: {report.summary.totalSize} bytes");
                Debug.Log($"[BuildAppx] Output: {System.IO.Path.GetFullPath(BuildFolder)}");
            }
            else
            {
                Debug.LogError($"[BuildAppx] Build failed: {report.summary.result}");
                if (report.steps != null)
                {
                    foreach (var step in report.steps)
                    {
                        foreach (var msg in step.messages)
                        {
                            if (msg.type == LogType.Error)
                                Debug.LogError($"  {msg.content}");
                        }
                    }
                }
                EditorApplication.Exit(1);
            }
        }

        private static void ConfigureWSASettings()
        {
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClient, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClientServer, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.PrivateNetworkClientServer, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.SpatialPerception, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.Microphone, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.GazeInput, true);

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.WSA, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WSA, ApiCompatibilityLevel.NET_Standard_2_0);

            PlayerSettings.WSA.packageName = "com.mrrep.navigation";
            PlayerSettings.companyName = "MRReP";
            PlayerSettings.productName = "MRReP-Navigation";

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.stripEngineCode = true;

            Debug.Log("[BuildAppx] WSA settings configured for HoloLens 2");
            Debug.Log("[BuildAppx] NOTE: Set Architecture=ARM64 and BuildType=IL2CPP in Build Settings manually before building.");
        }

        [MenuItem("Build/Switch to UWP")]
        public static void SwitchToUWP()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);
            Debug.Log("[BuildAppx] Switched to UWP build target");
        }
    }
}
