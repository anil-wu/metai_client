using System;
using System.Collections;
using UnityEngine;
using System.IO;

public class main : MonoBehaviour
{
    [Header("TTS Configuration")]
    public TTS tts; // TTS实例

    [Tooltip("要合成的文本内容")]
    public string textContent = "今天天气真不错！"; // 要合成的文本内容


    [Header("Audio Settings")]
    public AudioSource audioSource; // 用于播放音频的组件

    void Start()
    {
        // 确保audioSource有效
        if (audioSource == null)
        {
            // 尝试获取现有的AudioSource组件
            audioSource = GetComponent<AudioSource>();

            // 如果没有找到，则添加新的AudioSource组件
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("自动添加了AudioSource组件");
            }
        }

        // 初始化TTS实例
        tts = new TTS();
        tts.Init(); // 必须在Start中初始化路径
    }

    void Update()
    {
        // 按空格键触发TTS
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(ProcessTTSRequest());
        }
    }

    // 处理TTS请求的协程
    IEnumerator ProcessTTSRequest()
    {
        Debug.Log("开始TTS请求...");

        // 使用TTS实例合成语音
        yield return StartCoroutine(tts.SynthesizeSpeech(textContent));

        // 加载并播放生成的音频
        yield return StartCoroutine(LoadAndPlayAudio(tts.AudioSavePath));
    }


    // 加载并播放音频
    IEnumerator LoadAndPlayAudio(string path)
    {
        // 再次确保audioSource有效
        if (audioSource == null)
        {
            Debug.LogError("audioSource未分配且无法自动创建!");
            yield break;
        }

        // 检查文件是否存在
        if (!File.Exists(path))
        {
            Debug.LogError($"音频文件不存在: {path}");
            yield break;
        }

        // 创建URL (file:// 用于本地文件)
        string url = "file://" + path;
        Debug.Log($"加载音频: {url}");

        // 明确指定音频格式为WAV
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log($"正在播放TTS音频 (时长: {clip.length}秒)");
            }
            else
            {
                Debug.LogError($"音频加载失败: {www.error}");

                // 尝试读取文件内容作为文本，用于诊断
                try
                {
                    string fileContent = File.ReadAllText(path);
                    Debug.Log($"音频文件内容(文本): {fileContent.Substring(0, System.Math.Min(fileContent.Length, 100))}...");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"读取音频文件失败: {ex.Message}");
                }
            }
        }
    }
}
