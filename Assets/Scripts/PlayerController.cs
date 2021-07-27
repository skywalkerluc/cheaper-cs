using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // get & set doesn't work
    public Transform viewPoint;
    public float mouseSensitivity = 0.5f;
    public float verticalRotStore;
    public Vector2 mouseInput;
    public bool invertLook = true;
    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDirection;
    private Vector3 movement;
    public CharacterController charController;
    private Camera camera;
    private float jumpForce = 7.5f;
    public float gravityModifier = 2.5f;
    public Transform groundedStore;
    private bool isGrounded;
    public LayerMask groundLayers;
    public GameObject bulletImpact;
    // public float timeBetweenShots = .1f;
    private float shotCounter;
    public float muzzleDisplayTime = 0.02f;
    private float muzzleCounter;

    public float maxHeat = 10f, /*heatPerShot = 1f, */ coolRate = 3f, overHeatCoolrate = 5f;
    private float heatCounter;
    private bool overHeated;

    public Gun[] allGuns;
    private int selectedGun;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        camera = Camera.main;

        UIController.instance.weaponSlider.maxValue = maxHeat;
        SwitchGun();

        Transform newTransform = SpawnManager.instance.GetRandomSpawnPoint();
        transform.position = newTransform.position;
        transform.rotation = newTransform.rotation;
    }

    void Update()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        verticalRotStore += mouseInput.y;
        verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

        if(invertLook) {
            viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        } else {
            viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }

        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if(Input.GetKey(KeyCode.LeftShift)){
            activeMoveSpeed = runSpeed;
        } else {
            activeMoveSpeed = moveSpeed;
        }
        float yVelocity = movement.y;
        movement = ((transform.right * moveDirection.z) + (transform.forward * (moveDirection.x * -1))).normalized * activeMoveSpeed;
        movement.y = yVelocity;

        if(charController.isGrounded)
        {
            movement.y = 0f;
        }

        isGrounded = Physics.Raycast(groundedStore.position, Vector3.down, .25f, groundLayers);

        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }

        movement.y += Physics.gravity.y * Time.deltaTime * gravityModifier;
        charController.Move(movement * Time.deltaTime);

        if(allGuns[selectedGun].muzzleFlash.activeInHierarchy)
        {
            muzzleCounter -= Time.deltaTime;

            if(muzzleCounter <= 0)
            {
                allGuns[selectedGun].muzzleFlash.SetActive(false);
            }
        }
        HandleShoot();
        HandleGunChanger();

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        } else if(Cursor.lockState == CursorLockMode.None)
        {
            if(Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void HandleGunChanger()
    {
        if(Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedGun++;

            if(selectedGun >= allGuns.Length)
            {
                selectedGun = 0;
            }
            SwitchGun();
        } else if(Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedGun--;
            if(selectedGun < 0)
            {
                selectedGun = allGuns.Length - 1;
            }
            SwitchGun();
        }

        for(int i = 0; i < allGuns.Length; i++)
        {
            if(Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun = i;
                SwitchGun();
            }
        }
    }

    private void HandleShoot()
    {
        if(!overHeated) {
            if(Input.GetMouseButtonDown(0))
            {
                Shoot();
            }

            if(Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
            {
                shotCounter -= Time.deltaTime;

                if(shotCounter <= 0)
                {
                    Shoot();
                }
            }
            heatCounter -= coolRate * Time.deltaTime;
        } else
        {
            heatCounter -= overHeatCoolrate * Time.deltaTime;
            if(heatCounter <= 0)
            {
                overHeated = false;
                UIController.instance.overheatedMessage.gameObject.SetActive(false);
            }
        }

        if(heatCounter < 0)
        {
            heatCounter = 0f;
        }

        UIController.instance.weaponSlider.value = heatCounter;
    }

    private void Shoot()
    {
        // finding the center of the screen
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        ray.origin = camera.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject bulletImpactObj = Instantiate(bulletImpact, hit.point, Quaternion.LookRotation(hit.normal, Vector3.down));
            Destroy(bulletImpactObj, 10f);
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        HandleReload();
        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }

    private void HandleReload()
    {
        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;
            // FindObjectOfType<UIController>().overheatedMessage.gameObject.SetActive(true);
            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }
    }

    private void LateUpdate()
    {
        camera.transform.position = viewPoint.position;
        camera.transform.rotation = viewPoint.rotation;
    }

    private void SwitchGun()
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }
}
