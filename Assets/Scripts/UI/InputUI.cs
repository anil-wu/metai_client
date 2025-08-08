using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;

public class InputUI : MonoBehaviour {
    public Button sendBtn;
    public InputField videoUrlInput;
    public Button recordBtn; // 新增录音按钮
    public Button stopRecordBtn; // 新增停止录音按钮
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

        // 绑定录音按钮事件
        recordBtn.onClick.AddListener(StartRecording);
        stopRecordBtn.onClick.AddListener(StopRecording);

        // 绑定切换按钮事件
        textInput.onClick.AddListener(OnTextInputClick);
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

        stopRecordBtn.gameObject.SetActive(false);
        recordBtn.gameObject.SetActive(true);
    }

    // 开始录音（当按下录音按钮时调用）
    private void StartRecording() {
        stopRecordBtn.gameObject.SetActive(true);
        recordBtn.gameObject.SetActive(false);
        if (isRecording) return;

        // 检查麦克风权限
        if (!Microphone.IsRecording(null)) {
            isRecording = true;

            // 显示停止录音按钮
            stopRecordBtn.gameObject.SetActive(true);
            stopRecordBtn.interactable = true;

            // 隐藏录音按钮
            recordBtn.gameObject.SetActive(false);

            // 开始录音
            recordedClip = Microphone.Start(null, false, RECORD_LENGTH, SAMPLE_RATE);
        }
    }

    // 停止录音并识别（当松开录音按钮时调用）
    private void StopRecording() {
        if (!isRecording) return;
        stopRecordBtn.gameObject.SetActive(false);
        recordBtn.gameObject.SetActive(true);

        isRecording = false;

        // 停止录音
        Microphone.End(null);

        // 获取录音数据
        float[] samples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        // 转换为16位PCM格式（WAV）
        byte[] wavData = ConvertToWAV(samples, recordedClip.channels, SAMPLE_RATE);

        // 保存录音文件（用于测试）
        SaveRecordFile(wavData);

        // 调用语音识别
        StartCoroutine(RecognizeSpeech(wavData));

        // 重置按钮状态
        stopRecordBtn.gameObject.SetActive(false);
        recordBtn.gameObject.SetActive(true);
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
        // 重置按钮状态
        stopRecordBtn.interactable = false;
        recordBtn.interactable = true;
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

    // 保存录音文件到工程目录
    private void SaveRecordFile(byte[] wavData) {
        try {
            // 创建Records目录
            string recordsDir = Path.Combine(Application.dataPath, "../Records");
            if (!Directory.Exists(recordsDir)) {
                Directory.CreateDirectory(recordsDir);
            }

            // 生成带时间戳的文件名
            string fileName = $"record_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            string filePath = Path.Combine(recordsDir, fileName);

            // 保存文件
            File.WriteAllBytes(filePath, wavData);
            Debug.Log($"录音文件已保存: {filePath}");
        } catch (Exception ex) {
            Debug.LogError($"保存录音文件失败: {ex.Message}");
        }
    }
}
