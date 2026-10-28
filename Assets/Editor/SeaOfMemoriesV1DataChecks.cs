using System;
using UnityEditor;
using UnityEngine;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesV1DataChecks
    {
        [MenuItem("Sea of Memories/Checks/V1 Data")]
        public static void Run()
        {
            if (Application.isPlaying) { Debug.LogError("Stop Play mode first."); return; }
            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            ContentCatalog catalog = UnityEngine.Object.FindFirstObjectByType<ContentCatalog>();
            ScenarioRepository repository = UnityEngine.Object.FindFirstObjectByType<ScenarioRepository>();
            if (flow == null || catalog == null || repository == null) { Debug.LogError("Open MainScene first."); return; }
            var files = new TextAsset[5];
            string[] names = { "intro", "exploration", "questions", "true_end", "bad_end" };
            var settings = new SerializedObject(flow);
            SerializedProperty assigned = settings.FindProperty("scenarios");
            if (assigned == null || assigned.arraySize != 5) { Debug.LogError("MainScene must reference exactly 5 scenario files."); return; }
            for (int i = 0; i < names.Length; i++)
            {
                files[i] = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Scenario/" + names[i] + ".json");
                if (assigned.GetArrayElementAtIndex(i).objectReferenceValue != files[i])
                { Debug.LogError("Incorrect scenario assignment: " + names[i]); return; }
            }
            if (!repository.LoadAll(files) || !catalog.Validate() || !catalog.ValidateInterrogation()
                || !catalog.ValidateBackgrounds() || !catalog.ValidatePortraits() || !catalog.ValidateAudio()) return;
            foreach (string id in ContentCatalog.RequiredAudioIds)
                if (catalog.GetAudio(id) == null) { Debug.LogError("Local audio is not connected: " + id); return; }
            for (int i = 1; i <= 6; i++)
            {
                string id = "ITEM_" + i.ToString("00");
                ItemDefinition item = catalog.GetItem(id);
                if (item.Icon == null || item.LargeImage == null || item.DescriptionSequenceId != id + "_INSPECT")
                { Debug.LogError("Item image/description is not connected: " + id); return; }
            }
            Debug.Log("[SeaOfMemories] V1 data checks passed: 5 files, global IDs/links, new catalog IDs, item validity, local audio and item images. Use the separate Name Entry, Artwork Hotspots and Backlog checks; final ending scripts remain separate work.");
        }
    }
}
