using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 聊天面板控制器（带消息动画效果）
/// </summary>
public class ChatUI : MonoBehaviour {
    public GameObject messageItemPrefab; // 消息项预制体
    public RectTransform messageContainer; // 消息容器
    public Color userMessageColor = new Color(0.2f, 0.5f, 1f, 1f); // 用户消息颜色（蓝色）
    public Color characterMessageColor = new Color(0.0f, 1f, 0.0f, 1f); // 角色消息颜色（绿色）

    // 动画参数
    public float animationDuration = 0.5f; // 动画持续时间
    public float startScale = 0.5f;        // 初始缩放比例
    public float verticalOffset = 100f;    // 垂直偏移量

    // 面板控制
    private CanvasGroup canvasGroup;       // 控制面板透明度的 CanvasGroup

    // 消息管理
    private MessageItem currentMessage;    // 当前消息
    private MessageItem previousMessage;   // 上一条消息
    private List<MessageItem> messagePool = new List<MessageItem>(); // 消息项池子

    void Start() {
        // 添加或获取 CanvasGroup 组件
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 初始状态隐藏面板
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 注册聊天消息事件
        EventManager.StartListening(EventManager.AddMessageToChat, HandleAddMessageEvent);

        // 注册连接状态事件
        EventManager.StartListening(EventManager.ConnectionStatusEvent, HandleConnectionStatus);
    }

    // 处理连接状态变化
    private void HandleConnectionStatus(object isConnectedObj) {
        if (isConnectedObj is bool isConnected) {
            if (isConnected) {
                // 连接成功时淡入显示
                StartCoroutine(FadePanel(1, 0.5f));
            } else {
                // 断开连接时淡出隐藏
                StartCoroutine(FadePanel(0, 0.5f));
            }
        }
    }

    // 面板淡入淡出协程
    private IEnumerator FadePanel(float targetAlpha, float duration) {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = targetAlpha > 0.5f;
        canvasGroup.blocksRaycasts = targetAlpha > 0.5f;
    }

    void OnDestroy() {
        // 取消注册聊天消息事件
        EventManager.StopListening(EventManager.AddMessageToChat, HandleAddMessageEvent);

        // 取消注册连接状态事件
        EventManager.StopListening(EventManager.ConnectionStatusEvent, HandleConnectionStatus);
    }

    // 处理添加消息事件
    private void HandleAddMessageEvent(object messageObj) {
        // 动态解析事件参数
        var messageData = messageObj as Dictionary<string, object>;
        if (messageData != null &&
            messageData.ContainsKey("content") &&
            messageData.ContainsKey("isUserMessage")) {
            string content = messageData["content"].ToString();
            bool isUserMessage = (bool)messageData["isUserMessage"];
            AddMessage(content, isUserMessage);
        }
    }

    // 添加新消息
    public void AddMessage(string content, bool isUserMessage) {
        // 如果有当前消息，先转换为上一条消息
        if (currentMessage != null) {
            previousMessage = currentMessage;
        }

        // 如果有上一条消息，启动退出动画
        if (previousMessage != null) {
            StartCoroutine(AnimateMessageExit(previousMessage));
        }

        // 创建新消息
        currentMessage = GetMessageItemFromPool();
        currentMessage.SetText(content);
        currentMessage.SetAlignment(false);
        currentMessage.SetStyle(isUserMessage ? userMessageColor : characterMessageColor);

        // 设置初始状态（下方，缩小，透明）
        RectTransform rt = currentMessage.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -verticalOffset);
        rt.localScale = Vector3.one * startScale;
        currentMessage.SetAlpha(0);

        // 启动进入动画
        StartCoroutine(AnimateMessageEnter(currentMessage));
    }

    // 消息进入动画（保留固定 x 坐标）
    private IEnumerator AnimateMessageEnter(MessageItem item) {
        RectTransform rt = item.GetComponent<RectTransform>();
        float elapsed = 0;
        Vector3 startPos = rt.anchoredPosition;
        Vector3 targetPos = new Vector2(item.isRightSide ? -20 : 20, 0); // 使用固定 x 坐标

        while (elapsed < animationDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // 位置动画（从下方移动到目标位置）
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            // 缩放动画（从小变大）
            rt.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, t);

            // 透明度动画（从透明到不透明）
            item.SetAlpha(Mathf.Lerp(0, 1, t));

            yield return null;
        }

        // 确保最终状态
        rt.anchoredPosition = targetPos;
        rt.localScale = Vector3.one;
        item.SetAlpha(1);
    }

    // 消息退出动画（保留固定 x 坐标）
    private IEnumerator AnimateMessageExit(MessageItem item) {
        RectTransform rt = item.GetComponent<RectTransform>();
        float elapsed = 0;
        Vector3 startPos = rt.anchoredPosition;
        Vector3 targetPos = new Vector2(item.isRightSide ? -20 : 20, verticalOffset); // 使用固定 x 坐标

        while (elapsed < animationDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // 位置动画（向上移动）
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            // 缩放动画（从大变小）
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * startScale, t);

            // 透明度动画（从可见到透明）
            item.SetAlpha(Mathf.Lerp(1, 0, t));

            yield return null;
        }

        // 动画完成后回收消息
        item.gameObject.SetActive(false);
        messagePool.Add(item);
        previousMessage = null;
    }

    // 从缓存池获取消息项
    private MessageItem GetMessageItemFromPool() {
        // 尝试从缓存池获取
        foreach (MessageItem item in messagePool) {
            if (!item.gameObject.activeSelf) {
                item.gameObject.SetActive(true);
                return item;
            }
        }

        // 缓存池无可用项，创建新实例
        GameObject newObj = Instantiate(messageItemPrefab, messageContainer);
        return newObj.GetComponent<MessageItem>();
    }
}
