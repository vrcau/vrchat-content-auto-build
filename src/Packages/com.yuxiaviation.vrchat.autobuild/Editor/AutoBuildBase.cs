using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;

namespace VRChatAerospaceUniversity.VRChatAutoBuild
{
    public abstract class AutoBuildBase
    {
        #region Arguments

        [PublicAPI]
        public static AutoBuildArguments GetArguments()
        {
            var commandLineArgs = Environment.GetCommandLineArgs().ToList();

            var contentId = GetArgument(commandLineArgs, "--vrchat-auto-build-content-id", "VRC_AUTO_BUILD_CONTENT_ID");
            var scenePath = GetArgument(commandLineArgs, "--vrchat-auto-build-scene-path", "VRC_AUTO_BUILD_SCENE_PATH");
            var username = GetArgument(commandLineArgs, "--vrchat-auto-build-username", "VRC_AUTO_BUILD_USERNAME");
            var password = GetArgument(commandLineArgs, "--vrchat-auto-build-password", "VRC_AUTO_BUILD_PASSWORD");
            var totpKey = GetArgument(commandLineArgs, "--vrchat-auto-build-totp-key", "VRC_AUTO_BUILD_TOTP_KEY");

            if (scenePath == null)
            {
                throw new ArgumentNullException(nameof(AutoBuildArguments.ScenePath), "Scene path is required");
            }

            if (username == null)
            {
                throw new ArgumentNullException(nameof(AutoBuildArguments.Username), "Username is required");
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(AutoBuildArguments.Password), "Password is required");
            }

            if (totpKey == null)
            {
                throw new ArgumentNullException(nameof(AutoBuildArguments.TotpKey), "TOTP key is required");
            }

            return new AutoBuildArguments
            {
                ContentId = contentId,
                ScenePath = scenePath,
                Username = username,
                Password = password,
                TotpKey = totpKey
            };
        }

        [CanBeNull]
        private static string GetArgument(List<string> commandLineArgs, string commandLineArg,
            string environmentVariable)
        {
            var index = commandLineArgs.FindLastIndex(arg => arg == commandLineArg);

            var arg = index != -1 && commandLineArgs.Count > index + 1
                ? commandLineArgs[index + 1]
                : Environment.GetEnvironmentVariable(environmentVariable);

            return arg;
        }

        #endregion

        [PublicAPI]
        public static async Task<AutoBuildArguments> InitAutoBuildAsync()
        {
            var args = GetArguments();

            var customCachePath = Path.GetFullPath("AutoBuildCache");
            Debug.Log("Custom cache path: " + customCachePath);
            Caching.currentCacheForWriting = Caching.AddCache(customCachePath);

            Debug.Log("Opening scene");
            EditorSceneManager.OpenScene(args.ScenePath, OpenSceneMode.Single);
            Debug.Log("Scene opened");

            try
            {
                await InitSDKOnlineModeAsync();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to initialize SDK Online Mode", e);
            }

            try
            {
                await InitSDKAccount();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to initialize SDK Account", e);
            }

            try
            {
                await InitSDKBuildersAsync();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to initialize SDK Builders", e);
            }

            return args;
        }

        [PublicAPI]
        public static void ExitWithException(Exception e)
        {
            Debug.LogError("Failed to preform auto build:\n" + e);
            Debug.LogException(e);

            Exit();
        }

        [PublicAPI]
        public static async void Exit()
        {
            if (_logOutWhenExit)
            {
                Debug.Log("Logging out");
                APIUser.Logout();

                // Wait for http request to finish
                await Task.Delay(5000);

                ApiCredentials.Clear();
                Debug.Log("Logged out");
            }

            Debug.Log("Exiting");
            EditorApplication.Exit(0);
        }

        private static async Task InitSDKBuildersAsync()
        {
            var vrcSdkControlPanel = EditorWindow.GetWindow<VRCSdkControlPanel>();
            var showBuildersMethod =
                typeof(VRCSdkControlPanel).GetMethod("ShowBuilders", BindingFlags.Instance | BindingFlags.NonPublic);

            if (showBuildersMethod == null)
            {
                throw new Exception("Failed to get ShowBuilders method");
            }

            vrcSdkControlPanel.Show(true);
            showBuildersMethod.Invoke(vrcSdkControlPanel, null);

            // Because VRChat SDK initialize builder in ui schedule, we have to wait
            await Task.Delay(1000);
        }

        private static async Task InitSDKOnlineModeAsync()
        {
            API.SetOnlineMode(true);
            ApiCredentials.Load();

            var tcs = new TaskCompletionSource<bool>();

            ConfigManager.RemoteConfig.Init(() => { tcs.SetResult(true); }, () =>
            {
                Debug.LogError("Failed to initialize SDK: Failed to init remote config");
                tcs.SetException(new Exception("Failed to init remote config"));
            });

            await tcs.Task;
        }

        private static bool _logOutWhenExit;

        private static async Task InitSDKAccount()
        {
            var tcs = new TaskCompletionSource<bool>();

            if (ApiCredentials.IsLoaded())
            {
                APIUser.InitialFetchCurrentUser(_ =>
                {
                    Debug.Log($"Logged in as [{APIUser.CurrentUser.id}] {APIUser.CurrentUser.displayName}");

                    tcs.SetResult(true);
                }, model =>
                {
                    if (model == null)
                    {
                        Debug.LogError(
                            "Failed to initialize SDK Account: Failed to fetch current user: Unknown error (Model is null)");
                        tcs.SetException(new Exception("Failed to fetch current user: Unknown error (Model is null)"));
                        return;
                    }

                    Debug.LogError("Failed to initialize SDK Account: Failed to fetch current user: " + model.Error);
                    tcs.SetException(new Exception("Failed to fetch current user: " + model.Error));
                });
            }
            else
            {
                _logOutWhenExit = true;
                AutoBuildAuthentication.Login(GetArguments().Username, GetArguments().Password, GetArguments().TotpKey,
                    () => { tcs.SetResult(true); });
            }

            await tcs.Task;
        }
    }
}
