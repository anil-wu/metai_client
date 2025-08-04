using System;
using System.Collections.Generic;

public static class EventManager {
    private static Dictionary<string, Action<object>> eventDictionary = new Dictionary<string, Action<object>>();

    // 定义事件类型常量
    public const string OnMessageSent = "OnMessageSent";
    public const string AddMessageToChat = "AddMessageToChat";
    public const string SoundToggleEvent = "SoundToggleEvent";
    public const string CallButtonEvent = "CallButtonEvent";
    public const string ConnectionStatusEvent = "ConnectionStatusEvent";
    public const string ConnectingStatusEvent = "ConnectingStatusEvent"; // 新增连接中事件

    public static void StartListening(string eventName, Action<object> listener) {
        if (eventDictionary.TryGetValue(eventName, out Action<object> thisEvent)) {
            thisEvent += listener;
            eventDictionary[eventName] = thisEvent;
        } else {
            eventDictionary.Add(eventName, listener);
        }
    }

    public static void StopListening(string eventName, Action<object> listener) {
        if (eventDictionary.TryGetValue(eventName, out Action<object> thisEvent)) {
            thisEvent -= listener;
            eventDictionary[eventName] = thisEvent;
        }
    }

    public static void TriggerEvent(string eventName, object eventParam) {
        if (eventDictionary.TryGetValue(eventName, out Action<object> thisEvent)) {
            thisEvent?.Invoke(eventParam);
        }
    }
}
