using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StateUI : MonoBehaviour {
    // UI 元素引用
    public Button soundBtn;
    public Button callBtn;
    public Button CallOffBtn;
    public Image linkIcon;
    public Image linking; // 新增连接中状态显示组件
    public Text linkTips;

    // 按钮状态图标
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public Sprite linkConnectedSprite;
    public Sprite linkDisconnectedSprite;

    // 内部状态
    private bool isSoundOn = true;
    private bool isConnecting = false;
    private WebSocketManager webSocketManager;
    private Vector3 callBtnInitialPos; // 记录 CallBtn 初始位置
    private Coroutine pulseCoroutine; // 心跳动画协程

    void Start() {
        // 初始化按钮事件
        soundBtn.onClick.AddListener(ToggleSound);
        callBtn.onClick.AddListener(ConnectWebSocket);
        CallOffBtn.onClick.AddListener(OnCallOffButtonClicked); // 添加CallOffBtn点击事件

        // 记录 CallBtn 初始位置
        callBtnInitialPos = callBtn.transform.localPosition;

        // 初始状态设置
        UpdateSoundButton();

        // 初始隐藏连接中状态
        linking.gameObject.SetActive(false);

        // 注册连接状态更新事件
        EventManager.StartListening(EventManager.ConnectionStatusEvent, (param) => {
            if (param is bool isConnected) {
                UpdateConnectionStatus(isConnected);
            }
        });

        // 注册连接中状态事件
        EventManager.StartListening(EventManager.ConnectingStatusEvent, (param) => {
            if (param is string progress) {
                UpdateConnectingStatus(progress);
            }
        });
    }

    void OnDestroy() {
        // 取消事件注册
        EventManager.StopListening(EventManager.ConnectionStatusEvent, (param) => {
            if (param is bool isConnected) {
                UpdateConnectionStatus(isConnected);
            }
        });

        EventManager.StopListening(EventManager.ConnectingStatusEvent, (param) => {
            if (param is string progress) {
                UpdateConnectingStatus(progress);
            }
        });
    }

    // 切换声音状态
    private void ToggleSound() {
        isSoundOn = !isSoundOn;
        UpdateSoundButton();

        // 通过 EventManager 触发声音切换事件
        EventManager.TriggerEvent(EventManager.SoundToggleEvent, isSoundOn);
    }

    // 更新声音按钮图标
    private void UpdateSoundButton() {
        soundBtn.image.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
    }

    // 触发呼叫事件
    private void ConnectWebSocket() {
        if (isConnecting) return;

        isConnecting = true;
        StartCoroutine(AnimateCallButton());

        // 显示连接中状态并启动心跳动画
        linkIcon.gameObject.SetActive(false);
        linking.gameObject.SetActive(true);
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseAnimation());

        // 通过 EventManager 触发呼叫按钮事件
        EventManager.TriggerEvent(EventManager.CallButtonEvent, null);
    }

    // 心跳动画协程
    private IEnumerator PulseAnimation() {
        float scale = 1f;
        float speed = 2f;
        while (true) {
            // 缩放动画：1.0 -> 1.2 -> 1.0
            scale = 1f + 0.2f * Mathf.Sin(Time.time * speed);
            linking.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    // 更新连接状态显示
    private void UpdateConnectionStatus(bool isConnected) {
        Debug.Log($"UpdateConnectionStatus {isConnected}");
        // 停止心跳动画并隐藏 linking
        if (pulseCoroutine != null) {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        linking.gameObject.SetActive(false);
        linking.transform.localScale = Vector3.one; // 重置缩放

        // 显示 linkIcon
        linkIcon.gameObject.SetActive(true);

        if (isConnected) {
            linkIcon.sprite = linkConnectedSprite;
            linkTips.text = "Connected";
        } else {
            linkIcon.sprite = linkDisconnectedSprite;
            linkTips.text = "Disconnected";
            ResetCallButton(); // 断开连接时重置按钮状态
        }
    }

    // 更新连接中状态
    private void UpdateConnectingStatus(string progress) {
        // 只更新文本，不改变显示状态（状态已在点击 CallBtn 时设置）
        linkTips.text = progress;
    }

    // CallOff按钮点击处理
    private void OnCallOffButtonClicked() {
        // 触发关闭WebSocket事件
        EventManager.TriggerEvent(EventManager.CloseWebSocketEvent, null);
    }

    // 重置 Call 按钮状态
    private void ResetCallButton() {
        // 确保按钮可见且可交互
        callBtn.gameObject.SetActive(true);
        callBtn.interactable = true;
        callBtn.image.color = Color.white;

        // 恢复初始位置
        callBtn.transform.localPosition = callBtnInitialPos;
    }

    // CallBtn 动画协程
    private IEnumerator AnimateCallButton() {
        // 禁用按钮交互
        callBtn.interactable = false;

        Vector3 startPos = callBtn.transform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, -100f, 0); // 向下移动 100 单位

        float duration = 0.3f;
        float elapsed = 0f;

        // 向下移动并淡出
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            callBtn.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            callBtn.image.color = new Color(1, 1, 1, 1 - t);

            yield return null;
        }

        // 隐藏按钮
        callBtn.gameObject.SetActive(false);
        isConnecting = false;
    }
}
