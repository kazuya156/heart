using System;
using UnityEditor;
using UnityEngine;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesScenarioV11Checks
    {
        [MenuItem("Sea of Memories/Checks/Scenario v1.1")]
        public static void Run()
        {
            if (Application.isPlaying) { Debug.LogError("Stop Play mode first."); return; }
            var repo = UnityEngine.Object.FindFirstObjectByType<ScenarioRepository>();
            Require(repo != null, "Open MainScene first.");
            string[] names = { "intro", "exploration", "questions", "true_end", "bad_end" };
            var files = new TextAsset[5];
            for (int i = 0; i < names.Length; i++) files[i] = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Scenario/" + names[i] + ".json");
            Require(repo.LoadAll(files), "Scenario loading failed.");
            ScenarioNode start = repo.Find("TRUE_END_START").nodes[0];
            Require(start.portraitAction == "show" && start.speakerName == "看守" && start.expressionId == "GUARD_06", "New fields did not load.");
            Require(string.IsNullOrEmpty(start.bgmId), "TrueEnd must not start BGM_04 early.");
            Require(repo.Find("TRUE_END_AFTER_CLUE").nodes[1].bgmId == "BGM_04", "TrueEnd BGM cue missing.");
            Require(string.IsNullOrEmpty(repo.Find("BAD_END_START").nodes[0].bgmId), "BadEnd must not start BGM_03 early.");
            ScenarioNode dive = repo.Find("BAD_END_DIVE").nodes[0];
            Require(dive.portraitAction == "hide" && dive.bgmId == "BGM_03" && dive.seId == "SE_08", "Dive commands missing.");
            var items = new ItemDefinition[6];
            for (int i = 0; i < 6; i++) items[i] = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Data/Definitions/ITEM_0" + (i + 1) + ".asset");
            int orders = 0;
            for (int a = 0; a < 6; a++) for (int b = 0; b < 6; b++) for (int c = 0; c < 6; c++)
            {
                if (a == b || b == c || a == c) continue;
                var session = new GameSession();
                int count = 0, combo = 0, score = 0;
                foreach (int index in new[] { a, b, c })
                {
                    ItemDefinition item = items[index];
                    Require(item != null && session.TryAddItem(item.Id), "Item fixture missing.");
                    Require(session.TryBeginQuestion(item).status == QuestionStatus.Accepted, "Question selection rejected.");
                    count++;
                    Require(session.QuestionCount == count && session.Agitation == score && session.ComboCount == combo && session.HasAsked(item.Id), "Selection must reserve one question, without applying its result.");
                    if (item.IsValid) { combo++; score += combo == 1 ? 20 : combo == 2 ? 30 : 50; } else combo = 0;
                    QuestionResult result = session.CompleteQuestion(item);
                    Require(result.status == QuestionStatus.Accepted && session.QuestionCount == count && session.Agitation == score && session.ComboCount == combo, "Incorrect RESULT application.");
                    Require(session.CompleteQuestion(item).status != QuestionStatus.Accepted && session.Agitation == score, "Result applied twice.");
                    Require(session.TryBeginQuestion(item).status != QuestionStatus.Accepted && session.QuestionCount == count, "Repeated item counted twice.");
                    if (count == 3) Require(result.isFinalQuestion && result.ending == (score == 100 ? EndingType.True : EndingType.Bad), "Ending changed.");
                }
                session.Reset();
                Require(session.QuestionCount == 0 && session.Agitation == 0 && session.ItemIds.Count == 0, "Reset failed.");
                orders++;
            }
            Debug.Log("[SeaOfMemories] Scenario v1.1 checks passed: new fields/audio cues and 120 question orders, selection/result separation, no double counts and reset. Verify portraits, labels, effects and full endings in Play mode.");
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Scenario v1.1: " + message);
        }
    }
}
