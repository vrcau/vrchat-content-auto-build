using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;

namespace VRChatAerospaceUniversity.VRChatAutoBuild {
    public abstract class AutoBuildBase {
    #region Arguments

        [PublicAPI]
        public static AutoBuildArguments GetArguments() {
            var commandLineArgs = Environment.GetCommandLineArgs().ToList();

            var contentId = GetArgument(commandLineArgs, "--vrchat-auto-build-content-id", "VRC_AUTO_BUILD_CONTENT_ID");
            var scenePath = GetArgument(commandLineArgs, "--vrchat-auto-build-scene-path", "VRC_AUTO_BUILD_SCENE_PATH");

            if (scenePath == null) {
                throw new ArgumentNullException(nameof(AutoBuildArguments.ScenePath), "Scene path is required");
            }

            return new AutoBuildArguments {
                ContentId = contentId,
                ScenePath = scenePath
            };
        }

        [CanBeNull]
        private static string GetArgument(List<string> commandLineArgs, string commandLineArg,
            string environmentVariable) {
            var index = commandLineArgs.FindLastIndex(arg => arg == commandLineArg);

            var arg = index != -1 && commandLineArgs.Count > index + 1
                ? commandLineArgs[index + 1]
                : Environment.GetEnvironmentVariable(environmentVariable);

            return arg;
        }

    #endregion

        [PublicAPI]
        public static async Task<AutoBuildArguments> InitAutoBuildAsync() {
            var args = GetArguments();

            Debug.Log("Opening scene");
            EditorSceneManager.OpenScene(args.ScenePath, OpenSceneMode.Single);
            Debug.Log("Scene opened");

            try {
                await InitSDKOnlineModeAsync();
            }
            catch (Exception e) {
                throw new Exception("Failed to initialize SDK Online Mode", e);
            }

            try {
                await InitSDKBuildersAsync();
            }
            catch (Exception e) {
                throw new Exception("Failed to initialize SDK Builders", e);
            }

            return args;
        }

        [PublicAPI]
        public static void ExitWithException(Exception e) {
            Debug.LogError("Failed to preform auto build:\n" + e);
            Debug.LogException(e);

            EditorApplication.Exit(1);
        }

        private static async Task InitSDKBuildersAsync() {
            var vrcSdkControlPanel = EditorWindow.GetWindow<VRCSdkControlPanel>();
            var showBuildersMethod =
                typeof(VRCSdkControlPanel).GetMethod("ShowBuilders", BindingFlags.Instance | BindingFlags.NonPublic);

            if (showBuildersMethod == null) {
                throw new Exception("Failed to get ShowBuilders method");
            }

            vrcSdkControlPanel.Show(true);
            showBuildersMethod.Invoke(vrcSdkControlPanel, null);

            // Because VRChat SDK initialize builder in ui schedule, we have to wait
            await Task.Delay(1000);
        }

        private static async Task InitSDKOnlineModeAsync() {
            API.SetOnlineMode(true);
            ApiCredentials.Load();

            var tcs = new TaskCompletionSource<bool>();

            ConfigManager.RemoteConfig.Init(() => {
                APIUser.InitialFetchCurrentUser(userModel => {
                    Debug.Log($"Logged in as {userModel.Model.id}");

                    tcs.SetResult(true);
                }, model => {
                    Debug.LogError("Failed to initialize SDK: Failed to fetch current user: " + model.Error);
                    tcs.SetException(new Exception("Failed to fetch current user"));
                });
            }, () => {
                Debug.LogError("Failed to initialize SDK: Failed to init remote config");
                tcs.SetException(new Exception("Failed to init remote config"));
            });

            await tcs.Task;
        }
    }
}
