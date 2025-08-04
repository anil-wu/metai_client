using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using System.Collections.Generic;

public class MetAI : MonoBehaviour
{
    // UI 组件
    public RawImage videoDisplay;

    // 连接状态UI
    public Text connectionStatusText;

    // WebSocket 认证信息
    public string appkey = ""; // 替换为实际token
    public string versionId = "";

    private string username ; // 用户名
    private string token; // 登录后获得的token

    // TTS 实例
    private TTS tts;

    // WebSocket 管理器
    private WebSocketManager webSocketManager;

    // 音频播放组件
    public AudioSource audioSource;

    // 视频播放组件
    public VideoPlayer videoPlayer;

    // 当前视频完成回调
    private Action _currentVideoFinishedCallback;
    void Start()
    {
        Debug.Log("MetAI starty");
        gameObject.SetActive(false);

        // 注册登录成功事件
        EventManager.StartListening("OnLoginSuccess", HandleLoginSuccess);

        // 初始化 TTS
        tts = new TTS();
        tts.Init();

        // 初始化 WebSocket
        webSocketManager = gameObject.AddComponent<WebSocketManager>();
        webSocketManager.OnMessageReceived += HandleWebSocketMessage;
        webSocketManager.OnConnectionChanged += HandleConnectionChange;
        webSocketManager.OnConnectionProgress += HandleConnectionProgress;


        // 初始化视频组件
        InitializeVideoPlayer();

        // 注册消息接收事件
        EventManager.StartListening("OnMessageSent", HandleMessageReceived);
    }

    void OnDestroy()
    {
        // 取消注册事件
        EventManager.StopListening("OnMessageSent", HandleMessageReceived);
        EventManager.StopListening("OnLoginSuccess", HandleLoginSuccess);
    }

    // 处理登录成功事件
    private void HandleLoginSuccess(object loginDataObj)
    {
        var loginData = loginDataObj as Dictionary<string, object>;
        if (loginData != null)
        {
            // 更新用户信息
            if (loginData.ContainsKey("username"))
                username = loginData["username"].ToString();

            if (loginData.ContainsKey("token"))
                token = loginData["token"].ToString();

            // 显示MetAI界面
            if (gameObject != null)
                gameObject.SetActive(true);

            // 循环播放指定视频
            PlayVideo("idle_6.mp4", true);

            // 启动连接协程
            webSocketManager.Connect("ws://localhost:3002"); // 替换为实际 WebSocket 服务器地址

            // 更新连接状态
            if (connectionStatusText != null)
                connectionStatusText.text = "连接中...";
            }
    }

    // 处理接收到的消息
    private void HandleMessageReceived(object messageObj)
    {
        string message = messageObj as string;
        if (!string.IsNullOrEmpty(message))
        {
            SendMessageToChat(message);
        }
    }

