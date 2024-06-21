using UnityEngine;
using UnityEngine.Events;

namespace Starfire
{
  public class PlayerShipController : ShipController
  {
    [SerializeField] private float moveSpeed = 160f;
    private Vector2 keyboardInput = Vector2.zero;

    public UnityEvent<string> OnPlayerOrbitEnter;
    public UnityEvent<string> OnPlayerOrbitExit;

    protected override void Awake()
    {
        base.Awake();

        configuration.SetConfiguration(this, 360, 100, 100, 100);
    }

    protected override void Start()
    {
        shipRigidBody.centerOfMass = Vector2.zero;
        ChunkManager.Instance.AddShip(this);
    }

    protected override void Update()
    {
        base.Update();

        GetKeyboardInput();

        Vector2 mousePosition = GetMouseWorldPosition();
        RotateToVector(mousePosition, 200);

        if (Input.GetMouseButton(0))
        {
            FireProjectileToPosition(mousePosition);
        }
    }

    protected override void FixedUpdate()
    {   
        bool isBoosting = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKey(KeyCode.Space))
        {
            WarpInDirection(keyboardInput.normalized, moveSpeed, 1500f, isBoosting);
        }
        else
        {
            MoveInDirection(keyboardInput.normalized, moveSpeed, 1500f, isBoosting);
            if (isOrbiting is false) ApplyLinearDrag();
        }

        if (isOrbiting)
        {
            OrbitCelestialBody();
            return;
        }
    }

    public override void SetOrbitingBody(CelestialBehaviour orbitingBody, bool isParent = false)
    {
        if (isParent is false)
        {
            OnPlayerOrbitEnter.Invoke("Now orbiting " + orbitingBody.Name);
        }

        base.SetOrbitingBody(orbitingBody, isParent);
    }

    public override void RemoveOrbitingBody()
    {
        if (orbitingBody != null)
        {
            OnPlayerOrbitExit.Invoke("Leaving " + orbitingBody.Name);
        }
        else
        {
            Debug.LogWarning("PlayerShipController.RemoveOrbitingBody: orbitingBody is null");
        }

        base.RemoveOrbitingBody();
    }

    protected override void UpdateTimers()
    {
        if (currentProjectileFireTimer > 0) currentProjectileFireTimer -= Time.deltaTime;

        if (invulnerabilityTimer > 0) invulnerabilityTimer -= Time.deltaTime;
    }

    protected override void OnParticleCollision(GameObject other)
    {
        base.OnParticleCollision(other);

        EnableHealthbar(configuration.Health, configuration.MaxHealth);
    }

    protected override void EnableHealthbar(float currentHealth, float maxHealth)
    {
        UIManager.Instance.UpdatehealthBar(currentHealth, maxHealth);
    }

    private Vector2 GetMouseWorldPosition()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private Vector2 GetKeyboardInput()
    {
        keyboardInput.x = Input.GetAxis("Horizontal");
        keyboardInput.y = Input.GetAxis("Vertical");
        keyboardInput.Normalize();

        return keyboardInput;
    }
  }
}