using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeaOfMemories
{
    [Serializable]
    public sealed class ScenarioFile
    {
        public ScenarioSequence[] sequences;
    }

    [Serializable]
    public sealed class ScenarioSequence
    {
        public string id;
        public ScenarioNode[] nodes;
        public string nextSequenceId;
    }

    [Serializable]
    public sealed class ScenarioNode
    {
        public string type;
        public string speaker;
        public string text;
        public string expressionId;
        public string portraitAction;
        public string speakerName;
        public string backgroundId;
        public string seId;
        public string bgmId;
        public string screenEffectId;
        public ChoiceOptionData[] options;
    }

    [Serializable]
    public sealed class ChoiceOptionData
    {
        public string id;
        public string label;
        public string nextSequenceId;
    }

    public sealed class ScenarioRepository : MonoBehaviour
    {
        private readonly Dictionary<string, ScenarioSequence> sequences =
            new Dictionary<string, ScenarioSequence>();
        public bool HasChoices { get; private set; }
        public bool HasExpressions { get; private set; }
        public bool HasBackgroundCommands { get; private set; }
        public bool HasAudioCommands { get; private set; }

        public bool Load(TextAsset json) { return LoadAll(new[] { json }); }

        public bool LoadAll(TextAsset[] files)
        {
            sequences.Clear();
            HasChoices = false;
            HasExpressions = false;
            HasBackgroundCommands = false;
            HasAudioCommands = false;
            if (files == null || files.Length == 0) return Fail("No scenario files assigned.");
            var combined = new List<ScenarioSequence>();
            foreach (TextAsset json in files)
            {
                if (json == null) return Fail("A scenario file is missing.");
                ScenarioFile parsed;
                try { parsed = JsonUtility.FromJson<ScenarioFile>(json.text); }
                catch (Exception error) { return Fail(json.name + ": " + error.Message); }
                if (parsed == null || parsed.sequences == null || parsed.sequences.Length == 0)
                    return Fail("Scenario has no sequences: " + json.name);
                combined.AddRange(parsed.sequences);
            }
            var file = new ScenarioFile { sequences = combined.ToArray() };

            foreach (ScenarioSequence sequence in file.sequences)
            {
                if (sequence == null || string.IsNullOrWhiteSpace(sequence.id))
                    return Fail("A sequence has no ID.");
                if (sequences.ContainsKey(sequence.id))
                    return Fail("Duplicate sequence ID: " + sequence.id);
                if (sequence.nodes == null || sequence.nodes.Length == 0)
                    return Fail("Sequence has no nodes: " + sequence.id);

                foreach (ScenarioNode node in sequence.nodes)
                {
                    if (node == null || (node.type != "line" && node.type != "choice"))
                        return Fail("Unknown node type in: " + sequence.id);
                    if (!string.IsNullOrEmpty(node.portraitAction) && node.portraitAction != "show" && node.portraitAction != "hide")
                        return Fail("Unknown portraitAction in: " + sequence.id);
                    if (node.type == "line")
                    {
                        if (node.text == null)
                            return Fail("A line has no text: " + sequence.id);
                        if (node.speaker != "hero" && node.speaker != "guard" && node.speaker != "narration")
                            return Fail("Unknown speaker: " + node.speaker);
                    }
                    else
                    {
                        if (node.options == null || node.options.Length == 0)
                            return Fail("A choice has no options: " + sequence.id);
                        var optionIds = new HashSet<string>();
                        foreach (ChoiceOptionData option in node.options)
                        {
                            if (option == null || string.IsNullOrWhiteSpace(option.id)
                                || string.IsNullOrWhiteSpace(option.label)
                                || string.IsNullOrWhiteSpace(option.nextSequenceId))
                                return Fail("A choice option needs an ID, label and destination: " + sequence.id);
                            if (!optionIds.Add(option.id))
                                return Fail("Duplicate option ID in " + sequence.id + ": " + option.id);
                        }
                    }
                    if ((!string.IsNullOrEmpty(node.screenEffectId) && node.screenEffectId != "NONE"
                            && node.screenEffectId != "GLASS_SHAKE"
                            && node.screenEffectId != "BLACK_FADE" && node.screenEffectId != "WHITE_FADE"))
                        return Fail("Unknown screen effect: " + sequence.id);
                    if (!string.IsNullOrEmpty(node.backgroundId) && !ContentCatalog.IsBackgroundId(node.backgroundId))
                        return Fail("Unknown background ID in: " + sequence.id);
                    if ((!string.IsNullOrEmpty(node.bgmId) && !ContentCatalog.IsBgmId(node.bgmId))
                        || (!string.IsNullOrEmpty(node.seId) && !ContentCatalog.IsSeId(node.seId)))
                        return Fail("Unknown or retired audio command in: " + sequence.id);
                }
                sequences.Add(sequence.id, sequence);
            }

            foreach (ScenarioSequence sequence in sequences.Values)
            {
                foreach (ScenarioNode node in sequence.nodes)
                    if (node.type == "choice")
                        foreach (ChoiceOptionData option in node.options)
                            if (!sequences.ContainsKey(option.nextSequenceId))
                                return Fail("Missing choice destination: " + option.nextSequenceId);

                var visited = new HashSet<string>();
                ScenarioSequence current = sequence;
                while (current != null)
                {
                    if (!visited.Add(current.id))
                        return Fail("Automatic sequence cycle at: " + current.id);
                    if (string.IsNullOrEmpty(current.nextSequenceId))
                        break;
                    if (!sequences.TryGetValue(current.nextSequenceId, out current))
                        return Fail("A nextSequenceId does not exist.");
                }
            }
            foreach (ScenarioSequence sequence in sequences.Values)
                foreach (ScenarioNode node in sequence.nodes)
                {
                    if (node.type == "choice") HasChoices = true;
                    if (!string.IsNullOrEmpty(node.expressionId)) HasExpressions = true;
                    if (!string.IsNullOrEmpty(node.backgroundId)) HasBackgroundCommands = true;
                    if (!string.IsNullOrEmpty(node.seId) || !string.IsNullOrEmpty(node.bgmId)) HasAudioCommands = true;
                }
            return true;
        }

        public IEnumerable<string> GetExpressionIds()
        {
            foreach (ScenarioSequence sequence in sequences.Values)
                foreach (ScenarioNode node in sequence.nodes)
                    if (!string.IsNullOrEmpty(node.expressionId)) yield return node.expressionId;
        }

        public IEnumerable<string> GetAudioIds()
        {
            foreach (ScenarioSequence sequence in sequences.Values)
                foreach (ScenarioNode node in sequence.nodes)
                {
                    if (!string.IsNullOrEmpty(node.bgmId)) yield return node.bgmId;
                    if (!string.IsNullOrEmpty(node.seId)) yield return node.seId;
                }
        }

        public bool HasSequence(string id) { return !string.IsNullOrEmpty(id) && sequences.ContainsKey(id); }

        public ScenarioSequence Find(string sequenceId)
        {
            if (!string.IsNullOrEmpty(sequenceId) && sequences.TryGetValue(sequenceId, out var result))
                return result;
            Debug.LogError("[SeaOfMemories] Missing sequence: " + sequenceId, this);
            return null;
        }

        private bool Fail(string message)
        {
            sequences.Clear();
            HasChoices = false;
            HasExpressions = false;
            HasBackgroundCommands = false;
            HasAudioCommands = false;
            Debug.LogError("[SeaOfMemories] " + message, this);
            return false;
        }
    }
}
