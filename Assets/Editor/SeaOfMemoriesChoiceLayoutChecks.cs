using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories.EditorTools
{
    public static class SeaOfMemoriesChoiceLayoutChecks
    {
        private const string Menu = "Sea of Memories/Checks/Choice Layout (Play mode)";
        private static GameObject host;
        private static ChoicePanel panel;
        private static Button selected;
        private static int shownFrame, picked;
        private static double deadline;

        [MenuItem(Menu, true)]
        private static bool CanRun() { return EditorApplication.isPlaying && !EditorApplication.isPaused && host == null; }

        [MenuItem(Menu)]
        private static void Run()
        {
            if (!CanRun()) return;
            try
            {
                host = new GameObject("TemporaryChoiceLayoutChecks", typeof(RectTransform), typeof(Canvas));
                host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panel = host.AddComponent<ChoicePanel>();
                var content = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
                content.transform.SetParent(host.transform, false);
                var rect = content.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(720f, 300f);
                var templateRoot = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button), typeof(LayoutElement));
                templateRoot.transform.SetParent(content.transform, false);
                Button template = templateRoot.GetComponent<Button>();
                template.targetGraphic = templateRoot.GetComponent<Image>();
                templateRoot.GetComponent<LayoutElement>().minHeight = 84f;
                var labelRoot = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelRoot.transform.SetParent(templateRoot.transform, false);
                TMP_Text label = labelRoot.GetComponent<TextMeshProUGUI>();
                label.font = TMP_Settings.defaultFontAsset;
                Require(label.font != null, "Default TMP font missing.");
                label.fontSize = 30f;
                label.color = Color.clear;
                label.raycastTarget = false;
                templateRoot.SetActive(false);
                panel.Configure(rect, template);
                string longText = "";
                for (int i = 0; i < 14; i++) longText += "A complete choice must remain readable. ";
                panel.Show(new[] { new ChoiceOption("a", longText), new ChoiceOption("b", longText) }, id => { });
                List<Button> rows = Rows(rect);
                Require(rows.Count == 2, "Choice count.");
                CheckRows(rows);
                ScrollRect scroll = rect.parent.GetComponent<ScrollRect>();
                Require(scroll != null && scroll.vertical && rect.rect.height > scroll.viewport.rect.height,
                    "Long choices did not enable scrolling.");
                Require(scroll.viewport.GetComponent<RectMask2D>() != null && scroll.verticalScrollbar != null,
                    "Clipping or scrollbar missing.");
                panel.SetInteractionEnabled(false);
                Require(!scroll.enabled && !rows[0].interactable && !scroll.verticalScrollbar.interactable,
                    "Modal interaction lock.");
                panel.SetInteractionEnabled(true);
                Require(scroll.enabled && rows[0].interactable, "Modal interaction resume.");

                picked = 0;
                panel.Show(new[] { new ChoiceOption("a", "Take"), new ChoiceOption("b", "Leave") }, id =>
                {
                    Require(id == "a", "Wrong option ID.");
                    picked++;
                });
                rows = Rows(rect);
                CheckRows(rows);
                Require(!scroll.vertical && rows[0].GetComponent<RectTransform>().rect.height >= 84f,
                    "Short choices retained a long row or scrollbar.");
                selected = rows[0];
                selected.onClick.Invoke();
                Require(panel.IsOpen && picked == 0, "Opening-frame input selected a choice.");
                shownFrame = Time.frameCount;
                deadline = EditorApplication.timeSinceStartup + 5d;
                EditorApplication.update += Tick;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }
            catch (Exception error) { Debug.LogException(error); Cleanup(); }
        }

        private static void CheckRows(List<Button> rows)
        {
            Canvas.ForceUpdateCanvases();
            foreach (Button row in rows)
            {
                TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
                Require(Mathf.Approximately(label.fontSize, 30f) && !label.enableAutoSizing, "Font was shrunk.");
                float needed = label.GetPreferredValues(label.text, label.rectTransform.rect.width, Mathf.Infinity).y;
                Require(needed <= label.rectTransform.rect.height + 0.5f, "Choice text overflows its row.");
            }
            for (int i = 1; i < rows.Count; i++)
            {
                RectTransform above = rows[i - 1].GetComponent<RectTransform>();
                RectTransform below = rows[i].GetComponent<RectTransform>();
                float gap = above.localPosition.y + above.rect.yMin - (below.localPosition.y + below.rect.yMax);
                Require(gap >= 17.5f, "Rows overlap or spacing was lost.");
            }
        }

        private static List<Button> Rows(RectTransform content)
        {
            var rows = new List<Button>();
            foreach (Transform child in content)
                if (child.gameObject.activeSelf && child.name.StartsWith("Option_"))
                    rows.Add(child.GetComponent<Button>());
            return rows;
        }

        private static void Tick()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < deadline, "Timed out waiting for next frame.");
                if (Time.frameCount == shownFrame) return;
                selected.onClick.Invoke();
                Require(picked == 1 && !panel.IsOpen, "Selection did not close exactly once.");
                selected.onClick.Invoke();
                Require(picked == 1, "Stale input picked twice.");
                Debug.Log("[SeaOfMemories] Choice layout checks passed: wrapped row heights, spacing, scroll/clipping, fixed font size, modal lock/resume, short choices and one selection.");
                Cleanup();
            }
            catch (Exception error) { Debug.LogException(error); Cleanup(); }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            host = null;
            panel = null;
            selected = null;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("[SeaOfMemories] Choice layout check failed: " + message);
        }
    }
}
