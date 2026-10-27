using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesAudioSetup
    {
        [MenuItem("Sea of Memories/11 - Add Audio")]
        public static void AddAudio()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MainScene.unity") { Notice("Open Assets/Scenes/MainScene.unity first."); return; }
            var flows = new List<GameFlowController>();
            var controllers = new List<AudioController>();
            var listeners = new List<AudioListener>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                flows.AddRange(sceneRoot.GetComponentsInChildren<GameFlowController>(true));
                controllers.AddRange(sceneRoot.GetComponentsInChildren<AudioController>(true));
                foreach (AudioListener listener in sceneRoot.GetComponentsInChildren<AudioListener>(true))
                    if (listener.isActiveAndEnabled) listeners.Add(listener);
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return; }
            if (controllers.Count != 0) { Notice("AudioController already exists. No duplicate was created."); return; }
            if (listeners.Count != 1) { Notice("Keep exactly one enabled AudioListener (normally on Main Camera)."); return; }
            GameFlowController flow = flows[0];
            var serializedFlow = new SerializedObject(flow);
            var catalog = serializedFlow.FindProperty("catalog").objectReferenceValue as ContentCatalog;
            var repository = serializedFlow.FindProperty("repository").objectReferenceValue as ScenarioRepository;
            var scenario = serializedFlow.FindProperty("scenario").objectReferenceValue as TextAsset;
            var dialogue = serializedFlow.FindProperty("dialogue").objectReferenceValue as DialoguePlayer;
            var interrogation = serializedFlow.FindProperty("interrogation").objectReferenceValue as InterrogationController;
            if (catalog == null || repository == null || scenario == null || dialogue == null || interrogation == null
                || !interrogation.HasQuestionVisuals || serializedFlow.FindProperty("audioController").objectReferenceValue != null)
            { Notice("Apply step 10, save MainScene, and check its references first."); return; }
            var serializedDialogue = new SerializedObject(dialogue);
            var choices = serializedDialogue.FindProperty("choicePanel").objectReferenceValue as ChoicePanel;
            if (choices == null || !choices.IsConfigured || flow.transform.Find("Audio") != null
                || serializedDialogue.FindProperty("audioController").objectReferenceValue != null
                || new SerializedObject(choices).FindProperty("audioController").objectReferenceValue != null
                || new SerializedObject(interrogation).FindProperty("audioController").objectReferenceValue != null)
            { Notice("Audio references or Audio object already exist, or the choice UI is missing. Nothing was overwritten."); return; }
            if (!repository.Load(scenario) || !catalog.Validate() || !catalog.ValidatePortraits())
            { Notice("Check Console for scenario/catalog errors."); return; }
            var serializedCatalog = new SerializedObject(catalog);
            SerializedProperty audio = serializedCatalog.FindProperty("audioClips");
            var entries = new List<AudioCatalogEntry>();
            var ids = new HashSet<string>();
            for (int i = 0; i < audio.arraySize; i++)
            {
                SerializedProperty row = audio.GetArrayElementAtIndex(i);
                string id = row.FindPropertyRelative("id").stringValue;
                if ((!ContentCatalog.IsBgmId(id) && !ContentCatalog.IsSeId(id)) || !ids.Add(id))
                { Notice("Existing audio IDs are unknown, retired or duplicated. Fix them before continuing."); return; }
                entries.Add(new AudioCatalogEntry { id = id, clip = row.FindPropertyRelative("clip").objectReferenceValue as AudioClip });
            }
            foreach (string id in ContentCatalog.RequiredAudioIds)
                if (ids.Add(id)) entries.Add(new AudioCatalogEntry { id = id });

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add audio");
            Undo.RecordObject(catalog, "Register audio IDs");
            catalog.ConfigureAudio(entries.ToArray());
            if (!catalog.ValidateAudio())
            {
                Undo.RevertAllDownToGroup(group);
                Notice("Audio ID validation failed. Check Console. No audio objects were created.");
                return;
            }
            var audioRoot = new GameObject("Audio");
            audioRoot.transform.SetParent(flow.transform, false);
            AudioSource first = CreateSource(audioRoot.transform, "BGM_A");
            AudioSource second = CreateSource(audioRoot.transform, "BGM_B");
            AudioSource se = CreateSource(audioRoot.transform, "SE");
            var controller = audioRoot.AddComponent<AudioController>();
            controller.Configure(catalog, first, second, se, true);
            Undo.RegisterCreatedObjectUndo(audioRoot, "Create audio controller and sources");
            Undo.RecordObject(flow, "Connect flow audio");
            Undo.RecordObject(dialogue, "Connect dialogue audio");
            Undo.RecordObject(choices, "Connect choice audio");
            Undo.RecordObject(interrogation, "Connect question audio");
            flow.ConfigureAudio(controller);
            dialogue.ConfigureAudio(controller);
            choices.ConfigureAudio(controller);
            interrogation.ConfigureAudio(controller);
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(dialogue);
            EditorUtility.SetDirty(choices);
            EditorUtility.SetDirty(interrogation);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = audioRoot;
            Notice("Save MainScene (Ctrl+S), then Play. TEMPORARY TEST TONES are enabled for missing clips. The title has a low looping tone; actual page advances and choices have SE_01, dummy questions SE_06, endings scenario SEs. Assign final clips in ContentCatalog > Audio Clips, then turn OFF Use Temporary Test Tones for release. BGM_02-04 remain registered but have no invented story cues.");
        }

        private static AudioSource CreateSource(Transform parent, string objectName)
        {
            var sourceRoot = new GameObject(objectName);
            sourceRoot.transform.SetParent(parent, false);
            var source = sourceRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0f;
            source.spatialBlend = 0f;
            return source;
        }

        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M11", message, "OK"); }
    }
}
