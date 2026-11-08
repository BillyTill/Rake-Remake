using UnityEngine;

namespace Lake.Bodycam.Config
{
    /// <summary>
    /// Конфиг адаптера ввода. Управляет переключением между старым Input Manager и новым Input System.
    /// </summary>
    [CreateAssetMenu(fileName = "InputAdapterConfig", menuName = "Lake/Bodycam/Input Adapter Config")]
    public class InputAdapterConfig : ScriptableObject
    {
        [Header("Feature Flag")]
        [Tooltip("Включить новый Input System. false = старый Input Manager (дефолт, безопасно).")]
        [SerializeField] private bool enableNewInput = false;

        [Header("Debug")]
        [Tooltip("Логировать события ввода адаптера.")]
        [SerializeField] private bool logInputEvents = false;

        // Public getters
        public bool EnableNewInput => enableNewInput;
        public bool LogInputEvents => logInputEvents;
    }
}