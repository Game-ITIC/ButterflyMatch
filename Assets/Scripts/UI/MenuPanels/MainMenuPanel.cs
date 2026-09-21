using UI;
using UnityEngine;
using Views;

public class MainMenuPanel : MonoBehaviour
{
    [SerializeField] private SettingsPanel _settingsPanel;
    [SerializeField] private TaskPanel _taskPanel;
    [SerializeField] private InAppView _shopPanel;
    [SerializeField] private TopBarPanel _topBarPanel;
    [SerializeField] private MenuActionPanel _menuActionPanel;
    [SerializeField] private MenuNavPanel _menuNavPanel;
    [SerializeField] private IslandPanel _islandPanel;
    [SerializeField] private LifePopup _lifePopup;
    [SerializeField] private RewardPopup _rewardPopup;

    public SettingsPanel SettingsPanel
    {
        get
        {
            if (_settingsPanel != null) return _settingsPanel;
            _settingsPanel = GetComponentInChildren<SettingsPanel>(true) ?? FindObjectOfType<SettingsPanel>(true);
            return _settingsPanel;
        }
    }

    public TaskPanel TaskPanel
    {
        get
        {
            if (_taskPanel != null) return _taskPanel;
            _taskPanel = GetComponentInChildren<TaskPanel>(true) ?? FindObjectOfType<TaskPanel>(true);
            return _taskPanel;
        }
    }

    public InAppView ShopPanel
    {
        get
        {
            if (_shopPanel != null) return _shopPanel;
            _shopPanel = GetComponentInChildren<InAppView>(true) ?? FindObjectOfType<InAppView>(true);
            return _shopPanel;
        }
    }

    public TopBarPanel TopBarPanel
    {
        get
        {
            if (_topBarPanel != null) return _topBarPanel;
            _topBarPanel = GetComponentInChildren<TopBarPanel>(true) ?? FindObjectOfType<TopBarPanel>(true);
            return _topBarPanel;
        }
    }

    public MenuActionPanel MenuActionPanel
    {
        get
        {
            if (_menuActionPanel != null) return _menuActionPanel;
            _menuActionPanel = GetComponentInChildren<MenuActionPanel>(true) ?? FindObjectOfType<MenuActionPanel>(true);
            return _menuActionPanel;
        }
    }

    public MenuNavPanel MenuNavPanel
    {
        get
        {
            if (_menuNavPanel != null) return _menuNavPanel;
            _menuNavPanel = GetComponentInChildren<MenuNavPanel>(true) ?? FindObjectOfType<MenuNavPanel>(true);
            return _menuNavPanel;
        }
    }

    public LifePopup LifePopup
    {
        get
        {
            if (_lifePopup != null) return _lifePopup;
            _lifePopup = GetComponentInChildren<LifePopup>(true);
            return _lifePopup;
        }
    }

    public RewardPopup RewardPopup
    {
        get
        {
            if (_rewardPopup != null) return _rewardPopup;
            _rewardPopup = GetComponentInChildren<RewardPopup>(true);
            return _rewardPopup;
        }
    }

    public IslandPanel IslandPanel
    {
        get
        {
            if (_islandPanel != null) return _islandPanel;
            _islandPanel = GetComponentInChildren<IslandPanel>(true) ?? FindObjectOfType<IslandPanel>(true);
            return _islandPanel;
        }
    }
}
