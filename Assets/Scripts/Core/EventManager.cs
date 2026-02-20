using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuneRealm.Core
{
    /// <summary>
    /// Global event bus for decoupled communication between game systems.
    /// </summary>
    public static class EventManager
    {
        private static readonly Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();

        public static void Subscribe(string eventName, Action handler)
        {
            if (eventTable.ContainsKey(eventName))
                eventTable[eventName] = Delegate.Combine(eventTable[eventName], handler);
            else
                eventTable[eventName] = handler;
        }

        public static void Subscribe<T>(string eventName, Action<T> handler)
        {
            if (eventTable.ContainsKey(eventName))
                eventTable[eventName] = Delegate.Combine(eventTable[eventName], handler);
            else
                eventTable[eventName] = handler;
        }

        public static void Subscribe<T1, T2>(string eventName, Action<T1, T2> handler)
        {
            if (eventTable.ContainsKey(eventName))
                eventTable[eventName] = Delegate.Combine(eventTable[eventName], handler);
            else
                eventTable[eventName] = handler;
        }

        public static void Unsubscribe(string eventName, Action handler)
        {
            if (eventTable.ContainsKey(eventName))
            {
                eventTable[eventName] = Delegate.Remove(eventTable[eventName], handler);
                if (eventTable[eventName] == null)
                    eventTable.Remove(eventName);
            }
        }

        public static void Unsubscribe<T>(string eventName, Action<T> handler)
        {
            if (eventTable.ContainsKey(eventName))
            {
                eventTable[eventName] = Delegate.Remove(eventTable[eventName], handler);
                if (eventTable[eventName] == null)
                    eventTable.Remove(eventName);
            }
        }

        public static void Unsubscribe<T1, T2>(string eventName, Action<T1, T2> handler)
        {
            if (eventTable.ContainsKey(eventName))
            {
                eventTable[eventName] = Delegate.Remove(eventTable[eventName], handler);
                if (eventTable[eventName] == null)
                    eventTable.Remove(eventName);
            }
        }

        public static void Publish(string eventName)
        {
            if (eventTable.TryGetValue(eventName, out Delegate handler))
                (handler as Action)?.Invoke();
        }

        public static void Publish<T>(string eventName, T arg)
        {
            if (eventTable.TryGetValue(eventName, out Delegate handler))
                (handler as Action<T>)?.Invoke(arg);
        }

        public static void Publish<T1, T2>(string eventName, T1 arg1, T2 arg2)
        {
            if (eventTable.TryGetValue(eventName, out Delegate handler))
                (handler as Action<T1, T2>)?.Invoke(arg1, arg2);
        }

        public static void Clear()
        {
            eventTable.Clear();
        }
    }

    // Predefined event names
    public static class GameEvents
    {
        // Skill events
        public const string SkillXPGained = "Skill.XPGained";
        public const string SkillLevelUp = "Skill.LevelUp";
        public const string SkillingStarted = "Skill.Started";
        public const string SkillingStopped = "Skill.Stopped";
        public const string SkillingTick = "Skill.Tick";

        // Inventory events
        public const string ItemAdded = "Inventory.ItemAdded";
        public const string ItemRemoved = "Inventory.ItemRemoved";
        public const string InventoryFull = "Inventory.Full";
        public const string ItemEquipped = "Inventory.ItemEquipped";
        public const string ItemUnequipped = "Inventory.ItemUnequipped";

        // Resource events
        public const string ResourceDepleted = "Resource.Depleted";
        public const string ResourceRespawned = "Resource.Respawned";
        public const string ResourceHarvested = "Resource.Harvested";

        // Player events
        public const string PlayerInteract = "Player.Interact";
        public const string PlayerMoved = "Player.Moved";
        public const string PlayerStaminaChanged = "Player.StaminaChanged";

        // UI events
        public const string UIMenuOpened = "UI.MenuOpened";
        public const string UIMenuClosed = "UI.MenuClosed";
        public const string UINotification = "UI.Notification";

        // World events
        public const string TimeOfDayChanged = "World.TimeChanged";
        public const string WeatherChanged = "World.WeatherChanged";
        public const string ZoneEntered = "World.ZoneEntered";

        // Quest events
        public const string QuestStarted = "Quest.Started";
        public const string QuestCompleted = "Quest.Completed";
        public const string QuestObjectiveUpdated = "Quest.ObjectiveUpdated";
    }
}
