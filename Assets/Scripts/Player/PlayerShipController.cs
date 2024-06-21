using UnityEngine;
using UnityEngine.Events;

namespace Starfire
{
  public class PlayerShipController : ShipController
  {
    private Vector2 keyboardInput = Vector2.zero;

    // public UnityEvent<string> OnPlayerOrbitEnter;
    // public UnityEvent<string> OnPlayerOrbitExit;

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

    protected override void ConfigureShip()
    {
        configuration.SetConfiguration(this, 360, 100, 100, 160, 1500, 200);
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
        bool isBoosting = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKey(KeyCode.Space))
        {
            WarpInDirection(keyboardInput.normalized, configuration.ThrusterMaxSpeed, isBoosting);
        }
        else
        {
            MoveInDirection(keyboardInput.normalized, configuration.ThrusterMaxSpeed, isBoosting);
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
            UIManager.Instance.DisplayMinorAlert("Now orbiting " + orbitingBody.Name);
        }

        base.SetOrbitingBody(orbitingBody, isParent);
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