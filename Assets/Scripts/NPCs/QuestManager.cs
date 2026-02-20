using System.Collections.Generic;
using UnityEngine;
using RuneRealm.Core;

namespace RuneRealm.NPCs
{
    /// <summary>
    /// Quest system combining OSRS quest structure with Skyrim journal tracking.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [SerializeField] private QuestDefinition[] allQuests;
        private Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>();

        public event System.Action<string> OnQuestStarted;
        public event System.Action<string> OnQuestCompleted;
        public event System.Action<string, int> OnObjectiveUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeQuests();
        }

        private void InitializeQuests()
        {
            if (allQuests == null) return;

            foreach (var quest in allQuests)
            {
                questStates[quest.questId] = new QuestState
                {
                    questId = quest.questId,
                    status = QuestStatus.NotStarted,
                    currentObjective = 0
                };
            }
        }

        public void StartQuest(string questId)
        {
            if (!questStates.ContainsKey(questId)) return;
            if (questStates[questId].status != QuestStatus.NotStarted) return;

            questStates[questId].status = QuestStatus.InProgress;
            questStates[questId].currentObjective = 0;

            OnQuestStarted?.Invoke(questId);
            EventManager.Publish(GameEvents.QuestStarted, questId);

            UI.HUDManager.Instance?.ShowNotification($"Quest Started: {GetQuestName(questId)}");
        }

        public void AdvanceObjective(string questId)
        {
            if (!questStates.ContainsKey(questId)) return;
            if (questStates[questId].status != QuestStatus.InProgress) return;

            questStates[questId].currentObjective++;

            var quest = GetQuestDefinition(questId);
            if (quest != null && questStates[questId].currentObjective >= quest.objectives.Length)
            {
                CompleteQuest(questId);
            }
            else
            {
                OnObjectiveUpdated?.Invoke(questId, questStates[questId].currentObjective);
                EventManager.Publish(GameEvents.QuestObjectiveUpdated, questId);
            }
        }

        public void CompleteQuest(string questId)
        {
            if (!questStates.ContainsKey(questId)) return;

            questStates[questId].status = QuestStatus.Completed;

            OnQuestCompleted?.Invoke(questId);
            EventManager.Publish(GameEvents.QuestCompleted, questId);

            var quest = GetQuestDefinition(questId);
            UI.HUDManager.Instance?.ShowNotification($"Quest Complete: {quest?.questName ?? questId}");

            // Grant rewards
            if (quest?.rewards != null)
            {
                foreach (var reward in quest.rewards)
                {
                    if (reward.xpAmount > 0)
                    {
                        Skills.SkillManager.Instance?.AddXP(reward.xpRewardSkill, reward.xpAmount);
                    }
                }
            }
        }

        public bool IsQuestComplete(string questId)
        {
            return questStates.ContainsKey(questId) && questStates[questId].status == QuestStatus.Completed;
        }

        public bool IsQuestActive(string questId)
        {
            return questStates.ContainsKey(questId) && questStates[questId].status == QuestStatus.InProgress;
        }

        public QuestState GetQuestState(string questId)
        {
            return questStates.ContainsKey(questId) ? questStates[questId] : null;
        }

        private QuestDefinition GetQuestDefinition(string questId)
        {
            if (allQuests == null) return null;
            foreach (var q in allQuests)
            {
                if (q.questId == questId) return q;
            }
            return null;
        }

        private string GetQuestName(string questId)
        {
            return GetQuestDefinition(questId)?.questName ?? questId;
        }

        public SaveSystem.QuestSaveData[] GetSaveData()
        {
            var dataList = new List<SaveSystem.QuestSaveData>();
            foreach (var kvp in questStates)
            {
                dataList.Add(new SaveSystem.QuestSaveData
                {
                    questId = kvp.Key,
                    stage = kvp.Value.currentObjective,
                    completed = kvp.Value.status == QuestStatus.Completed
                });
            }
            return dataList.ToArray();
        }
    }

    [System.Serializable]
    public class QuestDefinition
    {
        public string questId;
        public string questName;
        [TextArea(2, 4)]
        public string description;
        public int requiredCombatLevel;
        public Inventory.SkillRequirement[] skillRequirements;
        public QuestObjective[] objectives;
        public QuestReward[] rewards;
        public string[] prerequisiteQuests;
        public QuestDifficulty difficulty;
    }

    [System.Serializable]
    public class QuestObjective
    {
        public string description;
        public ObjectiveType type;
        public string targetId;
        public int targetAmount;
    }

    [System.Serializable]
    public class QuestReward
    {
        public Skills.SkillType xpRewardSkill;
        public int xpAmount;
        public Inventory.ItemData itemReward;
        public int itemQuantity;
    }

    [System.Serializable]
    public class QuestState
    {
        public string questId;
        public QuestStatus status;
        public int currentObjective;
    }

    public enum QuestStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    public enum QuestDifficulty
    {
        Novice,
        Intermediate,
        Experienced,
        Master,
        Grandmaster
    }

    public enum ObjectiveType
    {
        TalkToNPC,
        CollectItem,
        ReachLocation,
        SkillCheck,
        DefeatEnemy
    }
}
