using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputUI : MonoBehaviour {
    public Button sendBtn;
    public InputField videoUrlInput;

    void Start() {
        // 绑定发送按钮点击事件
        sendBtn.onClick.AddListener(OnSendButtonClick);
    }

    // 发送按钮点击处理
    private void OnSendButtonClick() {
        string message = videoUrlInput.text.Trim();
        if (!string.IsNullOrEmpty(message)) {
            // 通过事件总线发送消息
            EventManager.TriggerEvent("OnMessageSent", message);

            // 清空输入框
            videoUrlInput.text = "";
        }
    }
}
