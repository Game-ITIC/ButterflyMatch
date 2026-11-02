using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarView : MonoBehaviour
{
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private TMP_Text _healthRelaodText;
    [SerializeField] private Image _image;
    [SerializeField] private Button _addMoreButton;

    private int _lives = -1;

    public Button AddMoreButton => _addMoreButton;
    public Image Icon => _image;
    public TMP_Text HealthText => _healthText;
    public TMP_Text ReloadText => _healthRelaodText;

    public void SetLives(int lives)
    {
        if (_healthText != null)
        {
            _healthText.SetText(lives.ToString());
        }

        if (_lives >= 0 && lives != _lives)
        {
            UIFeel.Punch(_image != null ? _image : _healthText, 0.2f);
        }

        _lives = lives;
    }

    public void SetReloadStatus(string status)
    {
        if (_healthRelaodText != null)
        {
            _healthRelaodText.SetText(status);
        }
    }
}
