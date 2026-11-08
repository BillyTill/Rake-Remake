using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Rake AI - Main enemy with complex behavior states.
/// 
/// Patrol States:
/// - Follow: Silently follows nearest player
/// - Stealth: Within 70 units, walks slowly to deal massive damage
/// - Chase: Player looked at Rake during Stealth -> glowing eyes, active pursuit
/// 
/// Combat States (reacting to damage):
/// - Low Health (< 1400 HP): Screams, speed/damage increase, glow disappears
/// - Blocking: Enough hits/flashlight -> covers face, immune from front/sides, vulnerable from back
/// - Fake Retreat (< 1950 HP or too many traps): Screams, runs to cave, invulnerable, returns later
/// </summary>
public class Enemy_AI : Unit_obj
{
    public enum PatrolPhase
    {
        Follow = 0,    // Silently stalking
        Stealth = 1,   // Close range, slow walk, massive damage
        Chase = 2      // Active pursuit
    }

    public enum CombatPhase
    {
        None = 0,
        LowHealth = 1,      // < 1400 HP
        Blocking = 2,       // Covering face
        FakeRetreat = 3     // Running to cave
    }

    [Header("Components")]
    [SerializeField] private Animator _animator;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private AudioSource _audioSource;

    [Header("Patrol Points")]
    [SerializeField] private List<Transform> _patrolPoints = new List<Transform>();
    [SerializeField] private List<Transform> _escapePoints = new List<Transform>();

    [Header("Settings")]
    [SerializeField] private float _walkSpeed = 3f;
    [SerializeField] private float _runSpeed = 8f;
    [SerializeField] private float _stealthSpeed = 1.5f;
    [SerializeField] private float _chaseSpeed = 12f;
    [SerializeField] private float _lowHealthSpeed = 15f;
    [SerializeField] private float _stopDistance = 0.5f;
    [SerializeField] private float _stealthRange = 70f;      // Enter stealth within this range
    [SerializeField] private float _attackRange = 3f;        // Attack range
    [SerializeField] private int _maxHealth = 2500;
    [SerializeField] private int _lowHealthThreshold = 1400;
    [SerializeField] private int _fakeRetreatThreshold = 1950;

    [Header("Audio")]
    [SerializeField] private AudioClip _walkSound;
    [SerializeField] private AudioClip[] _idleSounds;
    [SerializeField] private AudioClip[] _painSounds;
    [SerializeField] private AudioClip[] _attackRoars;
    [SerializeField] private AudioClip[] _attackHits;
    [SerializeField] private AudioClip _cameraBreakRoar;
    [SerializeField] private AudioClip _lowHealthScream;
    [SerializeField] private AudioClip _retreatScream;

    [Header("Visual")]
    [SerializeField] private Transform _focusPoint;      // For kill cam
    [SerializeField] private Renderer[] _glowRenderers;  // For Chase/LowHealth glow
    [SerializeField] private Material _glowMaterial;

    [Header("References")]
    [SerializeField] private GameObject _deadPrefab;
    [SerializeField] private Transform _cameraBreakTarget;

    // Runtime state
    private PatrolPhase _currentPatrolPhase = PatrolPhase.Follow;
    private CombatPhase _currentCombatPhase = CombatPhase.None;
    private Transform _playerTarget;
    private Vector3 _currentPatrolPoint;
    private Vector3 _currentHuntPoint;
    private Vector3 _currentEscapePoint;
    private Vector3 _currentBreakPoint;
    private Vector3 _viewRotation;

    private float _patrolWaitTimer;
    private float _huntWaitTimer;
    private float _dangerWaitTimer;
    private float _breakWaitTimer;
    private float _stepTimer;
    private int _hungryCounter;
    private int _attackType; // 0=none, 1=kick, 2=uppercut
    private bool _isPlayerHunting = false;
    private bool _isHaunting = false;
    private bool _isDanger = false;
    private bool _isPlayerKilling = false;
    private bool _isCameraBreaking = false;
    private bool _isVisible = false;
    private bool _soundState = false;
    private bool _stepDown = false;

    public int Health { get; set; }

    private void Start()
    {
        Health = _maxHealth;
        _navMeshAgent.stoppingDistance = _stopDistance;
        EnsurePlayerTarget();
        NotificationsManager.Instance.AddListener(this, "Event_PlayerFire");
    }

