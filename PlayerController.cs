using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;        
    public float runSpeed = 8f;         
    public float crouchSpeed = 2.5f;    
    public float jumpForce = 5f;        
    public float rollForce = 15f;       
    public float crouchHeight = 1.0f;   
    public float standHeight = 2.0f;    
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching = false;

    [Header("Mouse Look")]
    public float mouseSensitivity = 150f;
    private float xRotation = 0f;

    [Header("AK-47 Gun & Bullet Settings")]
    public int currentAmmo = 30;         
    public int maxAmmo = 120;            
    public bool isReloading = false;     
    [SerializeField] private float fireRate = 0.1f; 
    [SerializeField] private float range = 100f;    
    private float nextFireTime = 0f;
    public float bulletSpeed = 40f;

    [Header("Katana & Weapon Swap")]
    public GameObject akObject;          
    public GameObject katanaObject;      
    public TrailRenderer katanaTrail;    
    private const float KATANA_DAMAGE = 40.0f; 
    private bool isAttacking = false;    
    private enum WeaponType { AK47, Katana }
    private WeaponType currentWeapon = WeaponType.AK47;

    [Header("UI & Camera Zoom")]
    public Camera playerCam;             
    public Text hpText;                  
    public Text ammoText;                
    public float hp = 100f;              
    private float defaultFOV = 60f;      
    private float zoomFOV = 40f;         
    private float zoomSpeed = 10f;       

    void Start()
    {
        gameObject.tag = "Player";
        controller = GetComponent<CharacterController>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SwapTo(WeaponType.AK47);
    }

    void Update()
    {
        HandleMouseLook();   
        HandleMovement();    
        HandleWeaponSwap();  
        HandleCombat();      
        UpdateUI();          
    }

    void HandleMouseLook()
    {
        if (playerCam == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        playerCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching) currentSpeed = runSpeed;
        else if (isCrouching) currentSpeed = crouchSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
            controller.height = isCrouching ? crouchHeight : standHeight;
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded)
        {
            Vector3 rollDir = transform.forward * rollForce;
            controller.Move(rollDir * Time.deltaTime * 5f);
        }
    }

    private void HandleWeaponSwap()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwapTo(WeaponType.AK47);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwapTo(WeaponType.Katana);
    }

    private void SwapTo(WeaponType type)
    {
        currentWeapon = type;
        if (akObject != null) akObject.SetActive(type == WeaponType.AK47);
        if (katanaObject != null) katanaObject.SetActive(type == WeaponType.Katana);
    }

    void HandleCombat()
    {
        bool isAttackTriggered = Input.GetButton("Fire1") || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0));

        if (currentWeapon == WeaponType.AK47)
        {
            if (!isReloading)
            {
                if (isAttackTriggered && Time.time >= nextFireTime)
                {
                    if (currentAmmo > 0) ShootAK47();
                    else StartCoroutine(ReloadRoutine());
                }
            }

            if (Input.GetKeyDown(KeyCode.R) && currentAmmo < 30 && !isReloading)
            {
                StartCoroutine(ReloadRoutine());
            }

            if (Input.GetMouseButton(1))
                playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, zoomFOV, Time.deltaTime * zoomSpeed);
            else
                playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, defaultFOV, Time.deltaTime * zoomSpeed);
        }
        else if (currentWeapon == WeaponType.Katana)
        {
            bool isMeleeTriggered = Input.GetMouseButtonDown(0) || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0));
            if (isMeleeTriggered && !isAttacking) StartCoroutine(KatanaAttackRoutine());
        }
    }

    private void ShootAK47()
    {
        currentAmmo--;
        nextFireTime = Time.time + fireRate;

        RaycastHit hit;
        Vector3 targetPoint = playerCam.transform.position + playerCam.transform.forward * range;

        if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out hit, range))
        {
            targetPoint = hit.point;

            string targetTag = hit.collider.gameObject.tag;
            if (targetTag == "Enemy" || targetTag == "HiddenEnemy")
            {
                EnemyTarget enemy = hit.transform.GetComponentInParent<EnemyTarget>();
                bool isHidden = (targetTag == "HiddenEnemy");

                if (enemy != null)
                {
                    if (hit.collider.name == "Head") 
                    {
                        enemy.TakeDamage(80f, true, isHidden);
                        Debug.Log($"<color=red><b>[HEADSHOT!]</b></color> 무기: AK-47 | 대상: {hit.collider.transform.root.name} | 데미지: <b>80</b>");
                    }
                    else 
                    {
                        enemy.TakeDamage(25f, false, isHidden);
                        Debug.Log($"<color=orange>[BODY HIT]</color> 무기: AK-47 | 대상: {hit.collider.transform.root.name} | 데미지: <b>25</b>");
                    }
                }
            }
        }

        CreateVisualBullet(targetPoint);
    }

    private void CreateVisualBullet(Vector3 targetPosition)
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Temp_Bullet";
        bullet.transform.position = akObject != null ? akObject.transform.position : playerCam.transform.position;
        bullet.transform.localScale = new Vector3(0.08f, 0.08f, 0.2f);

        Destroy(bullet.GetComponent<Collider>());

        Renderer rend = bullet.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Unlit/Color"));
            rend.material.color = Color.yellow;
        }

        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        Vector3 direction = (targetPosition - bullet.transform.position).normalized;
        rb.velocity = direction * bulletSpeed;

        Destroy(bullet, 1.5f);
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(5.0f);
        currentAmmo = 30;
        isReloading = false;
    }

    private IEnumerator KatanaAttackRoutine()
    {
        isAttacking = true;
        if (katanaTrail != null) katanaTrail.emitting = true;

        RaycastHit hit;
        if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out hit, 3.0f))
        {
            string targetTag = hit.collider.gameObject.tag;
            if (targetTag == "Enemy" || targetTag == "HiddenEnemy")
            {
                EnemyTarget enemy = hit.transform.GetComponentInParent<EnemyTarget>();
                bool isHidden = (targetTag == "HiddenEnemy");

                if (enemy != null) 
                {
                    enemy.TakeDamage(KATANA_DAMAGE, false, isHidden);
                    Debug.Log($"<color=cyan>[KATANA SWING]</color> 무기: 카타나 | 대상: {hit.collider.transform.root.name} | 데미지: <b>40</b>");
                }
            }
        }

        yield return new WaitForSeconds(0.3f);
        if (katanaTrail != null) katanaTrail.emitting = false;
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    private void UpdateUI()
    {
        if (hpText != null) hpText.text = $"HP: {hp:0}";
        if (ammoText != null) ammoText.text = isReloading ? "RELOADING..." : $"AMMO: {currentAmmo} / 30";
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hp <= 0) hp = 0;
    }
}