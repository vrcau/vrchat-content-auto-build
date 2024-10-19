using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using VRC.Core;

namespace VRChatAerospaceUniversity.VRChatAutoBuild
{
    public class AutoBuildSecretStorage
    {
        [PublicAPI]
        public static AutoBuildSecretStorage Instance { get; private set; } = new();

        [CanBeNull] public string AuthCookie { get; set; }
        [CanBeNull] public string TwoFactorAuthCookie { get; set; }

        [PublicAPI]
        public static async Task<AutoBuildSecretStorage> LoadAsync(string path)
        {
            try
            {
                var storageFile = await File.ReadAllTextAsync(path);

                Instance = JsonConvert.DeserializeObject<AutoBuildSecretStorage>(storageFile);
                return Instance;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to load authentication storage", e);
            }
        }

        [PublicAPI]
        public void LoadApiCredentials(string username)
        {
            ApiCredentials.Set(username, username, "vrchat", AuthCookie, TwoFactorAuthCookie);
        }

        [PublicAPI]
        public async Task SaveAsync(string path)
        {
            try
            {
                var storageFile = JsonConvert.SerializeObject(this);

                await File.WriteAllTextAsync(path, storageFile);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to save authentication storage", e);
            }
        }
    }
}