    private void EnsurePlayerTarget()
    {
        if (_playerTarget != null) return;

        if (FPSController.Instance != null)
        {
            _playerTarget = FPSController.Instance.transform;
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTarget = player.transform;
    }

    private void Update()
    {
        if (_isPlayerKilling) return;

        if (!_isDanger)
        {
            // Normal patrol behavior
            if (!_isHaunting && !_isPlayerHunting)
            {
                if (_isCameraBreaking)
                {
                    CameraBreak();
                }
                else
                {
                    Patrol();
                }
            }

            if (_isPlayerHunting)
            {
                if (_playerTarget != null && !_playerTarget.GetComponent<FPSController>().IsHidden)
                {
                    PlayerHunt();
                }
                else
                {
                    _isPlayerHunting = false;
                }
            }

            if (_isHaunting)
            {
                Haunting();
            }
        }
        else if (!_isPlayerKilling)
        {
            // Combat behavior (reacting to damage)
            HandleCombatPhase();
        }

        UpdateAnimations();
    }

    /// <summary>
    /// Called when player fires a weapon nearby.
    /// </summary>
    public void Event_PlayerFire(Component sender)
    {
        if (!_isDanger && !_isPlayerKilling)
        {
            float dist = Vector3.Distance(transform.position, sender.transform.position);
            if (dist < 300f)
            {
                SetDangerState();
            }
        }
    }

    /// <summary>
    /// Take damage and trigger combat phases.
    /// </summary>
    public override void TakeDamage(int amount, string damageType)
    {
        Health -= amount;
        NotificationsManager.Instance.PostNotification(this, "OnDamageTaken");

        // Play pain sound
        if (_audioSource != null && _painSounds.Length > 0)
        {
            _audioSource.PlayOneShot(_painSounds[Random.Range(0, _painSounds.Length)]);
        }

        // Check combat phase transitions
        if (damageType == "SniperShot" || damageType == "TrapDamage")
        {
            if (!_isDanger)
            {
                SetDangerState();
            }

            // Check for Low Health phase
            if (Health <= _lowHealthThreshold && _currentCombatPhase != CombatPhase.LowHealth)
            {
                EnterLowHealthPhase();
            }
            // Check for Fake Retreat
            else if (Health <= _fakeRetreatThreshold && _currentCombatPhase == CombatPhase.None)
            {
                EnterFakeRetreat();
            }
            // Check for Blocking (after multiple hits)
            else if (_currentCombatPhase == CombatPhase.None && Random.value < 0.3f)
            {
                EnterBlockingPhase();
            }
        }

        if (Health <= 0)
        {
            Die();
        }
    }

    private void HandleCombatPhase()
    {
        switch (_currentCombatPhase)
        {
            case CombatPhase.LowHealth:
                LowHealthBehavior();
                break;
            case CombatPhase.Blocking:
                BlockingBehavior();
                break;
            case CombatPhase.FakeRetreat:
                FakeRetreatBehavior();
                break;
        }
    }

    #region Patrol Behaviors

    private void Patrol()
    {
        // Check for nearby player to start hunting
        if (_playerTarget != null && Vector3.Distance(_playerTarget.position, transform.position) < 60f)
        {
            SetPlayerHuntState();
            return;
        }

        // Select patrol point
        if (_currentPatrolPoint == Vector3.zero)
        {
            _navMeshAgent.Resume();
            _currentPatrolPoint = _patrolPoints[Random.Range(0, _patrolPoints.Count)].position;
        }

        // At patrol point
        if (Vector3.Distance(_currentPatrolPoint, transform.position) < _stopDistance)
        {
            _navMeshAgent.Stop();
            _animator.SetFloat("Speed", 0f);
            _patrolWaitTimer += Time.deltaTime;

            if (!_soundState)
            {
                PlayIdleSound();
                _soundState = true;
            }

            if (_patrolWaitTimer > 3f)
            {
                _patrolWaitTimer = 0f;
                _soundState = false;
                _currentPatrolPoint = _patrolPoints[Random.Range(0, _patrolPoints.Count)].position;
                _hungryCounter += 50;
            }
        }
        else
        {
            // Moving to patrol point
            _soundState = false;
            if (_navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial || _navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                _animator.SetFloat("Speed", 0f);
                _currentPatrolPoint = FindClosestPatrolPoint();
            }
            else
            {
                _animator.SetBool("Attack", false);
                _navMeshAgent.speed = _walkSpeed;
                if (!_navMeshAgent.pathPending)
                {
                    PlayFootstep(0.7f);
                    _navMeshAgent.SetDestination(_currentPatrolPoint);
                    _animator.SetFloat("Speed", 1f);
                }
                else
                {
                    _animator.SetFloat("Speed", 0f);
                }
            }
        }
    }

    private Vector3 FindClosestPatrolPoint()
    {
        float minDist = float.MaxValue;
        Vector3 closest = _patrolPoints[0].position;

        foreach (var point in _patrolPoints)
        {
            float dist = Vector3.Distance(transform.position, point.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = point.position;
            }
        }
        return closest;
    }

    private void PlayerHunt()
    {
        // Three stages of hunting
        if ((int)_currentPatrolPhase == 0) // Follow - silent stalking
        {
            _currentHuntPoint = _playerTarget.position;
            float dist = Vector3.Distance(_currentHuntPoint, transform.position);

            if (dist < 7f) // Close enough for stealth
            {
                _animator.SetFloat("Speed", 0f);
                _animator.SetBool("Attack", false);
                _navMeshAgent.Stop();
                _huntWaitTimer += Time.deltaTime;

                if (_huntWaitTimer > 10f)
                {
                    _huntWaitTimer = 0f;
                    _isPlayerHunting = false;
                    _navMeshAgent.Resume();
                }

                if (dist < 3f) // Very close - enter danger
                {
                    _navMeshAgent.Resume();
                    _isPlayerHunting = false;
                    _isDanger = true;
                    _huntWaitTimer = 0f;
                }
            }
            else
            {
                _navMeshAgent.Resume();
                _navMeshAgent.SetDestination(_currentHuntPoint);
                _animator.SetBool("Attack", false);
                _navMeshAgent.speed = _walkSpeed;
                _animator.SetFloat("Speed", 1f);
            }
        }
        else if ((int)_currentPatrolPhase == 1) // Stealth - slow walk, massive damage
        {
            // Check if player is looking at Rake
            Vector3 toRake = transform.position - _playerTarget.position;
            float angle = Vector3.Angle(toRake, _playerTarget.forward);

            if (angle < 60f) // Player looking at Rake
            {
                _isVisible = true;
            }
            else if (!_isVisible)
            {
                _isVisible = false;
            }

            if (_isVisible)
            {
                // Player saw us - enter Chase phase
                _currentPatrolPhase = PatrolPhase.Chase;
                _attackType = 0;
            }

            if (_isVisible)
            {
                if (_attackType == 0)
                {
                    _attackType = Random.Range(1, 3); // 1=kick, 2=uppercut
                }

                _currentHuntPoint = _playerTarget.position;
                float dist = Vector3.Distance(_currentHuntPoint, transform.position);

                if (dist < 5f)
                {
                    // Attack
                    _navMeshAgent.Stop();
                    _animator.SetFloat("Speed", 0f);
                    _isPlayerKilling = true;
                    _playerTarget.GetComponent<PlayerMovement>().Freeze();
                    _playerTarget.GetComponent<CameraMouseLook>().CanControl = false;
                    FPSController.Instance.HideWeapon();

                    Vector3 lookPos = _playerTarget.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);
                    _playerTarget.GetComponent<CameraMouseLook>().Focus(_focusPoint);

                    _animator.SetBool("Crouch", false);
                    if (!_soundState)
                    {
                        _soundState = true;
                        _audioSource.PlayOneShot(_attackRoars[Random.Range(0, _attackRoars.Length)]);
                    }

                    if (_attackType == 1)
                    {
                        _animator.SetBool("PlayerAttack", true);
                    }
                    else
                    {
                        _animator.SetBool("PlayerUppercut", true);
                    }

                    _huntWaitTimer += Time.deltaTime;
                    if (_huntWaitTimer > 1.2f)
                    {
                        _audioSource.PlayOneShot(_attackHits[Random.Range(0, _attackHits.Length)]);
                        _isPlayerKilling = false;
                        FPSController.Instance.TakeDamage(10, _attackType == 1 ? "RakeKick" : "RakeUppercut");
                        _playerTarget.GetComponent<PlayerMovement>().Unfreeze();
                        _playerTarget.GetComponent<CameraMouseLook>().CanControl = true;
                        _soundState = false;
                        SetDangerState();
                        _navMeshAgent.Resume();
                        _isDanger = true;
                        _huntWaitTimer = 0f;
                        _isPlayerHunting = false;
                        _isVisible = false;
                        _animator.SetBool("Attack", false);
                        _animator.SetBool("PlayerAttack", false);
                        _animator.SetBool("PlayerUppercut", false);
                    }
                }
                else
                {
                    _navMeshAgent.Resume();
                    _navMeshAgent.SetDestination(_currentHuntPoint);
                    _animator.SetBool("Crouch", false);
                    _animator.SetBool("Attack", false);
                    _navMeshAgent.speed = _runSpeed;
                    PlayFootstep(0.2f);
                    _animator.SetFloat("Speed", 2f);
                }
            }
            else
            {
                // Stealth approach - crouched, slow
                _currentHuntPoint = _playerTarget.position;
                float dist = Vector3.Distance(_currentHuntPoint, transform.position);

                if (dist < 3f)
                {
                    _navMeshAgent.Stop();
                    _animator.SetFloat("Speed", 0f);
                    _isPlayerKilling = true;
                    _playerTarget.GetComponent<PlayerMovement>().Freeze();
                    _playerTarget.GetComponent<CameraMouseLook>().CanControl = false;
                    FPSController.Instance.HideWeapon();

                    Vector3 lookPos = _playerTarget.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);
                    _playerTarget.GetComponent<CameraMouseLook>().Focus(_focusPoint);

                    _animator.SetBool("Crouch", false);
                    if (!_soundState)
                    {
                        _soundState = true;
                        _audioSource.PlayOneShot(_attackRoars[Random.Range(0, _attackRoars.Length)]);
                    }
                    _animator.SetBool("PlayerAttack", true);

                    _huntWaitTimer += Time.deltaTime;
                    if (_huntWaitTimer > 1.2f)
                    {
                        _audioSource.PlayOneShot(_attackHits[Random.Range(0, _attackHits.Length)]);
                        _isPlayerKilling = false;
                        FPSController.Instance.TakeDamage(10, "RakeUppercut");
                        _playerTarget.GetComponent<PlayerMovement>().Unfreeze();
                        _playerTarget.GetComponent<CameraMouseLook>().CanControl = true;
                        _soundState = false;
                        _attackType = 0;
                        SetDangerState();
                        _navMeshAgent.Resume();
                        _isDanger = true;
                        _huntWaitTimer = 0f;
                        _isPlayerHunting = false;
                        _isVisible = false;
                        _animator.SetBool("Attack", false);
                        _animator.SetBool("PlayerAttack", false);
                    }
                }
                else
                {
                    _navMeshAgent.Resume();
                    _navMeshAgent.SetDestination(_currentHuntPoint);
                    _animator.SetBool("Attack", false);
                    _navMeshAgent.speed = _stealthSpeed;
                    _animator.SetBool("Crouch", true);
                    _animator.SetFloat("Speed", 0f);
                }
            }
        }
        else if ((int)_currentPatrolPhase == 2) // Chase - active pursuit
        {
            _currentHuntPoint = _playerTarget.position;
            float dist = Vector3.Distance(_currentHuntPoint, transform.position);

            if (dist < 3f)
            {
                _navMeshAgent.Stop();
                _animator.SetFloat("Speed", 0f);
                _animator.SetBool("Crouch", false);
                _animator.SetBool("Attack", true);
                _huntWaitTimer += Time.deltaTime;

                if (_huntWaitTimer > 5f)
                {
                    _navMeshAgent.Resume();
                    _isDanger = true;
                    _isVisible = false;
                    _huntWaitTimer = 0f;
                    _isPlayerHunting = false;
                    _animator.SetBool("Attack", false);
                }
            }
            else
            {
                _navMeshAgent.Resume();
                _navMeshAgent.SetDestination(_currentHuntPoint);
                _animator.SetBool("Crouch", false);
                _animator.SetBool("Attack", false);
                _navMeshAgent.speed = _chaseSpeed;
                _animator.SetFloat("Speed", 2f);
            }
        }
    }

    private void Haunting()
    {
        // Hunt animals (boars) when hungry
        // Similar to original Animal_AI hunting logic
    }

    #endregion

    #region Combat Phases

    private void SetDangerState()
    {
        if (!_isPlayerKilling)
        {
            _navMeshAgent.Resume();
            _soundState = false;
            _currentEscapePoint = BuildEscapePoint();
            _isDanger = true;
            _animator.SetInteger("WalkType", 1);
            _animator.SetFloat("Speed", 2f);
            _currentCombatPhase = CombatPhase.None;
        }
    }

    private Vector3 BuildEscapePoint()
    {
        float radius = 300f;
        Vector3 randomDir = Random.insideUnitSphere * radius;
        randomDir += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDir, out hit, radius * 2f, NavMesh.AllAreas);
        return hit.position;
    }

    private void EnterLowHealthPhase()
    {
        _currentCombatPhase = CombatPhase.LowHealth;
        _navMeshAgent.speed = _lowHealthSpeed;

        // Scream
        if (_audioSource != null && _lowHealthScream != null)
        {
            _audioSource.PlayOneShot(_lowHealthScream);
        }

        // Disable glow (harder to see)
        SetGlow(false);
    }

    private void LowHealthBehavior()
    {
        // Aggressive pursuit at max speed
        if (_playerTarget != null)
        {
            _navMeshAgent.SetDestination(_playerTarget.position);
            _animator.SetFloat("Speed", 2f);
        }
    }

    private void EnterBlockingPhase()
    {
        _currentCombatPhase = CombatPhase.Blocking;
        _navMeshAgent.Stop();
        _animator.SetBool("Block", true);
        _animator.SetFloat("Speed", 0f);

        StartCoroutine(ExitBlockingPhase());
    }

    private IEnumerator ExitBlockingPhase()
    {
        yield return new WaitForSeconds(Random.Range(8f, 12f));
        _animator.SetBool("Block", false);
        _currentCombatPhase = CombatPhase.None;
        SetDangerState();
    }

    private void BlockingBehavior()
    {
        // Slowly approach player while blocking
        if (_playerTarget != null)
        {
            Vector3 lookPos = _playerTarget.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);

            _navMeshAgent.speed = _walkSpeed * 0.5f;
            _navMeshAgent.SetDestination(_playerTarget.position);
            _animator.SetFloat("Speed", 0.5f);
        }
    }

    private void EnterFakeRetreat()
    {
        _currentCombatPhase = CombatPhase.FakeRetreat;
        _isDanger = false;
        _navMeshAgent.Resume();
        _navMeshAgent.speed = _runSpeed;
        _animator.SetFloat("Speed", 2f);
        _animator.SetInteger("WalkType", 1);

        // Scream
        if (_audioSource != null && _retreatScream != null)
        {
            _audioSource.PlayOneShot(_retreatScream);
        }

        // Run to cave (first escape point)
        if (_escapePoints.Count > 0)
        {
            _currentEscapePoint = _escapePoints[0].position;
        }
        else
        {
            _currentEscapePoint = BuildEscapePoint();
        }

        StartCoroutine(FakeRetreatReturn());
    }

    private IEnumerator FakeRetreatReturn()
    {
        // Wait in cave
        yield return new WaitForSeconds(Random.Range(30f, 60f));

        // Return to patrol
        _currentCombatPhase = CombatPhase.None;
        _isDanger = true;
        SetDangerState();
    }

    private void FakeRetreatBehavior()
    {
        // Run to escape point
        if (Vector3.Distance(_currentEscapePoint, transform.position) < _stopDistance)
        {
            _animator.SetFloat("Speed", 0f);
        }
        else
        {
            _navMeshAgent.SetDestination(_currentEscapePoint);
            _animator.SetFloat("Speed", 2f);
        }
    }

    #endregion

    #region Camera Breaking

    public void SetCameraBreak(Transform cameraTransform, Vector3 viewRot)
    {
        _cameraBreakTarget = cameraTransform;
        _viewRotation = viewRot;
        _isCameraBreaking = true;
        _breakWaitTimer = 0f;
        _soundState = false;
    }

    private void CameraBreak()
    {
        float dist = Vector3.Distance(_currentBreakPoint, transform.position);

        if (dist < 3f)
        {
            _animator.SetBool("CameraBreak", true);
            _animator.SetFloat("Speed", 0f);
            _breakWaitTimer += Time.deltaTime;

            Vector3 targetRot = _viewRotation;
            targetRot.y = transform.position.y;
            transform.LookAt(targetRot);

            if (!_soundState)
            {
                _soundState = true;
                _audioSource.PlayOneShot(_cameraBreakRoar);
            }

            if (_breakWaitTimer > 1.9f)
            {
                _breakWaitTimer = 0f;
                _animator.SetBool("CameraBreak", false);
                if (_cameraBreakTarget != null)
                {
                    var recordCam = _cameraBreakTarget.GetComponent<RecordCamera_obj>();
                    if (recordCam != null) recordCam.SetUnsetupState();
                }
                _isCameraBreaking = false;
            }
        }
        else
        {
            PlayFootstep(0.2f);
            _animator.SetBool("CameraBreak", false);
            _navMeshAgent.speed = _runSpeed;
            _animator.SetBool("Crouch", false);
            _animator.SetInteger("WalkType", 1);
            _animator.SetFloat("Speed", 2f);
            _navMeshAgent.SetDestination(_currentBreakPoint);
        }
    }

    #endregion

    #region State Setters

    public void SetPlayerHuntState()
    {
        EnsurePlayerTarget();
        _navMeshAgent.Resume();
        _soundState = false;
        _animator.SetInteger("WalkType", 1);
        _isPlayerHunting = true;
        _isHaunting = false;
        _currentPatrolPhase = PatrolPhase.Follow;
    }

    public void SetPatrolState()
    {
        _soundState = false;
        _animator.SetBool("Crouch", false);
        _navMeshAgent.Resume();
        _animator.SetInteger("WalkType", 1);
        _playerTarget = null;
    }

    public void SetBreakState()
    {
        _soundState = false;
        _navMeshAgent.Resume();
        _isCameraBreaking = true;
    }

    public void TriggerTrap(float duration)
    {
        // Immobilize for duration
        StartCoroutine(TrapImmobilize(duration));
    }

    private IEnumerator TrapImmobilize(float duration)
    {
        _navMeshAgent.Stop();
        _animator.SetFloat("Speed", 0f);
        yield return new WaitForSeconds(duration);
        if (!_isPlayerKilling)
        {
            _navMeshAgent.Resume();
            SetDangerState();
        }
    }

    #endregion

    #region Helpers

    private void UpdateAnimations()
    {
        // Update glow for Chase phase
        if (_currentPatrolPhase == PatrolPhase.Chase)
        {
            SetGlow(true);
        }
        else if (_currentCombatPhase != CombatPhase.LowHealth)
        {
            SetGlow(false);
        }
    }

    private void SetGlow(bool enabled)
    {
        if (_glowRenderers != null)
        {
            foreach (var r in _glowRenderers)
            {
                if (r != null)
                {
                    if (enabled && _glowMaterial != null)
                    {
                        r.material = _glowMaterial;
                    }
                    else
                    {
                        r.material = r.sharedMaterial; // Reset to default
                    }
                }
            }
        }
    }

    private void PlayIdleSound()
    {
        if (_audioSource != null && _idleSounds.Length > 0)
        {
            _audioSource.PlayOneShot(_idleSounds[Random.Range(0, _idleSounds.Length)]);
        }
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
        if (_audioSource != null && _walkSound != null)
        {
            _audioSource.PlayOneShot(_walkSound);
        }
        yield return new WaitForSeconds(delay);
        _stepDown = false;
    }

    private void Die()
    {
        if (_deadPrefab != null)
        {
            Instantiate(_deadPrefab, transform.position, transform.rotation);
        }

        FPSController.Instance.GetComponent<Animator>().SetBool("Fade", true);
        InformationUI.Instance?.DrawObjective("You Win!");
        Destroy(gameObject);
    }

    #endregion
}