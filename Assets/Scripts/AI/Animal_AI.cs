using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Animal AI (Boar/Deer).
/// Wanders, eats at food points, flees from Rake/player.
/// Can be trapped and used for "Pig Cam".
/// </summary>
public class Animal_AI : Unit_obj
{
    public enum AnimalState
    {
        Idle,
        Walk,
        Eating,
        Walking,
        Escape
    }

    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private AnimalHead _animalHead;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private AudioSource _audioSource;

    [Header("Points")]
    [SerializeField] private List<Transform> _walkPoints = new List<Transform>();
    [SerializeField] private List<Transform> _foodPoints = new List<Transform>();
    [SerializeField] private List<Transform> _escapePoints = new List<Transform>();

    [Header("Settings")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _dangerDistance = 30f;
    [SerializeField] private int _maxHealth = 1000;
    [SerializeField] private float _stopDistance = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip _walkSound;
    [SerializeField] private AudioClip[] _howlSounds;

    [Header("Camera")]
    [SerializeField] private Transform _cameraMount; // For Pig Cam

    [Header("Effects")]
    [SerializeField] private GameObject _bloodSpot;
    [SerializeField] private GameObject _ragdoll;

    // Runtime
    private AnimalState _currentState = AnimalState.Walk;
    private int _health;
    private bool _isAlive = true;
    private bool _isHungry = false;
    private bool _isWalking = false;
    private bool _isDanger = false;
    private bool _isTrapped = false;
    private bool _isTakingDamage = false;
    private bool _isKilling = false;
    private int _hungryCounter = 0;
    private float _staminaCounter = 0f;
    private float _takeDamageWaitTime;
    private float _walkingWaitTime;
    private float _walkingWaitTimeMax = 3f;
    private float _eatingWaitTime;
    private float _eatingWaitTimeMax = 5f;
    private float _dangerWaitTime;
    private bool _enemyInRadius = false;
    private Transform _enemy;
    private Vector3 _currentWalkPoint;
    private Vector3 _currentEatPoint;
    private Vector3 _currentEscapePoint;
    private int _eatPointIndex = 0;
    private bool _stepDown = false;
    private float _walkSoundDelay = 0.7f;
    private float _runSoundDelay = 0.2f;
    private bool _soundState = false;

    public int Health => _health;
    public bool IsAlive => _isAlive;
    public bool IsTrapped => _isTrapped;

    private void Start()
    {
        _health = _maxHealth;
        _navMeshAgent.stoppingDistance = _stopDistance;
        NotificationsManager.Instance.AddListener(this, "Event_PlayerFire");
        SetWalkingState();
    }

    private void Update()
    {
        if (_isAlive)
        {
            if (_isTrapped)
            {
                Idle();
            }
            else if (_isDanger)
            {
                Escape();
            }
            else
            {
                // Check for nearby threats
                if (_enemyInRadius && _enemy != null)
                {
                    float dist = Vector3.Distance(_enemy.position, transform.position);
                    if (dist < _dangerDistance)
                    {
                        SetDangerState();
                    }
                    if (_animalHead != null && _animalHead.IsSeen)
                    {
                        SetDangerState();
                    }
                }

                if (_isHungry)
                {
                    Eating();
                }
                else
                {
                    Walking();
                }
            }
        }

        if (_isKilling)
        {
            Killing();
        }
    }

    public void Event_PlayerFire(Component sender)
    {
        if (!_isDanger)
        {
            float dist = Vector3.Distance(transform.position, sender.transform.position);
            if (dist < 300f)
            {
                SetDangerState();
            }
        }
    }

    public void Event_Trapped()
    {
        SetTrappedState();
    }

    public override void TakeDamage(int amount, string damageType)
    {
        _health -= amount;

        if (damageType == "SniperShot" || damageType == "TrapDamage")
        {
            SetDangerState();
        }

        if (_health <= 0)
        {
            Die();
        }
    }

    public void SetCamera(bool active)
    {
        if (_cameraMount != null)
        {
            var cam = _cameraMount.GetComponent<RecordCamera_obj>();
            if (cam != null) cam.gameObject.SetActive(active);
        }
    }

    #region State Methods

    public void SetHungryState()
    {
        ResetDestinations();
        RefreshFoodPoints();
        _navMeshAgent.Resume();
        _isHungry = true;
        _navMeshAgent.speed = _walkSpeed;

        if (_currentEatPoint == Vector3.zero && _foodPoints.Count > 0)
        {
            _eatPointIndex = Random.Range(0, _foodPoints.Count);
            _currentEatPoint = _foodPoints[_eatPointIndex].position;
        }
    }

    public void SetDangerState()
    {
        _audioSource?.PlayOneShot(_howlSounds[Random.Range(0, _howlSounds.Length)]);
        ResetDestinations();
        _navMeshAgent.Resume();
        _isDanger = true;
        _animator.SetBool("Eat", false);
        _animator.SetBool("Walk", false);
        _animator.SetFloat("Speed", 2f);
        _navMeshAgent.speed = _runSpeed;

        if (_currentEscapePoint == Vector3.zero && _escapePoints.Count > 0)
        {
            _currentEscapePoint = _escapePoints[Random.Range(0, _escapePoints.Count)].position;
        }
    }

    public void SetWalkingState()
    {
        _navMeshAgent.ResetPath();
        ResetDestinations();
        _navMeshAgent.Resume();
        _isWalking = true;
        _animator.SetFloat("Speed", 1f);
        _navMeshAgent.speed = _walkSpeed;

        if (_currentWalkPoint == Vector3.zero && _walkPoints.Count > 0)
        {
            _currentWalkPoint = _walkPoints[Random.Range(0, _walkPoints.Count)].position;
        }
    }

    public void SetTrappedState()
    {
        _isTrapped = true;
        _isHungry = false;
        SetCamera(true);
    }

    private void Idle()
    {
        _navMeshAgent.Stop();
        _animator.SetFloat("Speed", 0f);
        _animator.SetBool("Eat", false);
    }

    private void Walking()
    {
        if (_currentWalkPoint == Vector3.zero && _walkPoints.Count > 0)
        {
            _currentWalkPoint = _walkPoints[Random.Range(0, _walkPoints.Count)].position;
        }

        float dist = Vector3.Distance(_currentWalkPoint, transform.position);

        if (dist < _stopDistance)
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("Walk", true);
            _currentState = AnimalState.Walking;
            _walkingWaitTime += Time.deltaTime;

            if (_walkingWaitTime > _walkingWaitTimeMax)
            {
                _animator.SetBool("Walk", false);
                _animator.SetInteger("WalkType", Random.Range(0, 2));
                _walkingWaitTime = 0f;
                _animator.SetFloat("Speed", 1f);

                // Random nearby point
                Vector3 randomDir = Random.insideUnitSphere * 5f;
                randomDir += _currentWalkPoint;
                NavMeshHit hit;
                NavMesh.SamplePosition(randomDir, out hit, 5f, NavMesh.AllAreas);

                int[] hungerGain = { 10, 15, 20, 25 };
                _hungryCounter += hungerGain[Random.Range(0, hungerGain.Length)];
                UpdateHungerState();

                _currentWalkPoint = hit.position;
                int[] waitTimes = { 3, 3 };
                _walkingWaitTimeMax = waitTimes[Random.Range(0, waitTimes.Length)];
            }
        }
        else if (!_navMeshAgent.pathPending)
        {
            _currentState = AnimalState.Walk;
            PlayFootstep(_walkSoundDelay);
            _navMeshAgent.SetDestination(_currentWalkPoint);
            _animator.SetFloat("Speed", 1f);
        }
        else
        {
            _animator.SetFloat("Speed", 0f);
        }
    }

