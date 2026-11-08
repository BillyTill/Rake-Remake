using UnityEngine;
using Lake.Health;

namespace Lake.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] HealthComponent health;

        void Awake() { if (health == null) health = GetComponent<HealthComponent>(); }
    }
}