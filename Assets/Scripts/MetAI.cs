using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using System.Collections.Generic;

public class MetAI : MonoBehaviour {
    // UI 组件（videoDisplay 已移除）

    // 连接状态UI
    public Text connectionStatusText;

    // 角色2D控制器
    public Role2D role2d;

    // WebSocket 认证信息
    public string appkey = ""; // 替换为实际token
    public string versionId = "";

    private string username; // 用户名
    private string token; // 登录后获得的token

    // TTS 实例
    private TTS tts;

    // WebSocket 管理器
    private WebSocketManager webSocketManager;

    // 音频播放组件
    public AudioSource audioSource;

    // 角色声音开关状态
    private bool isCharacterSoundOn = true;

    // 视频播放组件已移除（由 Role2D 替代）

    void Start()
    {
        Debug.Log("MetAI starty");
        gameObject.SetActive(false);

        // 注册登录成功事件
        EventManager.StartListening("OnLoginSuccess", HandleLoginSuccess);

        // 注册声音切换事件
        EventManager.StartListening(EventManager.SoundToggleEvent, HandleSoundToggle);
        // 注册呼叫按钮事件
        EventManager.StartListening(EventManager.CallButtonEvent, HandleCallButton);

        // 初始化 TTS
        tts = new TTS();
        tts.Init();

        // 初始化 WebSocket
        webSocketManager = gameObject.AddComponent<WebSocketManager>();
        webSocketManager.OnMessageReceived += HandleWebSocketMessage;
        webSocketManager.OnConnectionChanged += HandleConnectionChange;
        webSocketManager.OnConnectionProgress += HandleConnectionProgress;

        // 视频组件初始化已移除（由 Role2D 替代）

        // 注册消息接收事件
        EventManager.StartListening("OnMessageSent", HandleMessageReceived);
        HandleConnectionChange(false);
    }

    void OnDestroy() {
        // 取消注册事件
        EventManager.StopListening("OnMessageSent", HandleMessageReceived);
        EventManager.StopListening("OnLoginSuccess", HandleLoginSuccess);
        EventManager.StopListening(EventManager.SoundToggleEvent, HandleSoundToggle);
        EventManager.StopListening(EventManager.CallButtonEvent, HandleCallButton);
    }

    // 处理登录成功事件
    private void HandleLoginSuccess(object loginDataObj) {
        var loginData = loginDataObj as Dictionary<string, object>;
        if (loginData != null) {
            // 更新用户信息
            if (loginData.ContainsKey("username")) {
                username = loginData["username"].ToString();
            }

            if (loginData.ContainsKey("token")) {
                token = loginData["token"].ToString();
            }

            // 显示MetAI界面
            if (gameObject != null) {
                gameObject.SetActive(true);
            }

            // 使用Role2D播放待机动作
            if (role2d != null) {
                role2d.PlayAction("idle_6.mp4");
            }

            // 启动连接协程
            // ConnectWebSocket(); // 替换为实际 WebSocket 服务器地址

        }
    }

    // 处理接收到的消息
    private void HandleMessageReceived(object messageObj) {
        string message = messageObj as string;
        if (!string.IsNullOrEmpty(message)) {
            SendMessageToChat(message);
        }
    }

    // 视频播放器初始化方法已移除（由 Role2D 替代）

