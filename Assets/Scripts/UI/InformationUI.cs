using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI system for displaying hints, objectives, pickup notifications, and item info.
/// Singleton pattern for global access.
/// </summary>
public class InformationUI : MonoBehaviour
{
    public static InformationUI Instance { get; private set; }

    [Header("Animators")]
    [SerializeField] private Animator _itemInfoAnimator;
    [SerializeField] private Animator _effectAnimator;

    [Header("Text Components")]
    [SerializeField] private Text _itemInfoText;
    [SerializeField] private Text _hintText;
    [SerializeField] private Text _objectiveText;

    [Header("Prefabs")]
    [SerializeField] private GameObject _pickupInfoPrefab;
    [SerializeField] private GameObject _hintObj;

    [Header("Layout")]
    [SerializeField] private Transform _logLayout;
    [SerializeField] private Transform _hintParent;

    private bool _showingHint;
    private bool _showingObjective;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Display an objective message for 5 seconds.
    /// </summary>
    public void DrawObjective(string message)
    {
        if (!_showingObjective)
        {
            _objectiveText.text = message;
            StartCoroutine(ShowObjectiveRoutine(message));
        }
    }

    private IEnumerator ShowObjectiveRoutine(string message)
    {
        _showingObjective = true;
        _objectiveText.text = message;
        yield return new WaitForSeconds(5f);
        _objectiveText.text = string.Empty;
        _showingObjective = false;
    }

    /// <summary>
    /// Display a pickup notification in the log.
    /// </summary>
    public void PickupInfo(string message)
    {
        if (_pickupInfoPrefab != null && _logLayout != null)
        {
            var obj = Instantiate(_pickupInfoPrefab, _pickupInfoPrefab.transform.position, _pickupInfoPrefab.transform.rotation);
            obj.transform.SetParent(_logLayout, false);
            obj.GetComponent<Text>().text = message;
        }
    }

    /// <summary>
    /// Show/hide item info text (item name, type, count).
    /// </summary>
    public void DrawItemInfo(string name, bool showType)
    {
        if (_itemInfoAnimator != null)
        {
            _itemInfoAnimator.SetBool("Show", showType);
        }
        if (_itemInfoText != null)
        {
            _itemInfoText.text = showType ? name : string.Empty;
        }
    }

    /// <summary>
    /// Display a hint message for 3 seconds.
    /// </summary>
    public void DrawHint(string text)
    {
        if (!_showingHint)
        {
            _hintText.text = text;
            StartCoroutine(ShowHintRoutine(text));
        }
    }

    private IEnumerator ShowHintRoutine(string text)
    {
        _showingHint = true;
        _hintText.text = text;
        yield return new WaitForSeconds(3f);
        _hintText.text = string.Empty;
        _showingHint = false;
    }

    /// <summary>
    /// Trigger the blood effect on screen (when player takes damage).
    /// </summary>
    public void ShowBloodEffect(bool show)
    {
        if (_effectAnimator != null)
        {
            _effectAnimator.SetBool("Blood", show);
        }
    }

    /// <summary>
    /// Trigger camera animator states (kick, fall, leg hit, death).
    /// </summary>
    public void SetCameraAnimatorState(string stateName, bool value)
    {
        if (_effectAnimator != null)
        {
            _effectAnimator.SetBool(stateName, value);
        }
    }
}