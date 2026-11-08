using UnityEngine;

/// <summary>
/// Survival guide menu - shows game mechanics, controls, tips.
/// </summary>
public class SuvivalGuideMenu : MonoBehaviour
{
    [SerializeField] private GameObject _guidePanel;
    [SerializeField] private GameObject[] _pages;
    private int _currentPage = 0;

    private void Start()
    {
        if (_guidePanel != null) _guidePanel.SetActive(false);
        ShowPage(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleGuide();
        }

        if (_guidePanel != null && _guidePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                NextPage();
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                PreviousPage();
            }
        }
    }

    public void ToggleGuide()
    {
        if (_guidePanel != null)
        {
            bool active = !_guidePanel.activeSelf;
            _guidePanel.SetActive(active);
            
            if (active)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Time.timeScale = 0f;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f;
            }
        }
    }

    private void ShowPage(int index)
    {
        if (_pages == null || _pages.Length == 0) return;
        
        index = Mathf.Clamp(index, 0, _pages.Length - 1);
        _currentPage = index;

        for (int i = 0; i < _pages.Length; i++)
        {
            if (_pages[i] != null)
            {
                _pages[i].SetActive(i == index);
            }
        }
    }

    private void NextPage()
    {
        ShowPage(_currentPage + 1);
    }

    private void PreviousPage()
    {
        ShowPage(_currentPage - 1);
    }
}