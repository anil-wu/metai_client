using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;

public class MteaAI : MonoBehaviour
{
    // UI 组件
    public InputField ttsInputField;
    public Button ttsButton;
    public InputField videoUrlInput;
    public RawImage videoDisplay;

    // TTS 实例
    private TTS tts;

    // 音频播放组件
    public AudioSource audioSource;

    // 视频播放组件
    public VideoPlayer videoPlayer;

    void Start()
    {
        // 初始化 TTS
        tts = new TTS();
        tts.Init();

        // 初始化视频组件
        InitializeVideoPlayer();

        // 绑定按钮事件
        ttsButton.onClick.AddListener(OnTTSButtonClick);

        // 循环播放指定视频
        PlayVideo("tallk_1.mp4", true);
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
    private void OnTTSButtonClick()
    {
        string text = ttsInputField.text;
        if (!string.IsNullOrEmpty(text))
        {
            StartCoroutine(tts.SynthesizeSpeech(text, PlayAudioFromBytes));
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
        videoPlayer.loopPointReached += (VideoPlayer source) => {
            onFinished?.Invoke();
        };

        // 准备并播放视频
        videoPlayer.Prepare();
        videoPlayer.Play();
    }

    void Update()
    {
        // 此方法现在留空
    }
}
