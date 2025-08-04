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

    // 随机播放待机动作的私有方法
    private void PlayRandomIdleAction() {
        // 获取所有待机动作文件
        string actionsPath = Path.Combine(Application.streamingAssetsPath, "Actions");
        string[] allFiles = Directory.GetFiles(actionsPath);

        // 过滤出以 "idle_" 开头且不是 .meta 的文件
        var idleActions = allFiles
            .Select(Path.GetFileName)
            .Where(name => name.StartsWith("idle_") && !name.EndsWith(".meta"))
            .ToList();

        // 排除当前正在播放的动作
        var availableActions = idleActions
            .Where(name => name != currentIdleAction)
            .ToList();

        // 如果没有可用动作，则使用全部
        if (!availableActions.Any()) {
            availableActions = idleActions;
        }

        // 随机选择一个动作
        if (availableActions.Any()) {
            int randomIndex = Random.Range(0, availableActions.Count);
            string selectedAction = availableActions[randomIndex];

            // 播放选中的动作
            PlayAction(selectedAction);

            // 设置播放完成回调
            videoPlayer.loopPointReached += OnActionFinished;
        }
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
