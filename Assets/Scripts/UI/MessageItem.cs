using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MessageItem : MonoBehaviour
{
    public RectTransform bg; // 消息背景
    public Text text; // 消息文本
    public Image bgImage;    // 背景图片组件
    public CanvasGroup canvasGroup; // 用于控制透明度
    public int Index { get; set; } = -1; // 消息索引

    void Awake()
    {
        // 确保CanvasGroup组件存在
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 设置自动换行（标准Text组件方式）
        text.horizontalOverflow = HorizontalWrapMode.Wrap;

        // 设置文本最大宽度（400 - 左右边距40 = 360）
        text.rectTransform.sizeDelta = new Vector2(360, 0);
    }

    // 设置消息文本并调整背景
    public void SetText(string messageText)
    {
        this.text.text = messageText;
        ResizeBackground();
    }

    // 调整背景大小（最大宽度400，高度自适应）
    private void ResizeBackground()
    {
        // 强制刷新布局（标准Text组件方式）
        LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);

        // 获取文本实际尺寸
        Vector2 textSize = new Vector2(
            text.preferredWidth,
            text.preferredHeight
        );

        // 应用最大宽度限制
        float bgWidth = Mathf.Min(textSize.x + 40, 400); // 40=左右边距
        float bgHeight = textSize.y + 30; // 30=上下边距

        // 更新背景尺寸
        bg.sizeDelta = new Vector2(bgWidth, bgHeight);

        // 设置文本实际高度
        text.rectTransform.sizeDelta = new Vector2(360, textSize.y);
    }

    // 设置对齐方式 (true=右侧, false=左侧)
    public void SetAlignment(bool isRightSide)
    {
        // 设置锚点和轴心
        bg.anchorMin = new Vector2(isRightSide ? 1 : 0, 0.5f);
        bg.anchorMax = new Vector2(isRightSide ? 1 : 0, 0.5f);
        bg.pivot = new Vector2(isRightSide ? 1 : 0, 0.5f);

        // 重置位置
        bg.anchoredPosition = Vector2.zero;

        // 设置文本对齐方式（标准Text组件）
        text.alignment = isRightSide ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
    }

    // 设置背景样式
    public void SetStyle(Color bgColor)
    {
        if (bgImage != null)
        {
            bgImage.color = bgColor;
        }

        // 确保背景尺寸正确
        ResizeBackground();
    }

    // 设置透明度
    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
    }
}
