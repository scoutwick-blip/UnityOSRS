using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using RuneRealm.Core;
using RuneRealm.NPCs;
using System.Collections.Generic;

namespace RuneRealm.UI
{
    /// <summary>
    /// Skyrim-style quest journal with categorized quest list,
    /// objective tracking, and quest details.
    /// </summary>
    public class QuestJournalUI : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private bool isOpen;

        [Header("Quest List")]
        [SerializeField] private Transform questListContainer;
        [SerializeField] private GameObject questEntryPrefab;

        [Header("Quest Detail")]
        [SerializeField] private TextMeshProUGUI questTitleText;
        [SerializeField] private TextMeshProUGUI questDescriptionText;
        [SerializeField] private TextMeshProUGUI questObjectiveText;
        [SerializeField] private TextMeshProUGUI questDifficultyText;
        [SerializeField] private Image questDifficultyIcon;

        [Header("Tabs")]
        [SerializeField] private Button activeQuestsTab;
        [SerializeField] private Button completedQuestsTab;
        [SerializeField] private Button allQuestsTab;

        private QuestFilter currentFilter = QuestFilter.Active;

        private enum QuestFilter { Active, Completed, All }

        private void Start()
        {
            if (menuRoot != null) menuRoot.SetActive(false);

            if (activeQuestsTab != null)
                activeQuestsTab.onClick.AddListener(() => SetFilter(QuestFilter.Active));
            if (completedQuestsTab != null)
                completedQuestsTab.onClick.AddListener(() => SetFilter(QuestFilter.Completed));
            if (allQuestsTab != null)
                allQuestsTab.onClick.AddListener(() => SetFilter(QuestFilter.All));
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.jKey.wasPressedThisFrame)
            {
                ToggleMenu();
            }

            if (isOpen && kb.escapeKey.wasPressedThisFrame)
            {
                CloseMenu();
            }
        }

        public void ToggleMenu()
        {
            if (isOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            isOpen = true;
            if (menuRoot != null) menuRoot.SetActive(true);
            RefreshQuestList();

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.InMenu);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseMenu()
        {
            isOpen = false;
            if (menuRoot != null) menuRoot.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.Playing);
        }

        private void SetFilter(QuestFilter filter)
        {
            currentFilter = filter;
            RefreshQuestList();
        }

        private void RefreshQuestList()
        {
            if (questListContainer == null) return;

            // Clear existing entries
            foreach (Transform child in questListContainer)
                Destroy(child.gameObject);

            // Quest list would be populated from QuestManager data
        }

        public void ShowQuestDetail(string questId)
        {
            var state = QuestManager.Instance?.GetQuestState(questId);
            if (state == null) return;

            // Update detail panel with quest info
        }

        private Color GetDifficultyColor(QuestDifficulty difficulty)
        {
            return difficulty switch
            {
                QuestDifficulty.Novice => new Color(0.4f, 0.8f, 0.4f),
                QuestDifficulty.Intermediate => new Color(0.9f, 0.7f, 0.2f),
                QuestDifficulty.Experienced => new Color(0.9f, 0.4f, 0.1f),
                QuestDifficulty.Master => new Color(0.8f, 0.2f, 0.2f),
                QuestDifficulty.Grandmaster => new Color(0.6f, 0.2f, 0.8f),
                _ => Color.white
            };
        }
    }
}
