using UnityEngine;

namespace Starfire.Ship
{
  public interface IShipController
  {
    ShipConfiguration Configuration { get; }
    // ShipInventory Inventory { get; }
    bool IsOrbiting { get; }
    // bool SetOrbitingBody(ICelestialBody orbitingBody);

    int Damage(int damage, DamageType damageType);
    void Repair(int repair, DamageType damageType);
    void Move(Vector2 direction);
    void Rotate(Vector2 direction);
  }

  [RequireComponent(typeof(Rigidbody2D))]
  public abstract class ShipController : MonoBehaviour, IShipController
  {
    protected ShipConfiguration configuration;
    // protected ShipInventory inventory;
    protected Rigidbody2D rigidBody;
    protected bool isOrbiting = false;

    public ShipConfiguration Configuration => configuration;
    // public ShipInventory Inventory => inventory;
    public bool IsOrbiting => isOrbiting;

    protected virtual void Awake()
    {
      // configuration = new ShipConfiguration();
      rigidBody = GetComponent<Rigidbody2D>();
    }

    protected virtual void FixedUpdate()
    {

    }

    protected void StarOrbiting()
    {
      // if (starController == null) {
      //   Debug.LogError("Error: StarOrbiting() null reference");
      //   return;
      // }

      // //MSet constant orbit velocity
      // lastOrbitalVelocity = orbitalVelocity;
      // orbitalVelocity = starController.OrbitingBehaviour.GetOrbitalVelocity(playerRigid2D);

      // playerRigid2D.velocity -= lastOrbitalVelocity;   //Working around unity physics
      // playerRigid2D.velocity += orbitalVelocity;

      // var orbitalDragX = new Vector2(orbitalVelocity.x - playerRigid2D.velocity.x, 0);
      // var orbitalDragY = new Vector2(0, orbitalVelocity.y - playerRigid2D.velocity.y);

      // //Orbital drag
      // if (playerRigid2D.velocity.x > orbitalVelocity.x || playerRigid2D.velocity.x < orbitalVelocity.x) {
      //   playerRigid2D.AddForce(orbitalDragX * playerRigid2D.mass, ForceMode2D.Force);
      // }

      // if (playerRigid2D.velocity.y > orbitalVelocity.y || playerRigid2D.velocity.y < orbitalVelocity.y) {
      //   playerRigid2D.AddForce(orbitalDragY * playerRigid2D.mass, ForceMode2D.Force);
      // } 
    }

    public abstract void Move(Vector2 direction);

    public abstract void Rotate(Vector2 direction);

    public int Damage(int damage, DamageType damageType) => configuration.Damage(damage, damageType);

    public void Repair(int repair, DamageType damageType) => configuration.Repair(repair, damageType);
  }
}