using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 플레이어 이동 / 시점 / 스태미너 / AK-47 / 카타나 / 조준 / 사망 처리.
/// 애니메이션은 animator 필드가 연결된 경우에만 파라미터를 세팅한다(없어도 동작).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public enum WeaponType { AK47 = 0, Katana = 1 }

    [Header("이동")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 5f;
    public float rollSpeed = 12f;
    public float rollDuration = 0.4f;
    public float standHeight = 2f;
    public float crouchHeight = 1f;

    [Header("스태미너")]
    public float staminaMax = 100f;
    public float sprintDrainPerSec = 20f;
    public float rollCost = 25f;
    public float staminaRegenPerSec = 15f;
    public float staminaRegenDelay = 1f;

    [Header("마우스")]
    public float mouseSensitivity = 150f;

    [Header("AK-47")]
    public int magazineSize = 30;
    public int reserveAmmo = 90;
    public int maxReserveAmmo = 180;
    public float akDamage = 25f;
    public float fireRate = 0.1f;
    public float range = 100f;
    public float reloadTime = 2.5f;
    public float bulletSpeed = 40f;

    [Header("카타나")]
    public float katanaDamage = 40f;
    public float katanaRange = 3f;
    public float katanaCooldown = 0.5f;

    [Header("무기 오브젝트")]
    public GameObject akObject;
    public GameObject katanaObject;
    public TrailRenderer katanaTrail;

    [Header("조준 / 카메라")]
    public Camera playerCam;
    public float defaultFOV = 60f;
    public float adsFOV = 40f;
    public float zoomSpeed = 10f;

    [Header("UI")]
    public Text hpText;
    public Text ammoText;
    public Text staminaText;

    [Header("스탯")]
    public float maxHp = 100f;

    [Header("애니메이션 (선택)")]
    public Animator animator;

    // --- 런타임 상태 (읽기 전용 노출) ---
    public float Hp { get; private set; }
    public float Stamina { get; private set; }
    public int CurrentAmmo { get; private set; }
    public bool IsDead { get; private set; }

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;
    private bool isReloading;
    private bool isRolling;
    private bool isAttacking;
    private float lastStaminaUseTime;
    private float nextFireTime;
    private float nextKatanaTime;
    private WeaponType currentWeapon = WeaponType.AK47;

    // ponytail: 트레이서용 공유 머티리얼 1개만 생성. 이펙트팀 머즐/트레이서 에셋 나오면 SpawnTracer 교체.
    private static Material s_tracerMat;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Hp = maxHp;
        Stamina = staminaMax;
        CurrentAmmo = magazineSize;
    }

    private void Start()
    {
        if (!CompareTag("Player")) gameObject.tag = "Player";
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EquipWeapon(WeaponType.AK47);
    }

    private void Update()
    {
        if (IsDead) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        HandleMouseLook();
        HandleMovement();
        HandleStamina();
        HandleWeaponSwap();
        HandleCombat();
        UpdateAnimator();
        UpdateUI();
    }

    private void HandleMouseLook()
    {
        if (playerCam == null) return;
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation - my, -85f, 85f);
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mx);
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 input = (transform.right * x + transform.forward * z);
        if (input.sqrMagnitude > 1f) input.Normalize();

        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && !isCrouching && z > 0.1f && Stamina > 0f;
        isSprinting = wantsSprint && !isRolling;

        float speed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
        if (!isRolling)
            controller.Move(input * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching && !isRolling)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            SetTrigger("jump");
        }

        if (Input.GetKeyDown(KeyCode.C) && !isRolling)
            SetCrouch(!isCrouching);

        if (Input.GetKeyDown(KeyCode.Q) && isGrounded && !isRolling && Stamina >= rollCost)
        {
            Vector3 dir = input.sqrMagnitude > 0.01f ? input.normalized : transform.forward;
            StartCoroutine(RollRoutine(dir));
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private IEnumerator RollRoutine(Vector3 dir)
    {
        isRolling = true;
        UseStamina(rollCost);
        SetTrigger("roll");
        if (isCrouching) SetCrouch(false);

        float t = 0f;
        while (t < rollDuration)
        {
            controller.Move(dir * rollSpeed * Time.deltaTime); // 수직 이동은 HandleMovement가 계속 처리
            t += Time.deltaTime;
            yield return null;
        }
        isRolling = false;
    }

    private void SetCrouch(bool value)
    {
        isCrouching = value;
        controller.height = value ? crouchHeight : standHeight;
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
    }

    private void HandleStamina()
    {
        if (isSprinting)
            UseStamina(sprintDrainPerSec * Time.deltaTime);
        else if (Time.time - lastStaminaUseTime >= staminaRegenDelay)
            Stamina = Mathf.Min(staminaMax, Stamina + staminaRegenPerSec * Time.deltaTime);
    }

    private void UseStamina(float amount)
    {
        Stamina = Mathf.Max(0f, Stamina - amount);
        lastStaminaUseTime = Time.time;
    }

    private void HandleWeaponSwap()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { EquipWeapon(WeaponType.AK47); return; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { EquipWeapon(WeaponType.Katana); return; }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            EquipWeapon(currentWeapon == WeaponType.AK47 ? WeaponType.Katana : WeaponType.AK47);
    }

    private void EquipWeapon(WeaponType type)
    {
        currentWeapon = type;
        if (akObject != null) akObject.SetActive(type == WeaponType.AK47);
        if (katanaObject != null) katanaObject.SetActive(type == WeaponType.Katana);
        SetInt("weapon", (int)type);
    }

    private void HandleCombat()
    {
        bool ads = currentWeapon == WeaponType.AK47 && Input.GetMouseButton(1) && !isReloading;
        if (playerCam != null)
        {
            float targetFOV = ads ? adsFOV : defaultFOV;
            playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
        SetBool("isAiming", ads);

        if (currentWeapon == WeaponType.AK47)
        {
            if (Input.GetKeyDown(KeyCode.R)) TryReload();

            if (Input.GetMouseButton(0) && !isReloading && Time.time >= nextFireTime)
            {
                if (CurrentAmmo > 0) ShootAK47();
                else TryReload();
            }
        }
        else // Katana
        {
            if (Input.GetMouseButtonDown(0) && !isAttacking && Time.time >= nextKatanaTime)
                StartCoroutine(KatanaAttackRoutine());
        }
    }

    private void ShootAK47()
    {
        CurrentAmmo--;
        nextFireTime = Time.time + fireRate;
        SetTrigger("shoot");

        Vector3 origin = playerCam.transform.position;
        Vector3 dir = playerCam.transform.forward;
        Vector3 endPoint = origin + dir * range;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            ApplyHit(hit, akDamage);
        }
        SpawnTracer(endPoint);
    }

    private IEnumerator KatanaAttackRoutine()
    {
        isAttacking = true;
        nextKatanaTime = Time.time + katanaCooldown;
        SetTrigger("katanaAttack");
        if (katanaTrail != null) katanaTrail.emitting = true;

        Vector3 origin = playerCam.transform.position;
        Vector3 dir = playerCam.transform.forward;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, katanaRange, ~0, QueryTriggerInteraction.Ignore))
            ApplyHit(hit, katanaDamage);

        yield return new WaitForSeconds(0.3f);
        if (katanaTrail != null) katanaTrail.emitting = false;
        yield return new WaitForSeconds(0.1f);
        isAttacking = false;
    }

    /// <summary>부위 콜라이더면 Hitbox로, 아니면 루트 EnemyTarget으로 몸통 데미지.</summary>
    private void ApplyHit(RaycastHit hit, float baseDamage)
    {
        Hitbox box = hit.collider.GetComponent<Hitbox>();
        if (box != null)
        {
            box.Receive(baseDamage);
            return;
        }
        EnemyTarget enemy = hit.collider.GetComponentInParent<EnemyTarget>();
        if (enemy != null) enemy.TakeDamage(baseDamage, false);
    }

    private void SpawnTracer(Vector3 target)
    {
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.name = "Tracer";
        Destroy(b.GetComponent<Collider>());
        b.transform.localScale = Vector3.one * 0.05f;
        b.transform.position = akObject != null ? akObject.transform.position : playerCam.transform.position;

        if (s_tracerMat == null)
        {
            s_tracerMat = new Material(Shader.Find("Unlit/Color")) { color = Color.yellow };
        }
        b.GetComponent<Renderer>().sharedMaterial = s_tracerMat;

        Rigidbody rb = b.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = (target - b.transform.position).normalized * bulletSpeed;
        Destroy(b, 1f);
    }

    private void TryReload()
    {
        if (isReloading || CurrentAmmo >= magazineSize || reserveAmmo <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        SetTrigger("reload");
        yield return new WaitForSeconds(reloadTime);

        int take = Mathf.Min(magazineSize - CurrentAmmo, reserveAmmo);
        CurrentAmmo += take;
        reserveAmmo -= take;
        isReloading = false;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        Hp = Mathf.Max(0f, Hp - damage);
        SetTrigger("hit");
        if (Hp <= 0f) Die();
    }

    private void Die()
    {
        IsDead = true;
        SetTrigger("die");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied();
            return;
        }

        // GameManager 없는 단독 테스트용 폴백
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(RestartAfter(3f));
    }

    /// <summary>회복 아이템 획득. 체력이 이미 최대면 false(획득 안 함).</summary>
    public bool AddHealth(float amount)
    {
        if (IsDead || Hp >= maxHp) return false;
        Hp = Mathf.Min(maxHp, Hp + amount);
        return true;
    }

    /// <summary>탄약 아이템 획득. 예비탄이 이미 최대면 false(획득 안 함).</summary>
    public bool AddAmmo(int amount)
    {
        if (reserveAmmo >= maxReserveAmmo) return false;
        reserveAmmo = Mathf.Min(maxReserveAmmo, reserveAmmo + amount);
        return true;
    }

    private IEnumerator RestartAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        Vector3 hv = controller.velocity; hv.y = 0f;
        animator.SetFloat("moveSpeed", hv.magnitude);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isSprinting", isSprinting);
        animator.SetBool("isRolling", isRolling);
    }

    private void SetTrigger(string n) { if (animator != null) animator.SetTrigger(n); }
    private void SetBool(string n, bool v) { if (animator != null) animator.SetBool(n, v); }
    private void SetInt(string n, int v) { if (animator != null) animator.SetInteger(n, v); }

    private void UpdateUI()
    {
        if (hpText != null) hpText.text = $"HP {Hp:0}";
        if (ammoText != null) ammoText.text = isReloading ? "재장전..." : $"AMMO {CurrentAmmo} / {reserveAmmo}";
        if (staminaText != null) staminaText.text = $"STA {Stamina:0}";
    }
}