    // 发送消息到聊天
    public void SendMessageToChat(string message) {
        if (!string.IsNullOrEmpty(message)) {
            // 创建消息数据字典
            var messageData = new Dictionary<string, object>
            {
                { "content", message },
                { "isUserMessage", true }
            };

            // 通过事件总线发送用户消息
            EventManager.TriggerEvent(EventManager.AddMessageToChat, messageData);

            // 通过WebSocket发送消息
            string messageJson = $@"{{
                ""event"": ""message"",
                ""data"": {{
                    ""content"": ""{message}""
                }}
            }}";
            webSocketManager.SendMessage(messageJson);
            Debug.Log("已发送消息: " + message);
        }
    }

    // 播放音频
    private void PlayAudioFromBytes(byte[] audioData) {
        AudioClip clip = WavUtility.ToAudioClip(audioData);
        audioSource.clip = clip;
        audioSource.Play();
    }

    // 处理声音切换事件
    private void HandleSoundToggle(object isSoundOnObj) {
        if (isSoundOnObj is bool isSoundOn) {
            // 设置音频源的静音状态（当isSoundOn为false时静音）
            audioSource.mute = !isSoundOn;
            // 更新角色声音开关状态
            isCharacterSoundOn = isSoundOn;
        }
    }

    // 处理呼叫按钮事件
    private void HandleCallButton(object param) {
        // 重新连接WebSocket
        ConnectWebSocket();
    }

    // 连接WebSocket
    private void ConnectWebSocket() {
        webSocketManager.Connect("ws://localhost:3002");
    }

    // WAV 音频工具类
    private static class WavUtility {
        public static AudioClip ToAudioClip(byte[] wavData) {
            int channels = wavData[22];
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int bitDepth = wavData[34];

            int dataStart = 44;
            for (int i = 0; i < wavData.Length - 4; i++) {
                if (wavData[i] == 'd' && wavData[i+1] == 'a' && wavData[i+2] == 't' && wavData[i+3] == 'a') {
                    dataStart = i + 8;
                    break;
                }
            }

            int dataSize = wavData.Length - dataStart;
            float[] samples = new float[dataSize / (bitDepth / 8)];

            if (bitDepth == 16) {
                for (int i = 0; i < samples.Length; i++) {
                    int offset = dataStart + i * 2;
                    short sample = BitConverter.ToInt16(wavData, offset);
                    samples[i] = sample / 32768.0f;
                }
            } else if (bitDepth == 8) {
                for (int i = 0; i < samples.Length; i++) {
                    samples[i] = (wavData[dataStart + i] - 128) / 128.0f;
                }
            } else {
                Debug.LogError($"不支持的位深度: {bitDepth}");
                return null;
            }

            AudioClip clip = AudioClip.Create("TTS_Audio", samples.Length / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    // 视频播放相关方法已移除（由 Role2D 替代）

    // 连接进度处理
    private void HandleConnectionProgress(string progress) {
        // 触发连接中状态事件
        EventManager.TriggerEvent(EventManager.ConnectingStatusEvent, progress);
    }

    // WebSocket 连接状态变更处理
    private void HandleConnectionChange(bool isConnected) {
        Debug.Log(isConnected ? "WebSocket 已连接" : "WebSocket 已断开");

        // 更新连接状态
        if (connectionStatusText != null) {
            connectionStatusText.text = isConnected ? "已连接" : "已断开";
        }

        // 控制角色明暗
        if (role2d != null) {
            role2d.SetBrightness(isConnected ? 1f : 0.2f);
        }

        // 连接成功后立即发送认证消息（使用非阻塞方式）
        if (isConnected) {
            string authMessage = $@"{{
                ""event"": ""auth"",
                ""data"": {{
                    ""appkey"": ""{appkey}"",
                    ""token"": ""{token}"",
                    ""versionId"": ""{versionId}""
                }}
            }}";

            webSocketManager.SendMessage(authMessage);
            Debug.Log("已发送认证消息");
        }

        // 发送连接状态变更事件
        EventManager.TriggerEvent(EventManager.ConnectionStatusEvent, isConnected);
    }

    // 定义消息响应数据结构
    [System.Serializable]
    private class MessageResponse {
        public string @event;
        public ResponseData data;
    }

    [System.Serializable]
    private class ResponseData {
        public string action;
        public string audioUrl;
        public string answer;
        public string emotion;
    }

    // WebSocket 消息处理
    private void HandleWebSocketMessage(string message) {
        Debug.Log($"收到消息: {message}");

        try {
            // 解析消息响应
            MessageResponse response = JsonUtility.FromJson<MessageResponse>(message);

            // 处理 message_response 事件
            if (response.@event == "message_response") {
                string answerText = response.data.answer;
                if (!string.IsNullOrEmpty(answerText)) {
                    Debug.Log($"收到回复: {answerText}  action: {response.data.action}");

                    // 创建消息数据字典
                    var messageData = new Dictionary<string, object>
                    {
                        { "content", answerText },
                        { "isUserMessage", false }
                    };

                    // 通过事件总线发送角色回复
                    EventManager.TriggerEvent(EventManager.AddMessageToChat, messageData);

                    // 根据角色声音开关状态决定是否播放语音
                    if (isCharacterSoundOn) {
                        StartCoroutine(tts.SynthesizeSpeech(answerText, response.data.emotion, PlayAudioFromBytes));
                    }
                    // 使用Role2D播放动作
                    if (role2d != null) {
                        role2d.PlayAction(response.data.action + ".mp4");
                    }
                }
                return;
            }
        } catch (Exception ex) {
            Debug.LogWarning($"消息解析失败: {ex.Message}");
        }
    }

    void Update() {
        // 此方法现在留空
    }
}
