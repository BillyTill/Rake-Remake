using UnityEngine;

/// <summary>
/// Tablet computer item - shows map, camera feeds, objectives.
/// </summary>
public class TabletComputer_item : DynamicObject
{
    [SerializeField] private GameObject _tabletUI;
    [SerializeField] private Animator _animator;

    public override void Action()
    {
        base.Action();
        OpenTablet();
    }

    public override string GetInteractionText()
    {
        return "Press 'E' to use tablet";
    }

    private void OpenTablet()
    {
        if (_tabletUI != null)
        {
            _tabletUI.SetActive(true);
        }
        if (_animator != null)
        {
            _animator.SetBool("Open", true);
        }

        FPSController.Instance?.SetPlayerState("INVENTORY");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseTablet()
    {
        if (_tabletUI != null)
        {
            _tabletUI.SetActive(false);
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