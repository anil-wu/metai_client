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
        // 移除按钮状态切换逻辑
        isRecording = false;
        // 停止录音
        Microphone.End(null);
        // 获取录音数据
        float[] samples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(samples, 0);
        // 转换为16位PCM格式（WAV）
        byte[] wavData = ConvertToWAV(samples, recordedClip.channels, SAMPLE_RATE);
        // 调用语音识别
        StartCoroutine(RecognizeSpeech(wavData));
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

    // 将音频数据转换为WAV格式
    private byte[] ConvertToWAV(float[] samples, int channels, int sampleRate) {
        using (MemoryStream stream = new MemoryStream()) {
            // WAV文件头
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            stream.Write(BitConverter.GetBytes(36 + samples.Length * 2), 0, 4); // 文件大小
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
            stream.Write(BitConverter.GetBytes(16), 0, 4); // fmt块大小
            stream.Write(BitConverter.GetBytes((ushort)1), 0, 2); // PCM格式
            stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2); // 声道数
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4); // 采样率
            stream.Write(BitConverter.GetBytes(sampleRate * channels * 2), 0, 4); // 字节率
            stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2); // 块对齐
            stream.Write(BitConverter.GetBytes((ushort)16), 0, 2); // 位深度
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
            stream.Write(BitConverter.GetBytes(samples.Length * 2), 0, 4); // 数据大小

            // 音频数据（16位PCM）
            foreach (float sample in samples) {
                short pcmSample = (short)(sample * short.MaxValue);
                stream.Write(BitConverter.GetBytes(pcmSample), 0, 2);
            }

            return stream.ToArray();
        }
    }
}
