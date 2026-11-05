using UnityEngine;
using UnityEngine.UI;

// Bottom nav bar of the main menu: three equal tabs and one raised bubble behind the open tab.
// MenuPager owns which page is open and drives both the highlight and the bubble.
public class MenuNavPanel : MonoBehaviour
{
    private const int TabCount = 3;

    [SerializeField] private Button _islandButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private Button _shopButton;

    [Tooltip("Raised shape behind the open tab. Its x anchor follows the pager position, so it tracks the finger while swiping.")]
    [SerializeField] private RectTransform _selectionBubble;
    [SerializeField, Range(0f, 1f)] private float _inactiveAlpha = 0.6f;

    public Button IslandButton => _islandButton;

    public Button MenuButton => _menuButton;

    public Button ShopButton => _shopButton;

    public Button[] NavigationButtons => new[] { IslandButton, MenuButton, ShopButton };

    // Highlights the tab whose screen is open; the others fade back.
    public void SetSelected(Button selected)
    {
        SetTabAlpha(IslandButton, selected);
        SetTabAlpha(MenuButton, selected);
        SetTabAlpha(ShopButton, selected);
    }

    // pagePosition is the pager's fractional page: 0 = Island tab, 1 = Menu tab, 2 = Shop tab.
    public void SetSelectionPosition(float pagePosition)
    {
        if (_selectionBubble == null) return;

        var x = (Mathf.Clamp(pagePosition, 0f, TabCount - 1) + 0.5f) / TabCount;
        var anchorMin = _selectionBubble.anchorMin;
        var anchorMax = _selectionBubble.anchorMax;
        anchorMin.x = x;
        anchorMax.x = x;
        _selectionBubble.anchorMin = anchorMin;
        _selectionBubble.anchorMax = anchorMax;
    }

    private void SetTabAlpha(Button tab, Button selected)
    {
        if (tab == null) return;
        var group = tab.GetComponent<CanvasGroup>();
        if (group == null) group = tab.gameObject.AddComponent<CanvasGroup>();
        group.alpha = tab == selected ? 1f : _inactiveAlpha;
    }
}
