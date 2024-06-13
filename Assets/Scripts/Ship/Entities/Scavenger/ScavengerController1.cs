using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace zephkelly.AI.Scavenger
{
//   public class ScavengerController1 : MonoBehaviour
//   {
//     [SerializeField] private SpriteRenderer scavengerSpriteRenderer;
//     [SerializeField] private ParticleSystem scavengerThrusterParticle;
//     [SerializeField] private Light2D scavengerThrusterLight;
//     [Space(10)]
//     [SerializeField] private ShipLaserFire scavengerLaserFire;
//     [SerializeField] private ParticleSystem scavengerLaserParticleSystem;

//     private StateMachine stateMachine;
//     private Rigidbody2D scavengerRigid2D;

//     private Transform playerTransform;

//     public ParticleSystem LaserParticleSystem { get => scavengerLaserParticleSystem; }
//     public ParticleSystem ThrusterParticleSystem { get => scavengerThrusterParticle; }
//     public Light2D ThrusterLight { get => scavengerThrusterLight; }
//     public SpriteRenderer ScavengerSpriteRenderer { get => scavengerSpriteRenderer; }

//     public int maxHealth { get; private set; } = 100;
//     public int health { get; private set; } = 100;
//     public float scavengerSpeed { get; private set; } = 100f;

//     private void Awake()
//     {
//       scavengerRigid2D = GetComponent<Rigidbody2D>();
//     }

//     private void Start()
//     {
//       stateMachine = new StateMachine();

//       ChangeState(new ScavengerIdleState(this));
//     }

//     private void Update()
//     {
//       stateMachine.Update();
//     }

//     private void FixedUpdate()
//     {
//       stateMachine.FixedUpdate();
//       //Linear drag
//       scavengerRigid2D.AddForce(-scavengerRigid2D.velocity * scavengerRigid2D.mass, ForceMode2D.Force);
//       if (scavengerRigid2D.velocity.magnitude < 0.1f) scavengerRigid2D.velocity = Vector2.zero;
//     }

//     public int TakeDamage(int damage)
//     {
//       health -= damage;
//       if (health <= 0)
//       {
//         health = 0;
//         Die();
//       }

//       StartCoroutine(InvulnerabilityFlash());
//       return health;
//     }

//     IEnumerator InvulnerabilityFlash()
//     {
//       scavengerSpriteRenderer.color = Color.red;
//       yield return new WaitForSeconds(0.4f);
//       scavengerSpriteRenderer.color = Color.white;
//     }

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//       if(other.CompareTag("Player") && stateMachine.currentState.GetType() != typeof(ScavengerChaseState))
//       {
//         playerTransform = other.transform;
//         stateMachine.ChangeState(new ScavengerChaseState(this, playerTransform, scavengerRigid2D));
//       }
//     }
    
//     private void OnCollisionEnter2D(Collision2D other)
//     {
//       GameObject otherObject = other.gameObject;

//       if (otherObject.CompareTag("Asteroid"))
//       {
//         var damage = 10;
//         TakeDamage(damage);
//         return;
//       }
//     }







    // private void OnParticleCollision(GameObject hitObject)
    // {
    //   Debug.Log(collisonEvents.Count);

    //   //Grab where our particles are colliding
    //   scavengerLaserParticleSystem.GetCollisionEvents(hitObject, collisonEvents);
    //   Vector2 hitPoint = collisonEvents[0].intersection;

    //   //Add force to the object we hit
    //   var directionOfForce = (hitPoint - (Vector2)transform.position).normalized;
    //   var hitRigid2D = hitObject.GetComponent<Rigidbody2D>();

    //   hitRigid2D.AddForceAtPosition(
    //     directionOfForce * explosionForce,
    //     hitPoint,
    //     ForceMode2D.Impulse
    //   );

    //   if (hitObject.CompareTag("Player"))
    //   {
    //     Debug.Log("Scavenger hit player!");
    //     hitObject.GetComponent<ShipController>().ShipConfig.TakeDamage(10);
    //   }
    //   else if (hitObject.CompareTag("Asteroid"))
    //   {
    //     hitObject.GetComponent<AsteroidController>().TakeDamage(1, hitPoint);
    //   }
    //   else if (hitObject.CompareTag("AsteroidPickup"))
    //   {
    //     var asteroidInfo = hitObject.GetComponent<AsteroidController>().AsteroidInfo;

    //     OcclusionManager.Instance.RemoveAsteroid.Add(asteroidInfo, asteroidInfo.ParentChunk);
    //   }
    //   else if (hitObject.CompareTag("Enemy"))
    //   {
    //     hitObject.GetComponentInParent<ScavengerController>().TakeDamage(10);
    //   }

    //   StartCoroutine(Explosion(0.5f, hitPoint));
    // }

    // IEnumerator Explosion(float duration, Vector2 explosionPoint)
    // {
    //   zephkelly.AudioManager.Instance.PlaySoundRandomPitch("ShipShootImpact", 0.7f, 1.3f);

    //   GameObject explosion = GameObject.Instantiate(explosionPrefab, explosionPoint, Quaternion.identity);
    //   var light = explosion.GetComponent<Light2D>();

    //   float startIntensity = light.intensity;
    //   float _lerp = 0;

    //   while (_lerp < 1)
    //   {
    //     _lerp += Time.deltaTime / duration;
    //     light.intensity = Mathf.Lerp(startIntensity, 0, _lerp);
    //     yield return new WaitForSeconds(0.1f);
    //   }

    //   GameObject.Destroy(light.gameObject);
    // }





//     public void ChangeState(IState newState)
//     {
//       stateMachine.ChangeState(newState);
//     }

//     private void Die()
//     {
//       Destroy(gameObject);
//     }
//   }
}