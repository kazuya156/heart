using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeaOfMemories
{
    [Serializable]
    public sealed class SpriteCatalogEntry
    {
        public string id;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class AudioCatalogEntry
    {
        public string id;
        public AudioClip clip;
    }

    // 家具・アイテムと画像IDを集約。画像未受領の登録項目はnullを許可する。
    public sealed class ContentCatalog : MonoBehaviour
    {
        [SerializeField] private FurnitureDefinition[] furniture;
        [SerializeField] private ItemDefinition[] items;
        [SerializeField] private ScenarioRepository repository;
        [SerializeField] private SpriteCatalogEntry[] sprites;
        [SerializeField] private AudioCatalogEntry[] audioClips;

        public static readonly string[] RequiredAudioIds =
        {
            "BGM_01", "BGM_02", "BGM_03", "BGM_04", "BGM_05",
            "SE_01", "SE_02", "SE_03", "SE_05", "SE_06", "SE_07", "SE_08", "SE_09", "SE_10"
        };

        public static readonly string[] RequiredBackgroundIds =
        { "BG_01", "BG_02", "BG_03", "BG_04", "BG_05", "BG_06", "BG_07", "BG_08", "BG_09" };

        public static bool IsBackgroundId(string id)
        {
            foreach (string known in RequiredBackgroundIds) if (id == known) return true;
            return false;
        }

        public bool ValidateBackgrounds()
        {
            var ids = new HashSet<string>();
            if (sprites != null)
                foreach (SpriteCatalogEntry entry in sprites)
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !ids.Add(NormalizeExpressionId(entry.id)))
                        return Fail("Sprite IDs are empty or duplicated.");
            foreach (string id in RequiredBackgroundIds)
                if (!HasSpriteId(id)) return Fail("Missing background ID: " + id);
            // 画像未受領のnullはBackgroundViewが仮背景へ置き換える。
            return true;
        }

        public static bool IsBgmId(string id)
        {
            return id == "BGM_01" || id == "BGM_02" || id == "BGM_03" || id == "BGM_04" || id == "BGM_05";
        }

        public static bool IsSeId(string id)
        {
            return id == "SE_01" || id == "SE_02" || id == "SE_03" || id == "SE_05"
                || id == "SE_06" || id == "SE_07" || id == "SE_08" || id == "SE_09" || id == "SE_10";
        }

        public void ConfigureAudio(AudioCatalogEntry[] entries) { audioClips = entries; }

        public bool HasAudioId(string id)
        {
            if (audioClips != null)
                foreach (AudioCatalogEntry entry in audioClips)
                    if (entry != null && entry.id == id) return true;
            return false;
        }

        public AudioClip GetAudio(string id)
        {
            if (audioClips != null)
                foreach (AudioCatalogEntry entry in audioClips)
                    if (entry != null && entry.id == id) return entry.clip;
            Debug.LogError("[SeaOfMemories] Missing audio ID: " + id, this);
            return null;
        }

        public bool ValidateAudio()
        {
            var ids = new HashSet<string>();
            if (audioClips != null)
                foreach (AudioCatalogEntry entry in audioClips)
                    if (entry == null || (!IsBgmId(entry.id) && !IsSeId(entry.id)) || !ids.Add(entry.id))
                        return Fail("Audio IDs are unknown, retired or duplicated (SE_04 is retired).");
            foreach (string id in RequiredAudioIds)
                if (!ids.Contains(id)) return Fail("Missing audio ID: " + id);
            if (repository != null)
                foreach (string id in repository.GetAudioIds())
                    if (!ids.Contains(id)) return Fail("Unknown scenario audio: " + id);
            // 未受領のclipはnullを許可。仮音ONなら実行時にのみ確認音を生成する。
            return true;
        }

        public void ConfigureSprites(SpriteCatalogEntry[] entries) { sprites = entries; }

        public static string NormalizeExpressionId(string id)
        {
            // 資料の短い表記と、共通接頭辞を付けた表記の両方を同じ枠へ解決する。
            switch (id)
            {
                case "CH_01_shaken1": return "shaken1";
                case "CH_01_shaken2": return "shaken2";
                case "CH_01_limit": return "limit";
                case "CH_01_smile": return "smile";
                default: return id;
            }
        }

        public bool HasSpriteId(string id)
        {
            if (sprites != null)
                foreach (SpriteCatalogEntry entry in sprites)
                    if (entry != null && NormalizeExpressionId(entry.id) == NormalizeExpressionId(id)) return true;
            return false;
        }

        public Sprite GetSprite(string id)
        {
            if (sprites != null)
                foreach (SpriteCatalogEntry entry in sprites)
                    if (entry != null && NormalizeExpressionId(entry.id) == NormalizeExpressionId(id)) return entry.sprite;
            Debug.LogError("[SeaOfMemories] Missing sprite ID: " + id, this);
            return null;
        }

        public bool ValidatePortraits()
        {
            var ids = new HashSet<string>();
            if (sprites != null)
                foreach (SpriteCatalogEntry entry in sprites)
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !ids.Add(NormalizeExpressionId(entry.id)))
                        return Fail("Sprite IDs are empty or duplicated.");
            string[] required = { "GUARD_01", "GUARD_02", "GUARD_03", "GUARD_04", "GUARD_05", "GUARD_06" };
            foreach (string id in required) if (!HasSpriteId(id)) return Fail("Missing portrait ID: " + id);
            if (repository != null)
                foreach (string id in repository.GetExpressionIds())
                    if (!HasSpriteId(id)) return Fail("Unknown scenario expression: " + id);
            return true;
        }

        public void Configure(FurnitureDefinition[] rooms, ItemDefinition[] information, ScenarioRepository scenarios)
        {
            furniture = rooms;
            items = information;
            repository = scenarios;
        }

        public ItemDefinition GetItem(string id)
        {
            if (items != null)
                foreach (ItemDefinition item in items)
                    if (item != null && item.Id == id) return item;
            Debug.LogError("[SeaOfMemories] Missing item definition: " + id, this);
            return null;
        }

        public FurnitureDefinition GetFurniture(string id)
        {
            if (furniture != null)
                foreach (FurnitureDefinition definition in furniture)
                    if (definition != null && definition.Id == id) return definition;
            Debug.LogError("[SeaOfMemories] Missing furniture definition: " + id, this);
            return null;
        }

        public bool Validate()
        {
            if (repository == null || furniture == null || furniture.Length != 3 || items == null || items.Length != 6)
                return Fail("Catalog needs a scenario repository, 3 furniture definitions and 6 item definitions.");
            var itemIds = new HashSet<string>();
            foreach (ItemDefinition item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Id) || !itemIds.Add(item.Id)
                    || string.IsNullOrWhiteSpace(item.DisplayName) || string.IsNullOrWhiteSpace(item.DescriptionSequenceId))
                    return Fail("An item definition is incomplete or duplicated.");
                // 説明は台詞のみ。取得判断は説明完了後に別の処理から表示する。
                if (!item.HasValidHotspot)
                    return Fail("Item hotspot must have positive size and fit within normalized 0..1 bounds: " + item.Id);
                string next = item.DescriptionSequenceId;
                var visited = new HashSet<string>();
                while (!string.IsNullOrEmpty(next))
                {
                    if (!visited.Add(next)) return Fail("Description sequence cycle: " + next);
                    ScenarioSequence sequence = repository.Find(next);
                    if (sequence == null) return false;
                    foreach (ScenarioNode node in sequence.nodes)
                        if (node.type != "line") return Fail("Item descriptions must contain line nodes only: " + next);
                    next = sequence.nextSequenceId;
                }
            }
            string[] owners = { "DESK", "BED", "WINDOW" };
            for (int i = 0; i < owners.Length; i++)
            {
                FurnitureDefinition definition = GetFurniture(owners[i]);
                if (definition == null || definition.BackgroundId != "BG_0" + (i + 5)
                    || definition.ItemIds == null || definition.ItemIds.Count != 2)
                    return Fail("Furniture data differs from the design: " + owners[i]);
                for (int j = 0; j < 2; j++)
                {
                    string expectedId = "ITEM_0" + (i * 2 + j + 1);
                    if (definition.ItemIds[j] != expectedId) return Fail("Incorrect furniture/item pair: " + owners[i]);
                    ItemDefinition item = GetItem(expectedId);
                    if (item == null || item.FurnitureId != owners[i] || item.IsValid != (expectedId == "ITEM_01" || expectedId == "ITEM_03" || expectedId == "ITEM_06"))
                        return Fail("Incorrect item owner or validity: " + expectedId);
                }
            }
            // 画像は未受領ならnullでよい。Viewがプレースホルダー表示に切り替える。
            return true;
        }

        public bool ValidateInterrogation()
        {
            if (items == null || items.Length != 6 || repository == null) return Fail("Interrogation catalog is incomplete.");
            foreach (ItemDefinition item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.QuestionLabel)
                    || !ValidateLines(item.QuestionSequenceId) || !ValidateLines(item.ReplySequenceId)) return false;
            }
            string[] singleLines = { "QUESTION_PROMPT", "QUESTION_MISSING_INFO", "QUESTION_VALID_REACTION" };
            foreach (string id in singleLines)
            {
                if (!ValidateLines(id)) return false;
                ScenarioSequence sequence = repository.Find(id);
                if (sequence.nodes.Length != 1 || !string.IsNullOrEmpty(sequence.nextSequenceId)
                    || string.IsNullOrEmpty(sequence.nodes[0].text)) return Fail("Expected one nonempty line: " + id);
            }
            return ValidateLines("BAD_END") && ValidateLines("TRUE_END");
        }

        private bool ValidateLines(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return Fail("A question or reply sequence ID is empty.");
            var visited = new HashSet<string>();
            while (!string.IsNullOrEmpty(id))
            {
                if (!visited.Add(id)) return Fail("Question/reply sequence cycle: " + id);
                ScenarioSequence sequence = repository.Find(id);
                if (sequence == null) return false;
                foreach (ScenarioNode node in sequence.nodes)
                    if (node.type != "line") return Fail("Questions and replies must use line nodes: " + id);
                id = sequence.nextSequenceId;
            }
            return true;
        }

        private bool Fail(string message)
        {
            Debug.LogError("[SeaOfMemories] " + message, this);
            return false;
        }
    }
}

