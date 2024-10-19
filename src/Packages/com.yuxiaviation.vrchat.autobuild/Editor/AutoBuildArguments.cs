using JetBrains.Annotations;

namespace VRChatAerospaceUniversity.VRChatAutoBuild {
    [PublicAPI]
    public class AutoBuildArguments {
        [CanBeNull] public string ContentId;
        public string ScenePath;
    }
}
