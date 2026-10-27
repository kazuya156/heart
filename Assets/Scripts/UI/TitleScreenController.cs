using UnityEngine;

namespace SeaOfMemories
{
    public sealed class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        public void Configure(GameFlowController gameFlow) { flow = gameFlow; }
        public void OnStartPressed() { flow.StartNewGame(); }
        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            Debug.Log("[SeaOfMemories] Quit is tested in a Windows build. Stop Play mode to exit the Editor test.");
#else
            Application.Quit();
#endif
        }
    }
}
