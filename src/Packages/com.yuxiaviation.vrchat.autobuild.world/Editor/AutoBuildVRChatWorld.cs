using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor.Api;

namespace VRChatAerospaceUniversity.VRChatAutoBuild.Worlds {
    internal static class AutoBuildVRChatWorld {
        [PublicAPI]
        private static async void BuildWorld() {
            try {
                await AutoBuildBase.InitAutoBuildAsync();
            }
            catch (Exception e) {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            Debug.Log("Building world");
            try {
                await BuildAsync();
            }
            catch (Exception e) {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            Debug.Log("World build complete");
            EditorApplication.Exit(0);
        }

        [PublicAPI]
        private static async void BuildAndUploadWorld() {
            AutoBuildArguments args;
            try {
                args = await AutoBuildBase.InitAutoBuildAsync();
            }
            catch (Exception e) {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            var worldId = args.ContentId;

            Debug.Log("Building world");

            try {
                await BuildAsync();
            }
            catch (Exception e) {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            Debug.Log("World build complete");

            Debug.Log($"Fetching world: {worldId}");
            var world = await FetchWorldAsync(worldId);
            Debug.Log(
                $"Fetched world: [{world.ID}] {world.Name} by {world.AuthorName}\nDescription: {world.Description}");

            Debug.Log(
                $"Uploading world: [{world.ID}] {world.Name} by {world.AuthorName}\nDescription: {world.Description}");
            try
            {
                await UploadAsync(world);
            } catch (Exception e)
            {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            Debug.Log("Upload complete");

            EditorApplication.Exit(0);
        }

        private static async Task BuildAsync() {
            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder)) {
                throw new Exception("Failed to get world builder");
            }

            var hasIsCompilingNoticed = false;
            while (EditorApplication.isCompiling)
            {
                if (hasIsCompilingNoticed) continue;

                Debug.Log("Waiting for scripts to compile");
                hasIsCompilingNoticed = true;
            }

            await builder.Build();
        }

        private static async Task UploadAsync(VRCWorld world) {
            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder)) {
                throw new Exception("Failed to get world builder");
            }

            try {
                await builder.UploadLastBuild(world);
            }
            catch (Exception e) {
                throw new Exception("Failed to upload world", e);
            }
        }

        private static async Task<VRCWorld> FetchWorldAsync(string worldId) {
            try {
                return await VRCApi.GetWorld(worldId, true);
            }
            catch (Exception e)
            {
                var ex = new Exception("Failed to fetch world", e);
                AutoBuildBase.ExitWithException(ex);

                throw ex;
            }
        }
    }
}
