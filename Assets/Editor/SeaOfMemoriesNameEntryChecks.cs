using System;
using UnityEditor;
using UnityEngine;

namespace SeaOfMemories.Editor
{
    public static class SeaOfMemoriesNameEntryChecks
    {
        [MenuItem("Sea of Memories/Checks/Name Entry")]
        public static void Run()
        {
            var session = new GameSession();
            Require(session.PlayerName == "", "Initial name must be empty.");
            foreach (string invalid in new[] { null, "", "   ", "　", "ABCDEFGHIJKLM", "名\n前", "名\t前" })
                Require(!session.TrySetPlayerName(invalid) && session.PlayerName == "", "Invalid name was accepted.");
            Require(session.TrySetPlayerName("　ケン　") && session.PlayerName == "ケン", "Japanese name/trim failed.");
            Require(!session.TrySetPlayerName(" ") && session.PlayerName == "ケン", "Rejected input must preserve the accepted name.");
            Require(session.TrySetPlayerName("ABCDEFGHIJKL"), "12 characters must be accepted.");
            Require(session.TrySetPlayerName("か\u3099"), "Combining character must be accepted.");
            Require(session.TrySetPlayerName("<b>KEN</b>"), "Plain-text name must not be rewritten as markup.");
            Require(session.TryAddItem("ITEM_01"), "Reset test setup failed.");
            session.Reset();
            Require(session.PlayerName == "" && session.ItemIds.Count == 0 && session.QuestionCount == 0
                && session.ComboCount == 0 && session.Agitation == 0, "New-game reset must clear name and game state.");
            Require(session.TrySetPlayerName("次の名前") && session.PlayerName == "次の名前", "Second playthrough name failed.");
            Debug.Log("[SeaOfMemories] Name entry checks passed: blank/length validation, Japanese text, trim, rejected input and new-game reset. Test UI, IME and BGM in Play mode.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Name entry check: " + message);
        }
    }
}
