using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsToggleView : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private CustomToggle _toggle;

    public CustomToggle Toggle => _toggle != null ? _toggle : (_toggle = GetComponentInChildren<CustomToggle>(true));
}
