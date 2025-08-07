using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO; // 添加 System.IO 命名空间用于文件操作
using System.Linq; // 添加 Linq 用于集合操作

public class Role2D : MonoBehaviour {
    public AudioSource audioSource;
    public VideoPlayer videoPlayer;
    public RawImage displayRole;

    private string currentIdleAction; // 当前播放的待机动作

    // Start is called before the first frame update
    void Start() {
        // 初始化视频播放器
        InitializeVideoPlayer();

        // 开始播放随机待机动作
        PlayRandomIdleAction();
    }

    // 初始化视频播放器
    private void InitializeVideoPlayer() {
        // 创建 RenderTexture
        RenderTexture renderTexture = new RenderTexture(1080, 1920, 24);
        videoPlayer.targetTexture = renderTexture;

        // 将视频输出连接到 displayRole
        if (displayRole != null) {
            displayRole.texture = renderTexture;
        }
    }

    // Update is called once per frame
    void Update() {
        // 此方法现在留空
    }

    public void PlayAction(string actionName) {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Actions", actionName);
        videoPlayer.url = path;
        videoPlayer.Play();

        // 如果是待机动作，更新当前记录
        if (actionName.StartsWith("idle_")) {
            currentIdleAction = actionName;
        }
    }

    // 随机播放待机动作的公共方法
    public void PlayRandomIdleAction() {

        // 按类型分组动作
        var quietActions = new List<string> { "idle_6", "idle_7" };
        var microActions = new List<string> { "idle_1", "idle_2", "idle_3", "idle_4", "idle_5" };
        var activeActions = new List<string> { "idle_8", "idle_9", "idle_10" };

        // 按概率选择动作类型
        float randomValue = Random.value;
        List<string> selectedType = null;

        if (randomValue < 0.7f) {
            selectedType = quietActions;
        } else if (randomValue < 0.95f) {
            selectedType = microActions;
        } else {
            selectedType = activeActions;
        }

        int randomIndex = Random.Range(0, selectedType.Count);
        string selectedAction = selectedType[randomIndex];

        // 播放选中的动作
        PlayAction(selectedAction + ".mp4");
        // 设置播放完成回调
        videoPlayer.loopPointReached += OnActionFinished;
    }

    // 公共方法：立即切换到随机待机动作（不等待当前动作完成）
    public void SwitchToRandomIdleImmediately() {
        // 取消当前视频的回调
        if (videoPlayer.isPlaying) {
            videoPlayer.loopPointReached -= OnActionFinished;
            videoPlayer.Stop();
        }
        // 直接播放随机待机动作
        PlayRandomIdleAction();
    }

    // 动作播放完成回调
    private void OnActionFinished(VideoPlayer vp) {
        // 取消之前的回调
        videoPlayer.loopPointReached -= OnActionFinished;

        // 播放新的随机待机动作
        PlayRandomIdleAction();
    }

    public void SetBrightness(float brightness) {
        brightness = Mathf.Clamp01(brightness);
        displayRole.color = new Color(brightness, brightness, brightness);
    }
}
