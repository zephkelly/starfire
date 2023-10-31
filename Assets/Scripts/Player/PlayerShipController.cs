using UnityEngine;

namespace Starfire.Ship.Player
{
  public class PlayerShipController : ShipController
  {
    [SerializeField] private float moveSpeed = 160f;
    private Vector2 keyboardInput = Vector2.zero;

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
      Move(keyboardInput, moveSpeed, Input.GetKey(KeyCode.LeftShift));

      base.FixedUpdate();
    }

    private void LookAtMouse()
    {
      Vector2 mouseDirection = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position;

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