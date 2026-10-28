using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeaOfMemories.EditorTools
{
    // Read local ignored assets only; never copy, move or upload their bytes.
    public static class SeaOfMemoriesLocalAssetSetup
    {
        [MenuItem("Sea of Memories/21 - Connect Local Assets")]
        public static void Connect()
        {
            if (Application.isPlaying) { Debug.LogError("Stop Play mode first."); return; }
            ContentCatalog catalog = UnityEngine.Object.FindFirstObjectByType<ContentCatalog>();
            if (catalog == null) { Debug.LogError("Open MainScene first."); return; }
            var sprites = new List<SpriteCatalogEntry>();
            var audio = new List<AudioCatalogEntry>();
            bool valid = true;
            for (int i = 1; i <= 9; i++) AddSprite("BG_" + i.ToString("00"), sprites, ref valid);
            for (int i = 1; i <= 6; i++) AddSprite("GUARD_" + i.ToString("00"), sprites, ref valid);
            for (int i = 1; i <= 6; i++) AddSprite("ITEM_" + i.ToString("00"), sprites, ref valid);
            foreach (string id in ContentCatalog.RequiredAudioIds)
            {
                string path = FindUnique(id, ref valid);
                AudioClip clip = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) { valid = false; Debug.LogError("Missing AudioClip: " + id); }
                audio.Add(new AudioCatalogEntry { id = id, clip = clip });
            }
            if (!valid) { Debug.LogError("Local assets were NOT connected. Resolve the errors above first."); return; }
            Undo.RecordObject(catalog, "Connect local assets");
            catalog.ConfigureSprites(sprites.ToArray());
            catalog.ConfigureAudio(audio.ToArray());
            EditorUtility.SetDirty(catalog);
            for (int i = 1; i <= 6; i++)
            {
                ItemDefinition item = catalog.GetItem("ITEM_" + i.ToString("00"));
                if (item == null) continue;
                Undo.RecordObject(item, "Connect item images");
                var serialized = new SerializedObject(item);
                Sprite picture = catalog.GetSprite(item.Id);
                serialized.FindProperty("icon").objectReferenceValue = picture;
                serialized.FindProperty("largeImage").objectReferenceValue = picture;
                serialized.ApplyModifiedProperties();
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(catalog.gameObject.scene);
            Debug.Log("[SeaOfMemories] 21 sprites and 14 audio clips connected. Save MainScene with Ctrl+S. LocalGameAssets remain ignored by Git.");
        }
        private static void AddSprite(string id, List<SpriteCatalogEntry> output, ref bool valid)
        {
            string path = FindUnique(id, ref valid);
            Sprite sprite = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) { valid = false; Debug.LogError("Missing Sprite: " + id + ". Check Sprite (2D and UI), Single, Apply."); }
            output.Add(new SpriteCatalogEntry { id = id, sprite = sprite });
        }
        private static string FindUnique(string id, ref bool valid)
        {
            string found = null;
            foreach (string guid in AssetDatabase.FindAssets(id, new[] { "Assets/LocalGameAssets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), id, StringComparison.Ordinal)) continue;
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (found != null) { valid = false; Debug.LogError("Ambiguous local asset: " + id); return null; }
                found = path;
            }
            if (found == null) { valid = false; Debug.LogError("Local asset not found: " + id); }
            return found;
        }
    }
}
