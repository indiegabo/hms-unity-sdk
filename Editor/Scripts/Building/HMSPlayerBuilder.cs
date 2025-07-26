using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HMSUnitySDK.Editor
{
    public static class HMSPlayerBuilder
    {
        private static HashSet<IHMSBuildOperation> _buildOperations = new();
        private static bool _initialized = false;

        public enum VersionUpdateDefinition
        {
            Patch,
            Minor,
            Major,
        }

        public struct HMSBuildOptions
        {
            public bool shoulUpdateVersion;
            public VersionUpdateDefinition versionUpdateDefinition;
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_initialized) return;

            // Registrar callbacks do Unity
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            _initialized = true;
        }

        public static void RegisterBuildOperation(IHMSBuildOperation operation)
        {
            if (!_buildOperations.Contains(operation))
            {
                _buildOperations.Add(operation);
            }
        }

        public static void UnregisterPreBuildOperation(IHMSBuildOperation operation)
        {
            _buildOperations.Remove(operation);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Limpar ou revalidar operações quando o modo de jogo muda
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                ValidateOperations();
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            // Nada a fazer aqui, as listas serão mantidas
        }

        private static void OnAfterAssemblyReload()
        {
            // Revalidar operações após recompilação
            ValidateOperations();
        }

        private static void ValidateOperations()
        {
            // Remover operações nulas ou inválidas
            _buildOperations = _buildOperations
                .Where(op => op != null)
                .Distinct()
                .ToHashSet();
        }

        public static async Awaitable BuildUsingProfile(
            BuildProfile profile,
            string basePath,
            BuildOptions buildOptions = BuildOptions.None,
            HMSBuildOptions hmsOptions = default
        )
        {
            if (profile == null)
            {
                Debug.LogError("Build profile is null!");
                return;
            }

            if (hmsOptions.shoulUpdateVersion)
            {
                string newVersion = UpdateVersion(Application.version, hmsOptions.versionUpdateDefinition);
                if (newVersion != Application.version)
                {
                    Debug.Log($"Updating version from {Application.version} to {newVersion}");
                    PlayerSettings.bundleVersion = newVersion;
                    AssetDatabase.SaveAssets();
                }
            }

            BuildProfile.SetActiveBuildProfile(profile);

            var preBuildOperations = _buildOperations.Select(op => op.Prebuild()).ToArray();
            await Task.WhenAll(preBuildOperations);

            string completePath = ResolveBuildPath(basePath, profile);
            var options = new BuildPlayerWithProfileOptions()
            {
                locationPathName = completePath,
                options = buildOptions,
                buildProfile = profile
            };

            var report = BuildPipeline.BuildPlayer(options);
            var outputPath = report.summary.outputPath;

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Build failed: {outputPath}");
                return;
            }

            var metadata = new HMSBuildMetadata()
            {
                ProductName = Application.productName,
                Version = Application.version,
                ExecutableName = Path.GetFileName(completePath),
                Path = Path.GetDirectoryName(outputPath),
                Size = report.summary.totalSize,
                BuiltAt = DateTime.Now,
                Platform = report.summary.platform,
            };

            EditorApplication.LockReloadAssemblies();
            try
            {                // Executa as operações de pós-build SINCRONAMENTE
                foreach (var op in _buildOperations)
                {
                    await RunTaskSynchronously(() => op.Postbuild(metadata), op.Name);
                }

                Debug.Log($"Build complete: {outputPath}");
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies(); // Libera novamente
            }
        }

        public static async Awaitable BuildCleanUsingProfile(BuildProfile profile, string path, HMSBuildOptions hmsOptions = default)
        {
            await BuildUsingProfile(
                profile,
                path,
                BuildOptions.CleanBuildCache | BuildOptions.StrictMode,
                hmsOptions
            );
        }

        private static string SanitizeFileName(string name)
        {
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(name.Split(invalidChars));
        }

        private static string SanitizeVersion(string version)
        {
            // Replace any dots that might cause issues with underscores
            return version.Replace('.', '_');
        }

        private static string UpdateVersion(string currentVersion, VersionUpdateDefinition updateType)
        {
            if (string.IsNullOrEmpty(currentVersion))
            {
                return "1.0.0";
            }

            try
            {
                var parts = currentVersion.Split('.');
                int major = parts.Length > 0 ? int.Parse(parts[0]) : 0;
                int minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
                int patch = parts.Length > 2 ? int.Parse(parts[2]) : 0;

                switch (updateType)
                {
                    case VersionUpdateDefinition.Major:
                        major++;
                        minor = 0;
                        patch = 0;
                        break;
                    case VersionUpdateDefinition.Minor:
                        minor++;
                        patch = 0;
                        break;
                    case VersionUpdateDefinition.Patch:
                        patch++;
                        break;
                    default:
                        return currentVersion;
                }

                return $"{major}.{minor}.{patch}";
            }
            catch
            {
                Debug.LogError($"Invalid version format: {currentVersion}. Using default 1.0.0");
                return "1.0.0";
            }
        }

        private static string ResolveBuildPath(string basePath, BuildProfile unityBuildProfile)
        {
            // Get product name and version from PlayerSettings
            string productName = Application.productName;
            string version = Application.version;
            BuildTarget target = unityBuildProfile.GetBuildTargetInternal();

            // Sanitize names
            productName = SanitizeFileName(productName);
            string versionSuffix = $"v{SanitizeVersion(version)}";
            string platform = GetPlatformName(target);

            // Create versioned folder name (this will be our root output folder)
            string folderName = $"{productName}.{versionSuffix}.{platform}";

            // Full output path
            string outputPath = Path.Combine(basePath, folderName);

            // Create directory if it doesn't exist
            Directory.CreateDirectory(outputPath);

            var buildTarget = unityBuildProfile.GetBuildTargetInternal();
            string path = buildTarget switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 =>
                    Path.Combine(outputPath, folderName + ".exe"),
                BuildTarget.StandaloneLinux64 =>
                    Path.Combine(outputPath, folderName + ".x86_64"),
                _ => Path.Combine(outputPath, folderName + ".exe")
            };

            return path;
        }

        private static async Task RunTaskSynchronously(Func<Task> taskFunc, string operationName = "Build Operation")
        {
            try
            {
                var task = taskFunc.Invoke();
                float progress = 0f;

                while (!task.IsCompleted)
                {
                    progress = Mathf.Repeat(progress + 0.01f, 0.95f); // Indeterminate progress
                    bool cancelRequested = EditorUtility.DisplayCancelableProgressBar(
                        "Processing Post-Build Operations",
                        $"{operationName}...",
                        progress
                    );

                    if (cancelRequested)
                    {
                        Debug.LogWarning("Operation was cancelled by the user!");
                        throw new OperationCanceledException();
                    }

                    await Task.Yield();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static string GetPlatformName(BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => "windows",
                BuildTarget.StandaloneLinux64 => "linux",
                BuildTarget.StandaloneOSX => "macos",
                _ => "windows"
            };
        }
    }
}