    private void Eating()
    {
        if (_foodPoints.Count == 0) return;

        float dist = Vector3.Distance(_currentEatPoint, transform.position);
        var foodPoint = _foodPoints[_eatPointIndex].GetComponent<FoodPoint>();

        // If food point is taken, find new one
        if (dist < 10f && dist > _stopDistance && foodPoint != null && foodPoint.IsUsed)
        {
            SetHungryState();
            return;
        }

        if (dist < _stopDistance)
        {
            if (foodPoint != null) foodPoint.IsUsed = true;
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("Eat", true);
            _eatingWaitTime += Time.deltaTime;
            _currentState = AnimalState.Eating;

            if (_eatingWaitTime > _eatingWaitTimeMax)
            {
                if (foodPoint != null)
                {
                    foodPoint.IsUsed = false;
                    foodPoint.Consume();
                }
                _eatingWaitTime = 0f;
                _hungryCounter = 0;
                _isHungry = false;
                _currentWalkPoint = Vector3.zero;
                _animator.SetBool("Eat", false);
                _animator.SetFloat("Speed", 1f);
                SetWalkingState();
            }
        }
        else
        {
            PlayFootstep(_walkSoundDelay);
            _currentState = AnimalState.Walk;
            _animator.SetBool("Eat", false);
            _navMeshAgent.SetDestination(_currentEatPoint);
            _animator.SetFloat("Speed", 1f);
        }
    }

