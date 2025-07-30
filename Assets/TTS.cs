using System;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Net.Http;
using System.Text;
#if NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#else
using UnityEngine;
#endif

public class TTS
{
    // TTS配置参数
    public string appkey = "V6fJ9tzk8lpPsJDi";
    public string token = "619a98169e534748a14742ffa0445153";
    public string voice = "zhibei_emo";
    // public string emotionCategory = "happy";
    public float emotionIntensity = 1.0f;

    // 音频保存路径
    private string audioSavePath;
    public string AudioSavePath => audioSavePath; // 提供公共访问

    public void Init()
    {
        // 设置音频文件保存路径（必须在Unity生命周期方法中调用）
        audioSavePath = Path.Combine(Application.persistentDataPath, "tts_audio.wav");
        Debug.Log("TTS音频将保存至: " + audioSavePath);
    }

    // 合成语音
    // 定义回调委托
    public delegate void TTSCallback(string audioPath);

    public IEnumerator SynthesizeSpeech(string textContent, string emotionCategory,System.Action<byte[]> audioCallback = null)
    {
        Debug.Log("开始TTS请求..." + textContent);

        // 生成SSML格式文本
        string ssmlText = $@"<speak voice=""{voice}"">
    <emotion category=""{emotionCategory}"" intensity=""{emotionIntensity}"">{textContent}</emotion>
</speak>";
        Debug.Log("生成的SSML文本: " + ssmlText);

        // 发送SSML文本请求并获取音频数据
        yield return ProcessPOSTRequest(ssmlText, "wav", 16000, audioCallback);
    }

    // 发送POST请求并返回音频数据
    private IEnumerator ProcessPOSTRequest(string text, string format, int sampleRate, System.Action<byte[]> audioCallback)
    {
        string url = "https://nls-gateway-cn-shanghai.aliyuncs.com/stream/v1/tts";

#if NEWTONSOFT_JSON
        JObject obj = new JObject();
        obj["voice"] = voice;
        obj["appkey"] = appkey;
        obj["token"] = token;
        obj["text"] = text;
        obj["format"] = format;
        obj["sample_rate"] = sampleRate;
        string bodyContent = obj.ToString();
#else
        // 使用Unity的JsonUtility创建JSON
        TTSRequestData requestData = new TTSRequestData {
            appkey = appkey,
            token = token,
            text = text,
            format = format,
            sample_rate = sampleRate,
            voice = voice
        };
        string bodyContent = JsonUtility.ToJson(requestData);
#endif
        StringContent content = new StringContent(bodyContent, Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = client.PostAsync(url, content).Result;

            // 获取Content-Type
            string contentType = response.Content.Headers.ContentType?.MediaType;
            Debug.Log($"TTS响应Content-Type: {contentType}");

            if (response.IsSuccessStatusCode && contentType != null && contentType.StartsWith("audio/"))
            {
                byte[] audioBuff = response.Content.ReadAsByteArrayAsync().Result;
                Debug.Log($"TTS请求成功! 获取音频数据 ({audioBuff.Length} 字节)");

                // 直接返回音频数据
                audioCallback?.Invoke(audioBuff);
            }
            else
            {
                // 记录详细错误信息
                string responseBody = response.Content.ReadAsStringAsync().Result;
                string error = $"TTS请求失败: {response.StatusCode} - {response.ReasonPhrase}\n响应内容: {responseBody}";
                Debug.LogError(error);
            }
        }

        yield return null;
    }

    // 定义TTS请求数据结构
    [System.Serializable]
    private class TTSRequestData
    {
        public string appkey;
        public string token;
        public string text;
        public string format;
        public int sample_rate;
        public string voice;
    }
}
