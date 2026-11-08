using UnityEngine;

/// <summary>
/// Notebook - displays lore, clues, and objectives.
/// </summary>
public class Notebook_obj : DynamicObject
{
    [SerializeField] private GameObject _notebookUI;
    [SerializeField] private Animator _animator;

    public override void Action()
    {
        base.Action();
        OpenNotebook();
    }

    public override string GetInteractionText()
    {
        return "Press 'E' to read notebook";
    }

    private void OpenNotebook()
    {
        if (_notebookUI != null)
        {
            _notebookUI.SetActive(true);
        }
        if (_animator != null)
        {
            _animator.SetBool("Open", true);
        }

        // Disable player
        FPSController.Instance?.SetPlayerState("INVENTORY");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseNotebook()
    {
        if (_notebookUI != null)
        {
            _notebookUI.SetActive(false);
        }
        if (_animator != null)
        {
            _animator.SetBool("Open", false);
        }

        FPSController.Instance?.SetPlayerState("FULL");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}