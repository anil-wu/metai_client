using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEngine;

public class ASR {
    // 单例实例
    private static ASR _instance;
    public static ASR Instance {
        get {
            if (_instance == null) {
                _instance = new ASR();
            }
            return _instance;
        }
    }

    // 私有构造函数
    private ASR() {}

    // 阿里云凭证（由 MetAI 管理）
    public string appkey { get; set; }
    public string token { get; set; }

    // 设置阿里云凭证
    public void SetCredentials(string token, string appkey) {
        this.token = token;
        this.appkey = appkey;
        Debug.Log("ASR 凭证已设置: token=" + token + ", appkey=" + appkey);
    }

    // 语音识别方法
    public IEnumerator RecognizeSpeech(byte[] audioData, System.Action<string> callback) {
        string url = "https://nls-gateway-cn-shanghai.aliyuncs.com/stream/v1/asr";

        // 构建请求参数
        var query = new System.Collections.Generic.Dictionary<string, string> {
            {"appkey", appkey},
            {"token", token},
            {"format", "wav"},
            {"sample_rate", "16000"}
        };

        // 发送请求
        using (HttpClient client = new HttpClient()) {
            // 添加查询参数
            var builder = new UriBuilder(url);
            var queryString = System.Web.HttpUtility.ParseQueryString(builder.Query);
            foreach (var param in query) {
                queryString[param.Key] = param.Value;
            }
            builder.Query = queryString.ToString();

            // 创建请求内容
            ByteArrayContent content = new ByteArrayContent(audioData);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            // 发送请求
            HttpResponseMessage response = client.PostAsync(builder.ToString(), content).Result;

            if (response.IsSuccessStatusCode) {
                string result = response.Content.ReadAsStringAsync().Result;
                // 解析JSON响应（简化处理）
                int textStart = result.IndexOf("\"result\"") + 10;
                int textEnd = result.IndexOf("\"", textStart);
                string recognizedText = result.Substring(textStart, textEnd - textStart);
                Debug.Log("recognizedText " + recognizedText);
                callback?.Invoke(recognizedText);
            } else {
                Debug.LogError($"ASR请求失败: {response.StatusCode}");
                callback?.Invoke(null);
            }
        }

        yield return null;
    }
}
