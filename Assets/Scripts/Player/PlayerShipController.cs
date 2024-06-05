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
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        GetKeyboardInput();
        LookAtMouse();
    }

    protected override void FixedUpdate()
    {   
        Move(keyboardInput.normalized, moveSpeed, Input.GetKey(KeyCode.LeftShift));

        base.FixedUpdate();
    }

    public override void SetOrbitingBody(CelestialBehaviour orbitingBody, bool isParent = false)
    {
        if (isParent is false)
        {
            OnPlayerOrbitEnter.Invoke("Now orbiting " + orbitingBody.CelestialName);
        }

        base.SetOrbitingBody(orbitingBody, isParent);
    }

    public override void RemoveOrbitingBody()
    {
      if (orbitingBody != null)
      {
        OnPlayerOrbitExit.Invoke("Leaving " + orbitingBody.CelestialName);
      }
      else
      {
        Debug.LogWarning("PlayerShipController.RemoveOrbitingBody: orbitingBody is null");
      }

      base.RemoveOrbitingBody();
    }

    private void LookAtMouse()
    {
      mouseDirection = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position;

      float angle = Mathf.Atan2(mouseDirection.y, mouseDirection.x) * Mathf.Rad2Deg;
      transform.rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
    }

    private void GetKeyboardInput()
    {
      keyboardInput.x = Input.GetAxis("Horizontal");
      keyboardInput.y = Input.GetAxis("Vertical");
      keyboardInput.Normalize();
    }
  }
}