    // 初始化视频播放器
    private void InitializeVideoPlayer()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None; // 不使用单独音频源
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // // 设置视频显示纹理
        if (videoDisplay != null)
        {
            // 创建 RenderTexture
            RenderTexture renderTexture = new RenderTexture(1080, 1920, 24);
            videoPlayer.targetTexture = renderTexture;
            videoDisplay.texture = renderTexture;
        }
    }

    // 发送消息到聊天
    public void SendMessageToChat(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
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
    private void PlayAudioFromBytes(byte[] audioData)
    {
        AudioClip clip = WavUtility.ToAudioClip(audioData);
        audioSource.clip = clip;
        audioSource.Play();
    }

    // WAV 音频工具类
    private static class WavUtility
    {
        public static AudioClip ToAudioClip(byte[] wavData)
        {
            int channels = wavData[22];
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int bitDepth = wavData[34];

            int dataStart = 44;
            for (int i = 0; i < wavData.Length - 4; i++)
            {
                if (wavData[i] == 'd' && wavData[i+1] == 'a' && wavData[i+2] == 't' && wavData[i+3] == 'a')
                {
                    dataStart = i + 8;
                    break;
                }
            }

            int dataSize = wavData.Length - dataStart;
            float[] samples = new float[dataSize / (bitDepth / 8)];

            if (bitDepth == 16)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    int offset = dataStart + i * 2;
                    short sample = BitConverter.ToInt16(wavData, offset);
                    samples[i] = sample / 32768.0f;
                }
            }
            else if (bitDepth == 8)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = (wavData[dataStart + i] - 128) / 128.0f;
                }
            }
            else
            {
                Debug.LogError($"不支持的位深度: {bitDepth}");
                return null;
            }

            AudioClip clip = AudioClip.Create("TTS_Audio", samples.Length / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    // 播放视频接口
    public void PlayVideo(string videoFileName, bool loop, Action onFinished = null)
    {
        // 移除之前注册的所有完成回调
        videoPlayer.loopPointReached -= OnVideoFinished;

        string videoPath = Path.Combine(Application.streamingAssetsPath, "Actions", videoFileName);

        // 设置视频源
        videoPlayer.url = videoPath;
        videoPlayer.isLooping = loop;

        // 保存回调并注册事件处理程序
        _currentVideoFinishedCallback = onFinished;
        videoPlayer.loopPointReached += OnVideoFinished;

        // 准备并播放视频
        videoPlayer.Prepare();
        videoPlayer.Play();
    }

    // 视频完成事件处理
    private void OnVideoFinished(VideoPlayer source)
    {
        // 触发当前视频的回调
        _currentVideoFinishedCallback?.Invoke();

        // 清除当前回调
        _currentVideoFinishedCallback = null;

        // 如果是非空闲视频，播放空闲视频
        if (!videoPlayer.isLooping)
        {
            PlayIdleVideo();
        }
    }

    // 播放空闲视频（不注册完成回调）
    private void PlayIdleVideo()
    {
        // 移除之前注册的所有完成回调
        videoPlayer.loopPointReached -= OnVideoFinished;

        string idlePath = Path.Combine(Application.streamingAssetsPath, "Actions", "idle_6.mp4");
        Debug.Log("播放空闲视频: " + idlePath);

        // 设置空闲视频
        videoPlayer.url = idlePath;
        videoPlayer.isLooping = true; // 空闲视频循环播放

        // 准备并播放视频
        videoPlayer.Prepare();
        videoPlayer.Play();
    }

    // 连接进度处理
    private void HandleConnectionProgress(string progress)
    {
        if (connectionStatusText != null)
            connectionStatusText.text = progress;
    }

    // WebSocket 连接状态变更处理
    private void HandleConnectionChange(bool isConnected)
    {
        Debug.Log(isConnected ? "WebSocket 已连接" : "WebSocket 已断开");

        // 更新连接状态
        if (connectionStatusText != null)
            connectionStatusText.text = isConnected ? "已连接" : "已断开";

        // 连接成功后立即发送认证消息（使用非阻塞方式）
        if (isConnected)
        {
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
    }

    // 定义消息响应数据结构
    [System.Serializable]
    private class MessageResponse
    {
        public string @event;
        public ResponseData data;
    }

    [System.Serializable]
    private class ResponseData
    {
        public string action;
        public string audioUrl;
        public string answer;
        public string emotion;
    }

    // WebSocket 消息处理
    private void HandleWebSocketMessage(string message)
    {
        Debug.Log($"收到消息: {message}");

        try
        {
            // 解析消息响应
            MessageResponse response = JsonUtility.FromJson<MessageResponse>(message);

            // 处理 message_response 事件
            if (response.@event == "message_response")
            {
                string answerText = response.data.answer;
                if (!string.IsNullOrEmpty(answerText))
                {
                    Debug.Log($"收到回复: {answerText}  action: {response.data.action}");

                    // 创建消息数据字典
                    var messageData = new Dictionary<string, object>
                    {
                        { "content", answerText },
                        { "isUserMessage", false }
                    };

                    // 通过事件总线发送角色回复
                    EventManager.TriggerEvent(EventManager.AddMessageToChat, messageData);

                    StartCoroutine(tts.SynthesizeSpeech(answerText, response.data.emotion, PlayAudioFromBytes));
                    PlayVideo(response.data.action + ".mp4", false);
                }
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"消息解析失败: {ex.Message}");
        }
    }

    void Update()
    {
        // 此方法现在留空
    }
}
