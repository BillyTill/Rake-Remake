using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Simple menu button with click event.
/// </summary>
public class Menu_button : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private UnityEvent _onClick;

    private void Awake()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        _onClick?.Invoke();
    }
}