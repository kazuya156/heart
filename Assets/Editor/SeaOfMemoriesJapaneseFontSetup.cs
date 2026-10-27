using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace SeaOfMemories.EditorTools
{
    // Editorだけでフォント取得・SDF生成・既存フォントの補完設定を行う。ゲーム中は通信しない。
    public static class SeaOfMemoriesJapaneseFontSetup
    {
        private const string Menu = "Sea of Memories/13 - Add Japanese Font";
        private const string Folder = "Assets/Fonts";
        private const string FontPath = Folder + "/NotoSansCJKjp-Regular.otf";
        private const string AssetPath = Folder + "/NotoSansCJKjp-Regular-SDF.asset";
        private const string LicensePath = "Assets/StreamingAssets/ThirdParty/NotoSansCJKjp-OFL.txt";
        private const string FontUrl = "https://raw.githubusercontent.com/notofonts/noto-cjk/f8d157532fbfaeda587e826d4cd5b21a49186f7c/Sans/OTF/Japanese/NotoSansCJKjp-Regular.otf";
        private const string FontBlobSha = "f56224957fb13a81b4c14bac34f2f058a017f9fb";
        private const int FontBytes = 16467736;
        private const string Seed = "[仮台詞]返事の言い方を選んでください。どちらを選んでも、同じ探索へ進みます。"
            + "不揃いな鉛筆落書き小さな服午前２時の目覚まし時計弟の名前の賞状無限に広がる海必要な情報を持っていないッ！？？？取得する取得しない……「」『』";
        private static UnityWebRequest download;

        [MenuItem(Menu, true)]
        public static bool CanRun() { return !EditorApplication.isPlayingOrWillChangePlaymode && download == null; }

        [MenuItem(Menu)]
        public static void AddJapaneseFont()
        {
            if (!CanRun() || !CheckScene(out _, out _)) return;
            try
            {
                if (!File.Exists(LicensePath)) throw new InvalidOperationException("The bundled font license is missing. Fetch the complete M13 branch first.");
                if (File.Exists(FontPath))
                {
                    VerifyFont(File.ReadAllBytes(FontPath));
                    ApplyFont();
                    return;
                }
                download = UnityWebRequest.Get(FontUrl);
                download.timeout = 60;
                download.SendWebRequest();
                EditorApplication.update += Tick;
                AssemblyReloadEvents.beforeAssemblyReload += StopDownload;
                EditorApplication.quitting += StopDownload;
            }
            catch (Exception error) { Fail(error); }
        }

        private static void Tick()
        {
            if (download == null) { StopDownload(); return; }
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode
                    || EditorUtility.DisplayCancelableProgressBar("Sea of Memories / Japanese font",
                        "Downloading Noto Sans CJK JP (15.7 MiB). Cancel keeps existing UI unchanged.", Mathf.Clamp01(download.downloadProgress)))
                { StopDownload(); return; }
                if (!download.isDone) return;
                if (download.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException("Font download failed: " + download.error + ". Check the connection and run menu 13 again. Existing files were not overwritten.");
                byte[] bytes = download.downloadHandler.data;
                StopDownload();
                VerifyFont(bytes);
                // ダウンロード中に別の処理が同名ファイルを作った場合も上書きしない。
                if (File.Exists(FontPath)) VerifyFont(File.ReadAllBytes(FontPath));
                else
                {
                    EnsureFontFolder();
                    using (var stream = new FileStream(FontPath, FileMode.CreateNew, FileAccess.Write)) stream.Write(bytes, 0, bytes.Length);
                }
                AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceSynchronousImport);
                ApplyFont();
            }
            catch (Exception error) { Fail(error); }
        }

        private static void VerifyFont(byte[] bytes)
        {
            if (bytes == null || bytes.Length != FontBytes) throw new InvalidOperationException("Unexpected font file size. No existing file was overwritten.");
            // Git blobのハッシュを、固定した公式コミットのファイルと照合する。
            using (SHA1 sha = SHA1.Create())
            {
                byte[] header = Encoding.ASCII.GetBytes("blob " + bytes.Length + "\0");
                sha.TransformBlock(header, 0, header.Length, header, 0);
                sha.TransformFinalBlock(bytes, 0, bytes.Length);
                string actual = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
                if (actual != FontBlobSha) throw new InvalidOperationException("Font checksum mismatch. Keep the existing file and check Docs/M13_JapaneseFont.md.");
            }
        }

        private static void ApplyFont()
        {
            if (!CheckScene(out Scene scene, out List<TMP_FontAsset> fonts)) return;
            EnsureFontFolder();
            AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceSynchronousImport);
            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (source == null) throw new InvalidOperationException("Unity could not import the OTF font. Check Console and the font Import Settings.");
            TMP_FontAsset japanese = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            if (japanese == null && File.Exists(AssetPath)) throw new InvalidOperationException("A different file already exists at the SDF asset path. It was not overwritten.");
            if (japanese == null)
            {
                japanese = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048,
                    AtlasPopulationMode.Dynamic, true);
                if (japanese == null) throw new InvalidOperationException("SDF font creation failed. Make sure Include Font Data is enabled on the source font.");
                japanese.name = "NotoSansCJKjp-Regular-SDF";
                // 先に保存し、再実行でも同じGUIDを使う。失敗時も既存アセットを削除しない。
                AssetDatabase.CreateAsset(japanese, AssetPath);
            }
            if (japanese.sourceFontFile != source || japanese.atlasPopulationMode != AtlasPopulationMode.Dynamic
                || !japanese.isMultiAtlasTexturesEnabled)
                throw new InvalidOperationException("The existing SDF asset has different source/dynamic/multi-atlas settings. It was not overwritten.");
            japanese.TryAddCharacters(Seed, out string missing);
            if (!string.IsNullOrEmpty(missing)) throw new InvalidOperationException("Font is missing test characters: " + missing);
            SaveSubAssets(japanese);
            // internalなプロパティへ直接依存せず、存在する版でのみ事前生成文字をビルドへ残す。
            var serialized = new SerializedObject(japanese);
            SerializedProperty clearOnBuild = serialized.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearOnBuild != null) { clearOnBuild.boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(japanese);
            AssetDatabase.SaveAssets();
            VerifySavedSubAssets(japanese);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Japanese fallback font");
            foreach (TMP_FontAsset font in fonts)
            {
                if (font == japanese) continue;
                List<TMP_FontAsset> fallbacks = font.fallbackFontAssetTable;
                if (fallbacks != null && fallbacks.Contains(japanese)) continue;
                Undo.RecordObject(font, "Add Japanese fallback font");
                var updated = fallbacks == null ? new List<TMP_FontAsset>() : new List<TMP_FontAsset>(fallbacks);
                updated.Add(japanese);
                font.fallbackFontAssetTable = updated;
                EditorUtility.SetDirty(font);
            }
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = japanese;
            Notice("Japanese fallback font is ready. Save MainScene (Ctrl+S), then Play > START. The first placeholder line is Japanese; it should not contain square boxes. English fonts, sizes and UI positions are unchanged. Commit ALL font/SDF/atlas/material/meta changes too. See Docs/M13_JapaneseFont.md.");
        }

        private static void SaveSubAssets(TMP_FontAsset font)
        {
            foreach (Texture2D atlas in font.atlasTextures)
            {
                if (atlas == null) continue;
                if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
                EditorUtility.SetDirty(atlas);
            }
            Material material = font.material;
            if (material == null) throw new InvalidOperationException("The SDF material is missing.");
            if (!AssetDatabase.Contains(material)) AssetDatabase.AddObjectToAsset(material, font);
            material.mainTexture = font.atlasTexture;
            EditorUtility.SetDirty(material);
        }

        private static void VerifySavedSubAssets(TMP_FontAsset font)
        {
            bool hasMaterial = false, hasAtlas = false;
            foreach (UnityEngine.Object item in AssetDatabase.LoadAllAssetsAtPath(AssetPath))
            {
                if (item == font.material) hasMaterial = true;
                if (item == font.atlasTexture) hasAtlas = true;
            }
            if (!hasMaterial || !hasAtlas) throw new InvalidOperationException("SDF atlas/material were not saved as sub-assets. Run menu 13 again after checking Console.");
        }

        private static bool CheckScene(out Scene scene, out List<TMP_FontAsset> fonts)
        {
            scene = SceneManager.GetActiveScene();
            fonts = new List<TMP_FontAsset>();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/MainScene.unity")
            { Notice("Stop Play mode and open Assets/Scenes/MainScene.unity first."); return false; }
            var flows = new List<GameFlowController>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                flows.AddRange(sceneRoot.GetComponentsInChildren<GameFlowController>(true));
                foreach (TMP_Text text in sceneRoot.GetComponentsInChildren<TMP_Text>(true))
                    if (text.font != null && !fonts.Contains(text.font)) fonts.Add(text.font);
            }
            if (flows.Count != 1) { Notice("MainScene needs exactly one GameFlowController."); return false; }
            var view = new SerializedObject(flows[0]).FindProperty("backgroundView").objectReferenceValue as BackgroundView;
            if (view == null || !view.IsConfigured) { Notice("Apply step 12 and save MainScene first."); return false; }
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null && !fonts.Contains(defaultFont)) fonts.Add(defaultFont);
            if (fonts.Count == 0) { Notice("Import TMP Essential Resources and check the scene fonts."); return false; }
            foreach (TMP_FontAsset font in fonts)
                if (!AssetDatabase.GetAssetPath(font).StartsWith("Assets/", StringComparison.Ordinal))
                { Notice("A scene font is not an editable project asset. Copy it into Assets and assign it before continuing."); return false; }
            return true;
        }

        private static void EnsureFontFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                if (Directory.Exists(Folder)) AssetDatabase.Refresh();
                else AssetDatabase.CreateFolder("Assets", "Fonts");
            }
        }
        private static void StopDownload()
        {
            EditorApplication.update -= Tick;
            AssemblyReloadEvents.beforeAssemblyReload -= StopDownload;
            EditorApplication.quitting -= StopDownload;
            if (download != null) { download.Abort(); download.Dispose(); download = null; }
            EditorUtility.ClearProgressBar();
        }
        private static void Fail(Exception error) { StopDownload(); Debug.LogException(error); Notice(error.Message); }
        private static void Notice(string message) { EditorUtility.DisplayDialog("Sea of Memories / M13", message, "OK"); }
    }
}
