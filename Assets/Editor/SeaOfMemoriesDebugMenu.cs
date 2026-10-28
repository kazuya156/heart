using UnityEditor;
using UnityEngine;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesDebugMenu
    {
        private const string ReturnMenu = "Sea of Memories/Debug/Return to Title";

        [MenuItem(ReturnMenu)]
        public static void ReturnToTitle()
        {
            if (!Application.isPlaying) return;
            var flows = Object.FindObjectsByType<GameFlowController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (flows.Length != 1)
            {
                Debug.LogError("[SeaOfMemories] Open one running MainScene to return to its title.");
                return;
            }
            flows[0].EnterTitle();
        }

        [MenuItem(ReturnMenu, true)]
        public static bool CanReturnToTitle() { return Application.isPlaying; }
    }
}
