using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using System.Collections.Generic;

public class MteaAI : MonoBehaviour
{
    // UI 组件
    public InputField ttsInputField;
    public Button sendBtn;
    public InputField videoUrlInput;
    public RawImage videoDisplay;

    // 连接状态UI
    public Text connectionStatusText;

    // WebSocket 认证信息
    public string token = "npc_sk_OtMbpcwNepjvF1E4PQfxDyB0qA0b0NMi"; // 替换为实际token

    // TTS 实例
    private TTS tts;

    // WebSocket 管理器
    private WebSocketManager webSocketManager;

    // 音频播放组件
    public AudioSource audioSource;

    // 视频播放组件
    public VideoPlayer videoPlayer;
    void Start()
    {
        // 初始化 TTS
        tts = new TTS();
        tts.Init();

        // 初始化 WebSocket
        webSocketManager = gameObject.AddComponent<WebSocketManager>();
        webSocketManager.OnMessageReceived += HandleWebSocketMessage;
        webSocketManager.OnConnectionChanged += HandleConnectionChange;
        webSocketManager.OnConnectionProgress += HandleConnectionProgress;

        // 启动连接协程
        webSocketManager.Connect("ws://localhost:3002"); // 替换为实际 WebSocket 服务器地址

        // 更新连接状态
        if (connectionStatusText != null)
            connectionStatusText.text = "连接中...";

        // 初始化视频组件
        InitializeVideoPlayer();

        // 绑定按钮事件
        sendBtn.onClick.AddListener(OnsendBtnClick);

        // 循环播放指定视频
        PlayVideo("idle_6.mp4", true);
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

    // TTS 按钮点击事件
    private void OnsendBtnClick()
    {
        string inputMessage = ttsInputField.text;
        if (!string.IsNullOrEmpty(inputMessage))
        {
            string messageJson = $@"{{
                ""event"": ""message"",
                ""data"": {{
                    ""characterId"": ""ccac5baa-9468-4a7e-b86c-e19d3460fc3f"",
                    ""content"": ""{inputMessage}""
                }}
            }}";
            webSocketManager.SendMessage(messageJson);
            Debug.Log("已发送消息: " + inputMessage);

            // 清空输入框
            ttsInputField.text = "";
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

        string videoPath = Path.Combine(Application.streamingAssetsPath, "videos", videoFileName);
        // 设置视频源
        videoPlayer.url = videoPath;
        videoPlayer.isLooping = loop;

        // 设置完成回调
        videoPlayer.loopPointReached += (VideoPlayer source) =>
        {
            onFinished?.Invoke();
            PlayVideo("idle_6.mp4", true);
        };

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
                    ""appkey"": ""{token}"",
                    ""userId"": ""Anil_1987"",
                    ""username"": ""Anil"",
                    ""characterId"": ""ccac5baa-9468-4a7e-b86c-e19d3460fc3f"",
                    ""version"": ""v0""
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
