using UnityEngine;

/// <summary>
/// Handles root motion for animated characters.
/// </summary>
public class RootMotionScript : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private bool _applyRootMotion = true;

    private void OnAnimatorMove()
    {
        if (!_applyRootMotion || _animator == null) return;

        // Apply root motion to transform
        transform.position = _animator.rootPosition;
        transform.rotation = _animator.rootRotation;
    }

    public void SetApplyRootMotion(bool apply)
    {
        _applyRootMotion = apply;
        if (_animator != null)
        {
            _animator.applyRootMotion = apply;
        }
    }
}