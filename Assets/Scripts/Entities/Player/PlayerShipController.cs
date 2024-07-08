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
        shipRigidBody.centerOfMass = Vector2.zero;
        
        var newConfiguration = new ShipConfiguration(this, 2600, 100, 100, 100, 160, 1500, 200);
        var newTransponder = new Transponder("Player", Faction.Player, 90000);
        var newInventory = new Inventory();
    
        var newShip = new Ship(this, new StandardAICore(), newConfiguration, newTransponder, newInventory);
        
        ChunkManager.Instance.AddShip(this);
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

        // When we orbit a planet we want to increase the speed so we can account for gravity
        float newWarpSpeed = Ship.Configuration.WarpIncrementSpeed;
        if (IsOrbiting) newWarpSpeed = Ship.Configuration.ThrusterMaxSpeed;

        if (Input.GetKey(KeyCode.Space) && isBoosting is true)
        {
            WarpInDirection(inputDirection, newWarpSpeed, isBoosting);
            UpdateWarpFuel(Ship.Configuration.WarpFuel, Ship.Configuration.MaxWarpFuel);
        }
        else
        {
            MoveInDirection(inputDirection, Ship.Configuration.ThrusterMaxSpeed, isBoosting);
        }
    }

    private bool HandleOrbiting()
    {
        if (IsOrbiting is false) return false;
        
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

        base.SetOrbitingBody(_orbitingBody, returningToParent);
    }

    public override void RemoveOrbitingBody()
    {
        if (OrbitingBody != null)
        {
            UIManager.Instance.DisplayMinorAlert("Leaving " + OrbitingBody.Name);
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

        UpdateHealth(Ship.Configuration.Health, Ship.Configuration.MaxHealth);
    }

    public override void UpdateHealth(float currentHealth, float maxHealth)
    {
        UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }

    public override void UpdateWarpFuel(float currentWarpFuel, float maxWarpFuel)
    {
        UIManager.Instance.UpdateWarpFuelBar(currentWarpFuel, maxWarpFuel);
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