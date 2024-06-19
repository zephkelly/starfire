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

        configuration.SetConfiguration(this, 1000, 100, 100, 100);
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        GetKeyboardInput();

        Vector2 mouseDirection = GetMouseWorldPosition();
        RotateToVector(mouseDirection, 200);


        if (Input.GetMouseButton(0))
        {
            AimWeapons(GetMouseWorldPosition());
            FireProjectile();
        }
    }

    protected override void FixedUpdate()
    {   
        if (Input.GetKey(KeyCode.Space))
        {
            MoveInDirection(keyboardInput.normalized, 1500, Input.GetKey(KeyCode.LeftShift));
        }
        else
        {
            MoveInDirection(keyboardInput.normalized, moveSpeed, Input.GetKey(KeyCode.LeftShift));
        }

        base.FixedUpdate();
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

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return mousePosition;
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