using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global event notification system.
/// Used for decoupled communication between components (e.g., weapon fire notifications to AI).
/// Persists across scene loads.
/// </summary>
public class NotificationsManager : MonoBehaviour
{
    private readonly Dictionary<string, List<Component>> _listeners = new Dictionary<string, List<Component>>();

    private static NotificationsManager _instance;
    public static NotificationsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("NotificationsManager");
                _instance = obj.AddComponent<NotificationsManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Register a component to listen for a specific notification.
    /// </summary>
    public void AddListener(Component sender, string notificationName)
    {
        if (!_listeners.ContainsKey(notificationName))
        {
            _listeners.Add(notificationName, new List<Component>());
        }
        if (!_listeners[notificationName].Contains(sender))
        {
            _listeners[notificationName].Add(sender);
        }
    }

    /// <summary>
    /// Unregister a component from a notification.
    /// </summary>
    public void RemoveListener(Component sender, string notificationName)
    {
        if (!_listeners.ContainsKey(notificationName))
            return;

        for (int i = _listeners[notificationName].Count - 1; i >= 0; i--)
        {
            if (_listeners[notificationName][i] == sender)
            {
                _listeners[notificationName].RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Send a notification to all registered listeners.
    /// </summary>
    public void PostNotification(Component sender, string notificationName)
    {
        if (!_listeners.ContainsKey(notificationName))
            return;

        foreach (var listener in _listeners[notificationName])
        {
            if (listener != null)
            {
                listener.SendMessage(notificationName, sender, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    /// <summary>
    /// Clear all listeners.
    /// </summary>
    public void ClearListeners()
    {
        _listeners.Clear();
    }

    /// <summary>
    /// Remove null references from listener lists.
    /// Called automatically on level load.
    /// </summary>
    public void RemoveRedundancies()
    {
        var cleanDict = new Dictionary<string, List<Component>>();
        foreach (var kvp in _listeners)
        {
            var validListeners = new List<Component>();
            foreach (var listener in kvp.Value)
            {
                if (listener != null)
                {
                    validListeners.Add(listener);
                }
            }
            if (validListeners.Count > 0)
            {
                cleanDict.Add(kvp.Key, validListeners);
            }
        }
        _listeners.Clear();
        foreach (var kvp in cleanDict)
        {
            _listeners.Add(kvp.Key, kvp.Value);
        }
    }

    private void OnLevelWasLoaded(int level)
    {
        RemoveRedundancies();
    }
}