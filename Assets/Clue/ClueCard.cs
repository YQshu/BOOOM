using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueCard : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button clickButton;
    [SerializeField] private GameObject lockedOverlay;  // 未解锁遮罩（可选）

    private ClueData clueData;
    private System.Action<ClueData> onClickCallback;

    public void Initialize(ClueData data, System.Action<ClueData> callback)
    {
        clueData = data;
        onClickCallback = callback;

        // 设置显示
        if (iconImage != null && data.clueIcon != null)
            iconImage.sprite = data.clueIcon;

        if (titleText != null)
            titleText.text = data.clueName;

        // 绑定点击事件
        if (clickButton != null)
            clickButton.onClick.AddListener(OnClick);

        // 已解锁，隐藏遮罩
        if (lockedOverlay != null)
            lockedOverlay.SetActive(false);
    }

    private void OnClick()
    {
        onClickCallback?.Invoke(clueData);
    }
}
