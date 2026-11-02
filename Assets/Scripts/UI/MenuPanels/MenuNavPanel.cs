using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuNavPanel : MonoBehaviour
{
    [SerializeField] private Button _islandButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private Button _shopButton;

    public Button IslandButton
    {
        get
        {
            if (_islandButton != null) return _islandButton;
            _islandButton = FindChildButton("IslandButton", "Island Button", "Island", "RegionButton", "Region Button");
            return _islandButton;
        }
    }

    public Button MenuButton
    {
        get
        {
            if (_menuButton != null) return _menuButton;
            _menuButton = FindChildButton("MainMenuButton", "Main Button", "MainMenu Button", "MenuButton", "Menu", "Main");
            return _menuButton;
        }
    }

    public Button ShopButton
    {
        get
        {
            if (_shopButton != null) return _shopButton;
            _shopButton = FindChildButton("ShopButton", "Shop Button", "Shop", "InAppButton");
            return _shopButton;
        }
    }

    public Button[] NavigationButtons => new[] { IslandButton, MenuButton, ShopButton };

    [SerializeField, Range(0f, 1f)] private float _inactiveAlpha = 0.6f;

    // Highlights the tab whose screen is open; the others fade back.
    public void SetSelected(Button selected)
    {
        SetTabAlpha(IslandButton, selected);
        SetTabAlpha(MenuButton, selected);
        SetTabAlpha(ShopButton, selected);
    }

    private void SetTabAlpha(Button tab, Button selected)
    {
        if (tab == null) return;
        var group = tab.GetComponent<CanvasGroup>();
        if (group == null) group = tab.gameObject.AddComponent<CanvasGroup>();
        group.alpha = tab == selected ? 1f : _inactiveAlpha;
    }

    private Button FindChildButton(params string[] names)
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            foreach (var name in names)
            {
                if (string.Equals(button.gameObject.name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return button;
                }
            }
        }

        return null;
    }
}
