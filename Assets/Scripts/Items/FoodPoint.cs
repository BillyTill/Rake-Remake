using System.Collections;
using UnityEngine;

/// <summary>
/// Food point where animals eat.
/// Can be used by one animal at a time.
/// </summary>
public class FoodPoint : MonoBehaviour
{
    public bool IsUsed { get; set; } = false;

    [SerializeField] private GameObject _foodModel;
    [SerializeField] private float _respawnTime = 60f;

    private bool _isConsumed = false;

    public void Eat()
    {
        if (_foodModel != null)
        {
            _foodModel.SetActive(false);
        }
        _isConsumed = true;
        StartCoroutine(RespawnRoutine());
    }

    public void Consume()
    {
        // Called when animal finishes eating
        _isConsumed = true;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnTime);
        if (_foodModel != null)
        {
            _foodModel.SetActive(true);
        }
        _isConsumed = false;
    }
}