    private void Escape()
    {
        float dist = Vector3.Distance(_currentEscapePoint, transform.position);

        if (dist < _stopDistance)
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("Walk", false);
            _animator.SetBool("Eat", false);

            if (!_enemyInRadius)
            {
                _dangerWaitTime += Time.deltaTime;
                _currentState = AnimalState.Idle;

                if (_dangerWaitTime > 5f)
                {
                    _dangerWaitTime = 0f;
                    _currentEatPoint = Vector3.zero;
                    _currentWalkPoint = Vector3.zero;
                    _isDanger = false;

                    if (_isWalking) SetWalkingState();
                    if (_isHungry) SetHungryState();
                }
            }
            else
            {
                SetDangerState();
                _dangerWaitTime = 0f;
            }
        }
        else
        {
            PlayFootstep(_runSoundDelay);
            _currentState = AnimalState.Escape;
            _animator.SetBool("Eat", false);
            _navMeshAgent.SetDestination(_currentEscapePoint);
            _animator.SetFloat("Speed", 2f);
        }
    }

    private void Killing()
    {
        _navMeshAgent.Stop();
        _isHungry = false;
        _isWalking = false;
        _animator.SetFloat("Speed", 0f);
        _isDanger = false;
    }

    private void UpdateHungerState()
    {
        if (_hungryCounter > 100)
        {
            SetHungryState();
        }
    }

    private void ResetDestinations()
    {
        _currentEatPoint = Vector3.zero;
        _currentEscapePoint = Vector3.zero;
        _currentWalkPoint = Vector3.zero;
    }

    private void RefreshFoodPoints()
    {
        for (int i = _foodPoints.Count - 1; i >= 0; i--)
        {
            if (_foodPoints[i] == null)
            {
                _foodPoints.RemoveAt(i);
            }
        }
    }

    public void AfterTrapEffect()
    {
        SetDangerState();
        _isTrapped = false;
    }

    private void PlayFootstep(float interval)
    {
        if (!_stepDown)
        {
            StartCoroutine(FootstepRoutine(interval));
        }
    }

    private IEnumerator FootstepRoutine(float delay)
    {
        _stepDown = true;
        _audioSource?.PlayOneShot(_walkSound);
        yield return new WaitForSeconds(delay);
        _stepDown = false;
    }

    private void Die()
    {
        _isAlive = false;
        _animator.SetBool("Dead", true);
        _navMeshAgent.Stop();

        // Blood effect
        Vector3 pos = transform.position;
        pos.y += 1f;
        Ray ray = new Ray(pos, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (_bloodSpot != null)
            {
                Instantiate(_bloodSpot, hit.point + hit.normal * 0.2f, Quaternion.LookRotation(-hit.normal));
            }
        }

        NotificationsManager.Instance.RemoveListener(this, "Event_PlayerFire");
        _animator.SetFloat("Speed", 0f);
        _animator.SetBool("Eat", false);

        if (_ragdoll != null)
        {
            Instantiate(_ragdoll, transform.position, Quaternion.identity);
        }

        // Remove camera if attached
        if (_cameraMount != null)
        {
            var cam = _cameraMount.GetComponent<RecordCamera_obj>();
            if (cam != null)
            {
                CameraManager.Instance?.RemoveCamera(cam.transform);
            }
        }

        Destroy(gameObject);
    }

    #endregion

    #region Triggers

    private void OnTriggerEnter(Collider other)
    {
        var enemyAI = other.GetComponent<Enemy_AI>();
        if (enemyAI != null)
        {
            _enemy = other.transform;
            _enemyInRadius = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var enemyAI = other.GetComponent<Enemy_AI>();
        if (enemyAI != null)
        {
            _enemyInRadius = false;
        }
    }

    #endregion
}