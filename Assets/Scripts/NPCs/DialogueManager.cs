using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RuneRealm.Core;
using System.Collections;
using System.Collections.Generic;

namespace RuneRealm.NPCs
{
    /// <summary>
    /// Skyrim-style dialogue system with choices and quest integration.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup dialogueGroup;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI npcNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Settings")]
        [SerializeField] private float typewriterSpeed = 0.03f;
        [SerializeField] private bool useTypewriter = true;

        private NPCController currentNPC;
        private DialogueData currentDialogue;
        private int currentNodeIndex;
        private bool isTyping;
        private Coroutine typewriterCoroutine;
        private string fullText;

        public bool IsInDialogue => currentNPC != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        private void Update()
        {
            if (!IsInDialogue) return;

            // Skip typewriter or advance dialogue
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                if (isTyping)
                {
                    // Skip to full text
                    StopTypewriter();
                    dialogueText.text = fullText;
                }
                else if (currentDialogue != null)
                {
                    var node = currentDialogue.nodes[currentNodeIndex];
                    if (node.choices == null || node.choices.Length == 0)
                    {
                        if (node.nextNodeIndex >= 0)
                            ShowNode(node.nextNodeIndex);
                        else
                            EndDialogue();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EndDialogue();
            }
        }

        public void StartDialogue(NPCController npc, DialogueData dialogue)
        {
            currentNPC = npc;
            currentDialogue = dialogue;
            currentNodeIndex = 0;

            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (dialogueGroup != null) dialogueGroup.alpha = 1f;

            if (npcNameText != null)
                npcNameText.text = npc.NPCName;

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.Dialogue);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ShowNode(0);
        }

        private void ShowNode(int nodeIndex)
        {
            if (currentDialogue == null || nodeIndex < 0 || nodeIndex >= currentDialogue.nodes.Length)
            {
                EndDialogue();
                return;
            }

            currentNodeIndex = nodeIndex;
            var node = currentDialogue.nodes[nodeIndex];

            fullText = node.text;

            // Clear choices
            ClearChoices();

            // Show text
            if (useTypewriter)
            {
                typewriterCoroutine = StartCoroutine(TypewriterEffect(node.text));
            }
            else
            {
                dialogueText.text = node.text;
                ShowChoices(node);
            }
        }

        private IEnumerator TypewriterEffect(string text)
        {
            isTyping = true;
            dialogueText.text = "";

            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSecondsRealtime(typewriterSpeed);
            }

            isTyping = false;
            ShowChoices(currentDialogue.nodes[currentNodeIndex]);
        }

        private void StopTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            isTyping = false;
        }

        private void ShowChoices(DialogueNode node)
        {
            if (node.choices == null || node.choices.Length == 0) return;
            if (choiceButtonPrefab == null || choicesContainer == null) return;

            foreach (var choice in node.choices)
            {
                GameObject buttonGO = Instantiate(choiceButtonPrefab, choicesContainer);
                var buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = choice.text;

                var button = buttonGO.GetComponent<Button>();
                int targetNode = choice.nextNodeIndex;
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        if (choice.onSelect != null)
                            choice.onSelect.Invoke();

                        if (targetNode >= 0)
                            ShowNode(targetNode);
                        else
                            EndDialogue();
                    });
                }
            }
        }

        private void ClearChoices()
        {
            if (choicesContainer == null) return;

            foreach (Transform child in choicesContainer)
            {
                Destroy(child.gameObject);
            }
        }

        public void EndDialogue()
        {
            StopTypewriter();

            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (dialogueGroup != null) dialogueGroup.alpha = 0f;

            ClearChoices();

            if (currentNPC != null)
            {
                currentNPC.EndInteraction();
                currentNPC = null;
            }

            currentDialogue = null;

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.Playing);
        }
    }
}
