using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SeaOfMemories.EditorTools
{
    // 実際のGameSessionを使うEditor専用の回帰チェック。シーン・アセットは変更しない。
    public static class SeaOfMemoriesAcquisitionChecks
    {
        [MenuItem("Sea of Memories/Checks/Acquisition Rules")]
        public static void RunFromMenu() { CheckOrLog(); }

        public static bool CheckOrLog()
        {
            try
            {
                int checkedOrders = 0;
                VisitOrders(new List<string>(), new HashSet<string>(), ref checkedOrders);
                Require(checkedOrders == 360, "Expected 360 ordered selections.");
                Debug.Log("[SeaOfMemories] Acquisition checks passed: 360 ordered four-item selections, invalid IDs, duplicate/capacity rejection, read-only inventory and reset.");
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("[SeaOfMemories] Acquisition check failed: " + error.Message);
                return false;
            }
        }

        private static void VisitOrders(List<string> order, HashSet<string> used, ref int checkedOrders)
        {
            if (order.Count == GameSession.MaxItemCount)
            {
                CheckOrder(order);
                checkedOrders++;
                return;
            }
            for (int i = 1; i <= 6; i++)
            {
                string id = "ITEM_0" + i;
                if (!used.Add(id)) continue;
                order.Add(id);
                VisitOrders(order, used, ref checkedOrders);
                order.RemoveAt(order.Count - 1);
                used.Remove(id);
            }
        }

        private static void CheckOrder(List<string> order)
        {
            var session = new GameSession();
            string[] invalid = { null, "", "ITEM_00", "ITEM_07", "item_01", " ITEM_01" };
            foreach (string id in invalid) Require(!session.TryAddItem(id), "Invalid ID accepted.");
            Require(session.ItemIds.Count == 0, "Invalid IDs changed the inventory.");
            var expected = new List<string>();
            foreach (string id in order)
            {
                Require(session.TryAddItem(id), "Valid item rejected: " + id);
                expected.Add(id);
                Require(session.HasItem(id), "Added item not found.");
                Require(!session.TryAddItem(id), "Duplicate accepted.");
                Require(session.ItemIds.Count == expected.Count, "Duplicate changed item count.");
                for (int i = 0; i < expected.Count; i++) Require(session.ItemIds[i] == expected[i], "Acquisition order changed.");
            }
            Require(session.IsInventoryFull(), "Four items did not fill the inventory.");
            for (int i = 1; i <= 6; i++) Require(!session.TryAddItem("ITEM_0" + i), "Full inventory accepted an item.");
            Require(session.ItemIds.Count == 4, "Capacity rejection changed item count.");
            var mutable = session.ItemIds as IList<string>;
            if (mutable != null)
            {
                bool blocked = false;
                try { mutable.Add("ITEM_01"); }
                catch (NotSupportedException) { blocked = true; }
                Require(blocked, "Public inventory allowed external mutation.");
            }
            Require(session.QuestionCount == 0 && session.ComboCount == 0 && session.Agitation == 0,
                "Picking up valid/dummy items changed interrogation state.");
            session.Reset();
            Require(session.ItemIds.Count == 0 && !session.IsInventoryFull(), "Reset did not clear inventory.");
            foreach (string id in order) Require(!session.HasItem(id), "Reset left an owned item.");
            Require(session.TryAddItem(order[0]), "Could not acquire after reset.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
