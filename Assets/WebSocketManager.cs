using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebSocketManager : MonoBehaviour
{
    public enum ConnectionStatus
    {
        Connecting,
        Connected,
        Disconnected,
        Failed
    }

    private ClientWebSocket webSocket;
    private Uri serverUri;
    private CancellationTokenSource cancellationTokenSource;

    // 连接状态事件
    public event Action<bool> OnConnectionChanged;
    // 连接进度事件
    public event Action<string> OnConnectionProgress;
    // 消息接收事件
    public event Action<string> OnMessageReceived;

    // 初始化 WebSocket 连接
    public async void Connect(string url)
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket 已连接");
            return;
        }

        try
        {
            serverUri = new Uri(url);
            webSocket = new ClientWebSocket();
            cancellationTokenSource = new CancellationTokenSource();

            // 触发连接进度事件
            OnConnectionProgress?.Invoke("正在连接服务器...");

            await webSocket.ConnectAsync(serverUri, cancellationTokenSource.Token);

            // 连接成功
            OnConnectionChanged?.Invoke(true);
            OnConnectionProgress?.Invoke("连接成功");
            Debug.Log("WebSocket 连接成功");

            // 启动接收消息协程
            StartCoroutine(ReceiveMessages());
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket 连接失败: {ex.Message}");
            OnConnectionProgress?.Invoke($"连接失败: {ex.Message}");
            OnConnectionChanged?.Invoke(false);
        }
    }

    // 断开连接
    public async void Disconnect()
    {
        if (webSocket == null) return;

        try
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "关闭连接",
                CancellationToken.None);
        }
        finally
        {
            if (webSocket != null)
            {
                webSocket.Dispose();
                webSocket = null;
                cancellationTokenSource?.Cancel();
                OnConnectionProgress?.Invoke("连接已关闭");
                OnConnectionChanged?.Invoke(false);
                Debug.Log("WebSocket 已断开");
            }
        }
    }

    // 发送消息（改为协程方式）
    public void SendMessage(string message)
    {
        StartCoroutine(SendMessageCoroutine(message));
    }

    // 消息发送协程
    private IEnumerator SendMessageCoroutine(string message)
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("无法发送消息: WebSocket 未连接");
            yield break;
        }

        Task sendTask = null;
        try
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // 使用Task.Run在后台线程执行发送操作
            sendTask = Task.Run(async () =>
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"消息发送失败: {ex.Message}");
            yield break;
        }

        // 等待发送完成但不阻塞主线程
        while (!sendTask.IsCompleted)
        {
            yield return null;
        }

        if (sendTask.IsFaulted)
        {
            Debug.LogError($"消息发送失败: {sendTask.Exception.Message}");
        }
    }

    // 接收消息协程（完全非阻塞版本）
    private IEnumerator ReceiveMessages()
    {
        var buffer = new byte[4096];
        var segment = new ArraySegment<byte>(buffer);

        while (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            // 第一步：启动接收任务
            Task<WebSocketReceiveResult> receiveTask = null;
            try
            {
                receiveTask = webSocket.ReceiveAsync(segment, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"启动接收失败: {ex.Message}");
                Disconnect();
                yield break;
            }

            // 等待接收任务完成
            yield return new WaitUntil(() => receiveTask.IsCompleted);

            // 第二步：处理接收结果
            WebSocketReceiveResult result;
            try
            {
                if (receiveTask.IsFaulted)
                {
                    throw receiveTask.Exception;
                }

                result = receiveTask.Result;
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Disconnect();
                    yield break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理接收结果失败: {ex.Message}");
                Disconnect();
                yield break;
            }

            // 第三步：处理分片消息
            int count = result.Count;
            while (!result.EndOfMessage)
            {
                if (count >= buffer.Length)
                {
                    Debug.LogError("消息过长，超出缓冲区大小");
                    Disconnect();
                    yield break;
                }

                segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);

                // 启动分片接收
                Task<WebSocketReceiveResult> fragmentTask = null;
                try
                {
                    fragmentTask = webSocket.ReceiveAsync(segment, cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"启动分片接收失败: {ex.Message}");
                    Disconnect();
                    yield break;
                }

                // 等待分片接收完成
                yield return new WaitUntil(() => fragmentTask.IsCompleted);

                // 处理分片结果
                try
                {
                    if (fragmentTask.IsFaulted)
                    {
                        throw fragmentTask.Exception;
                    }

                    result = fragmentTask.Result;
                    count += result.Count;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"处理分片接收失败: {ex.Message}");
                    Disconnect();
                    yield break;
                }
            }

            // 处理完整消息
            try
            {
                string message = Encoding.UTF8.GetString(buffer, 0, count);
                OnMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理消息失败: {ex.Message}");
            }

            yield return null;
        }
    }

    void OnDestroy()
    {
        Disconnect();
    }
}
