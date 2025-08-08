using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using UnityEngine.EventSystems; // 添加 EventSystems 命名空间

public class InputUI : MonoBehaviour {
    public Button sendBtn;
    public InputField videoUrlInput;
    public Button recordBtn; // 录音按钮

    public Button textInput;    // 切换到文本输入按钮
    public Button recordInput;  //切换到语音输入按钮

    public GameObject SoundInputBox;
    public GameObject TextInputBox;


    private bool isRecording = false;
    private AudioClip recordedClip;
    private const int RECORD_LENGTH = 10; // 最大录音长度（秒）
    private const int SAMPLE_RATE = 16000; // 采样率

    void Start() {
        // 绑定发送按钮点击事件
        sendBtn.onClick.AddListener(OnSendButtonClick);

        // 绑定切换按钮事件
        textInput.onClick.AddListener(OnTextInputClick);

        // 为录音按钮添加事件触发器
        EventTrigger trigger = recordBtn.gameObject.AddComponent<EventTrigger>();

        // 添加按下事件
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { StartRecording(); });
        trigger.triggers.Add(pointerDownEntry);

        // 添加松开事件
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { StopRecording(); });
        trigger.triggers.Add(pointerUpEntry);
        recordInput.onClick.AddListener(OnRecordInputClick);


        // 设置初始UI状态
        SetTextInputMode();
    }

    void Update() {
        // Windows平台下空格键录音控制
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (SoundInputBox.activeSelf) {
            if (Input.GetKeyDown(KeyCode.Space)) {
                StartRecording();
            }
            if (Input.GetKeyUp(KeyCode.Space)) {
                StopRecording();
            }
        }
        #endif
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

    // 切换到文本输入模式
    private void OnTextInputClick() {
        SetTextInputMode();
    }

    // 切换到录音输入模式
    private void OnRecordInputClick() {
        SetRecordInputMode();
    }

    // 设置文本输入模式UI
    private void SetTextInputMode() {
        // 显示文本输入相关元素
        SoundInputBox.SetActive(false);
        TextInputBox.SetActive(true);

        recordInput.gameObject.SetActive(true);
        textInput.gameObject.SetActive(false);
    }

    // 设置录音输入模式UI
    private void SetRecordInputMode()
    {
        SoundInputBox.SetActive(true);
        TextInputBox.SetActive(false);

        recordInput.gameObject.SetActive(false);
        textInput.gameObject.SetActive(true);

        // 移除停止按钮的显示逻辑
        recordBtn.gameObject.SetActive(true);
    }

    // 开始录音（当按下录音按钮时调用）
    private void StartRecording() {
        if (isRecording) return;

        // 触发停止 TTS 播放事件
        EventManager.TriggerEvent("StopTTSPlayback", null);

        // 检查麦克风权限
        if (!Microphone.IsRecording(null)) {
            isRecording = true;
            // 开始录音
            recordedClip = Microphone.Start(null, false, RECORD_LENGTH, SAMPLE_RATE);
        }
    }

    // 停止录音并识别（当松开录音按钮时调用）
    private void StopRecording() {
        if (!isRecording) return;
        isRecording = false;
        Microphone.End(null);

        // 显示处理中提示
        videoUrlInput.text = "处理中...";
        videoUrlInput.interactable = false;

        // 启动协程处理音频转换和识别
        StartCoroutine(ProcessAudioData());
    }

    // 处理音频数据的协程
    private IEnumerator ProcessAudioData() {
        // 获取录音数据
        float[] samples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        // 转换为16位PCM格式（WAV）
        byte[] wavData = ConvertToWAV(samples, recordedClip.channels, SAMPLE_RATE);

        // 调用语音识别
        yield return StartCoroutine(RecognizeSpeech(wavData));

        // 恢复输入框状态
        videoUrlInput.interactable = true;
    }

    // 语音识别协程
    private IEnumerator RecognizeSpeech(byte[] audioData) {
        string recognizedText = null;

        yield return ASR.Instance.RecognizeSpeech(audioData, (text) => {
            recognizedText = text;
        });

        if (!string.IsNullOrEmpty(recognizedText))
        {
            // 将识别结果填入输入框
            videoUrlInput.text = recognizedText;
            OnSendButtonClick();
        }
    }

    // 将音频数据转换为WAV格式（优化版）
    private byte[] ConvertToWAV(float[] samples, int channels, int sampleRate) {
        // 预计算所需缓冲区大小
        int dataSize = samples.Length * 2;
        int fileSize = 36 + dataSize;

        // 使用MemoryStream预分配空间
        using (MemoryStream stream = new MemoryStream(44 + dataSize)) {
            // 使用BinaryWriter提高写入效率
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                // RIFF头
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(fileSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                // fmt块
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // fmt块大小
                writer.Write((ushort)1); // PCM格式
                writer.Write((ushort)channels); // 声道数
                writer.Write(sampleRate); // 采样率
                writer.Write(sampleRate * channels * 2); // 字节率
                writer.Write((ushort)(channels * 2)); // 块对齐
                writer.Write((ushort)16); // 位深度

                // data块
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize); // 数据大小

                // 音频数据（16位PCM）
                foreach (float sample in samples) {
                    short pcmSample = (short)(sample * short.MaxValue);
                    writer.Write(pcmSample);
                }
            }

            return stream.ToArray();
        }
    }
}
