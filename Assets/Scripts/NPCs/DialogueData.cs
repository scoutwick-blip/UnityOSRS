using UnityEngine;
using UnityEngine.Events;

namespace RuneRealm.NPCs
{
    /// <summary>
    /// ScriptableObject for dialogue trees.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "RuneRealm/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public string dialogueName;
        public DialogueNode[] nodes;
    }

    [System.Serializable]
    public class DialogueNode
    {
        [TextArea(3, 6)]
        public string text;
        public DialogueChoice[] choices;
        public int nextNodeIndex = -1; // -1 = end dialogue
        public UnityEvent onNodeEnter;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string text;
        public int nextNodeIndex = -1;
        public UnityEvent onSelect;
        public ChoiceRequirement requirement;
    }

    [System.Serializable]
    public class ChoiceRequirement
    {
        public RequirementType type;
        public Skills.SkillType skill;
        public int level;
        public string questId;
    }

    public enum RequirementType
    {
        None,
        SkillLevel,
        QuestComplete,
        HasItem
    }
}
