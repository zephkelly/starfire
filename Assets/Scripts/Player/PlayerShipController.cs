using UnityEngine;
using UnityEngine.Events;

namespace Starfire
{
  public class PlayerShipController : ShipController
  {
    [SerializeField] private float moveSpeed = 160f;
    private Vector2 keyboardInput = Vector2.zero;
    private Vector2 mouseDirection = Vector2.zero;

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
        Quaternion mouseRotation = GetMouseQuaternion();
        float maxDegreesPerSecond = 145.0f; // Change this value to your desired rotation speed
        float maxStep = maxDegreesPerSecond * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, mouseRotation, maxStep);

        //turn mouse

        Debug.DrawLine(transform.position, (Vector2)transform.position + mouseDirection, Color.red);

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
            Move(keyboardInput.normalized, 1500, Input.GetKey(KeyCode.LeftShift));
        }
        else
        {
            Move(keyboardInput.normalized, moveSpeed, Input.GetKey(KeyCode.LeftShift));
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

    private Quaternion GetMouseQuaternion()
    {
        mouseDirection = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position;

        float angle = Mathf.Atan2(mouseDirection.y, mouseDirection.x) * Mathf.Rad2Deg;
        return Quaternion.AngleAxis(angle - 90, Vector3.forward);
    }

    private Vector2 GetMouseWorldPosition()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    
    }

    private void GetKeyboardInput()
    {
      keyboardInput.x = Input.GetAxis("Horizontal");
      keyboardInput.y = Input.GetAxis("Vertical");
      keyboardInput.Normalize();
    }
  }
}