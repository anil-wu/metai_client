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

    void Start() {
        // 绑定按钮点击事件
        guestLoginButton.onClick.AddListener(OnGuestLoginClick);
    }

    void OnGuestLoginClick() {
        Debug.Log("OnGuestLoginClick");
        StartCoroutine(LoginAsGuest());
    }

    IEnumerator LoginAsGuest() {
        string url = "http://localhost:3001/auth/guest";
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
            Debug.Log($"{response.message}, Token: {response.token}");

            // 保存Token
            PlayerPrefs.SetString("AuthToken", response.token);

            // 触发登录成功事件
            var loginData = new Dictionary<string, object>
            {
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
