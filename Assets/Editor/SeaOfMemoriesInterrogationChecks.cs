using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesInterrogationChecks
    {
        [MenuItem("Sea of Memories/Checks/Question Rules")]
        public static void RunFromMenu() { CheckOrLog(); }

        public static bool CheckOrLog()
        {
            var items = new ItemDefinition[6];
            try
            {
                string[] owners = { "DESK", "DESK", "BED", "BED", "WINDOW", "WINDOW" };
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = ScriptableObject.CreateInstance<ItemDefinition>();
                    items[i].Configure("ITEM_0" + (i + 1), "TEST", i == 0 || i == 2 || i == 5, owners[i], "TEST_DESC");
                }
                // 仕様から書いた8パターンの期待値。実装と同じ計算式は使わない。
                var scores = new Dictionary<string, int[]>
                {
                    { "VVV", new[] {20, 50, 100} }, { "VVD", new[] {20, 50, 50} },
                    { "VDV", new[] {20, 20, 40} }, { "VDD", new[] {20, 20, 20} },
                    { "DVV", new[] {0, 20, 50} }, { "DVD", new[] {0, 20, 20} },
                    { "DDV", new[] {0, 0, 20} }, { "DDD", new[] {0, 0, 0} }
                };
                var combos = new Dictionary<string, int[]>
                {
                    { "VVV", new[] {1, 2, 3} }, { "VVD", new[] {1, 2, 0} },
                    { "VDV", new[] {1, 0, 1} }, { "VDD", new[] {1, 0, 0} },
                    { "DVV", new[] {0, 1, 2} }, { "DVD", new[] {0, 1, 0} },
                    { "DDV", new[] {0, 0, 1} }, { "DDD", new[] {0, 0, 0} }
                };
                int cases = 0;
                foreach (int[] owned in Permutations(new[] {0, 1, 2, 3, 4, 5}, 4))
                {
                    foreach (int[] asked in Permutations(owned, 3))
                    {
                        var session = new GameSession();
                        foreach (int index in owned) Require(session.TryAddItem(items[index].Id), "Acquisition failed.");
                        int missing = 0;
                        while (session.HasItem(items[missing].Id)) missing++;
                        AssertRejected(session, null, QuestionStatus.NotOwned);
                        AssertRejected(session, items[missing], QuestionStatus.NotOwned);
                        string pattern = "";
                        foreach (int index in asked) pattern += items[index].IsValid ? "V" : "D";
                        for (int step = 0; step < asked.Length; step++)
                        {
                            ItemDefinition item = items[asked[step]];
                            int before = session.Agitation;
                            QuestionResult result = session.TryApplyQuestion(item);
                            Require(result.status == QuestionStatus.Accepted, "Question rejected.");
                            Require(result.delta == scores[pattern][step] - before, "Incorrect delta: " + pattern);
                            Require(result.agitationAfter == scores[pattern][step] && session.Agitation == result.agitationAfter,
                                "Incorrect accumulated agitation: " + pattern);
                            Require(result.comboAfter == combos[pattern][step] && session.ComboCount == result.comboAfter,
                                "Incorrect combo: " + pattern);
                            Require(result.questionsAfter == step + 1 && session.QuestionCount == step + 1,
                                "Incorrect question count.");
                            Require(session.HasAsked(item.Id) && result.isFinalQuestion == (step == 2), "Incorrect asked/final flag.");
                            EndingType expected = step < 2 ? EndingType.None : pattern == "VVV" ? EndingType.True : EndingType.Bad;
                            Require(result.ending == expected, "Incorrect ending: " + pattern);
                            AssertRejected(session, item, QuestionStatus.AlreadyAsked);
                        }
                        foreach (int index in owned)
                            if (!session.HasAsked(items[index].Id)) AssertRejected(session, items[index], QuestionStatus.LimitReached);
                        for (int i = 0; i < owned.Length; i++) Require(session.ItemIds[i] == items[owned[i]].Id, "Ownership order changed.");
                        Require(session.Phase == GamePhase.Title && session.CurrentFurnitureId == "", "Question changed phase/furniture.");
                        session.Reset();
                        Require(session.ItemIds.Count == 0 && session.QuestionCount == 0 && session.ComboCount == 0 && session.Agitation == 0,
                            "Reset left progress behind.");
                        foreach (ItemDefinition item in items) Require(!session.HasAsked(item.Id), "Reset left asked IDs behind.");
                        cases++;
                    }
                }
                Require(cases == 8640, "Unexpected permutation count.");
                Debug.Log("[SeaOfMemories] Question checks passed: 8640 acquisition/question order combinations, all 8 valid/dummy patterns, rejections and reset.");
                return true;
            }
            catch (Exception error) { Debug.LogException(error); return false; }
            finally { foreach (ItemDefinition item in items) if (item != null) UnityEngine.Object.DestroyImmediate(item); }
        }

        private static void AssertRejected(GameSession session, ItemDefinition item, QuestionStatus expected)
        {
            int count = session.QuestionCount, combo = session.ComboCount, score = session.Agitation;
            string owned = string.Join(",", session.ItemIds);
            string asked = AskedSnapshot(session);
            QuestionResult result = session.TryApplyQuestion(item);
            Require(result.status == expected && result.delta == 0, "Incorrect rejection.");
            Require(count == session.QuestionCount && combo == session.ComboCount && score == session.Agitation
                && owned == string.Join(",", session.ItemIds) && asked == AskedSnapshot(session), "Rejected question changed state.");
            Require(result.questionsAfter == count && result.comboAfter == combo && result.agitationAfter == score
                && !result.isFinalQuestion && result.ending == EndingType.None, "Incorrect rejection snapshot.");
        }

        private static string AskedSnapshot(GameSession session)
        {
            string result = "";
            for (int i = 1; i <= 6; i++) result += session.HasAsked("ITEM_0" + i) ? "1" : "0";
            return result;
        }

        private static IEnumerable<int[]> Permutations(int[] source, int length)
        {
            return Enumerate(source, new int[length], new bool[source.Length], 0);
        }

        private static IEnumerable<int[]> Enumerate(int[] source, int[] selected, bool[] used, int depth)
        {
            if (depth == selected.Length) { yield return (int[])selected.Clone(); yield break; }
            for (int i = 0; i < source.Length; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                selected[depth] = source[i];
                foreach (int[] result in Enumerate(source, selected, used, depth + 1)) yield return result;
                used[i] = false;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[SeaOfMemories] Question check failed: " + message);
        }
    }
}
