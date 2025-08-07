using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class GuestLoginResponse {
    public string message;
    public string token;
}

public class LoginUI : MonoBehaviour {
    public InputField usernameInput;
    public Button guestLoginButton;
    public string url;

    void Start() {
        guestLoginButton.onClick.AddListener(OnGuestLoginClick);
    }

    void OnGuestLoginClick() {
        StartCoroutine(LoginAsGuest());
    }

    IEnumerator LoginAsGuest() {
        string username = usernameInput.text;

        // 创建请求体
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        string jsonBody = $"{{\"username\":\"{username}\", \"deviceId\":\"{deviceId}\"}}";
        byte[] rawBody = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        // 创建 POST 请求
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(rawBody);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 发送请求
        yield return request.SendWebRequest();

        // 处理响应
        if (request.result == UnityWebRequest.Result.Success) {
            GuestLoginResponse response = JsonUtility.FromJson<GuestLoginResponse>(request.downloadHandler.text);

            // 保存 Token
            PlayerPrefs.SetString("AuthToken", response.token);

            // 触发登录成功事件
            var loginData = new Dictionary<string, object> {
                { "username", usernameInput.text },
                { "token", response.token }
            };
            EventManager.TriggerEvent("OnLoginSuccess", loginData);

            // 隐藏登录界面
            gameObject.SetActive(false);
        } else {
            Debug.LogError($"登录失败: {request.error}");
        }
    }
}
