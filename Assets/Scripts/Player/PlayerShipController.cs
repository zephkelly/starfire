using UnityEngine;
using UnityEngine.Events;

namespace Starfire
{
  public class PlayerShipController : ShipController
  {
    private Vector2 keyboardInput = Vector2.zero;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        ConfigureShip();

        shipRigidBody.centerOfMass = Vector2.zero;
        ChunkManager.Instance.AddShip(this);
    }

    public override void ConfigureShip()
    {
        configuration.SetConfiguration(this, 260000, 100, 100, 160, 1500, 200, 6);
    }

    protected override void Update()
    {
        base.Update();

        GetKeyboardInput();

        Vector2 mousePosition = GetMouseWorldPosition();
        RotateToPosition(mousePosition, 200);

        if (Input.GetMouseButton(0))
        {
            FireProjectileToPosition(mousePosition);
        }
    }

    protected override void FixedUpdate()
    {
        UpdateMovement();
        if (HandleOrbiting()) return;
        HandleNonOrbitingBehavior();
    }

    private void UpdateMovement()
    {
        bool isBoosting = true;
        if (Input.GetKey(KeyCode.LeftShift)) isBoosting = false;

        Vector2 inputDirection = keyboardInput.normalized;

        if (Input.GetKey(KeyCode.Space) && isBoosting is true)
        {
            WarpInDirection(inputDirection, configuration.ThrusterMaxSpeed, isBoosting);
        }
        else
        {
            MoveInDirection(inputDirection, configuration.ThrusterMaxSpeed, isBoosting);
        }
    }

    private bool HandleOrbiting()
    {
        if (!isOrbiting) return false;
        
        OrbitCelestialBody();
        return true;
    }

    private void HandleNonOrbitingBehavior()
    {
        if (Input.GetKey(KeyCode.Space)) return;
        ApplyLinearDrag();
    }

    public override void SetOrbitingBody(CelestialBehaviour _orbitingBody, bool returningToParent = false)
    {
        if (returningToParent is false)
        {
            UIManager.Instance.DisplayMinorAlert("Now orbiting " + _orbitingBody.Name);
        }

        // if (returningToParent is true)
        // {
        //     UIManager.Instance.DisplayMinorAlert("Leaving " +  orbitingBody.Name);
        // }

        base.SetOrbitingBody(_orbitingBody, returningToParent);
    }

    public override void RemoveOrbitingBody()
    {
        if (orbitingBody != null)
        {
            UIManager.Instance.DisplayMinorAlert("Leaving " + orbitingBody.Name);
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

        UpdateHealthBar(configuration.Health, configuration.MaxHealth);
    }

    public override void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
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