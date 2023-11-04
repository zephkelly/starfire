using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
  public class OrbitingController : MonoBehaviour
  {
    private CelestialBehaviour celestialBehaviour;

    private Rigidbody2D celestialRigidbody;
    private List<Rigidbody2D> orbitingBodies = new List<Rigidbody2D>();

    private const float G = 0.2f;   //Newtons Gravity constant

    private void Awake()
    {
      celestialBehaviour = gameObject.GetComponent<CelestialBehaviour>();
      celestialRigidbody = gameObject.GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D c) 
    {
      if (c.CompareTag("Player"))
      {
        c.gameObject.GetComponent<ShipController>().SetOrbitingBody(celestialBehaviour);
        Debug.Log("Setting orbiting body around: " + celestialBehaviour.CelestialBodyType);
        return;
      }
      else if (c.CompareTag("Planet"))
      {
        if (celestialBehaviour.CelestialBodyType == CelestialBodyType.Planet) return;

        Rigidbody2D planetRigidbody = c.gameObject.GetComponent<Rigidbody2D>();
        c.gameObject.GetComponent<CelestialBehaviour>().SetOrbitingBody(celestialBehaviour);

        orbitingBodies.Add(planetRigidbody);
        ApplyInstantOrbitalVelocity(planetRigidbody);
        return;
      }
    }

    // private void OnTriggerStay2D(Collider2D c) 
    // {
    //   // IEnumerator CheckBodiesCoroutine() 
    //   // {
    //     // if (c.CompareTag("Player"))
    //     // {
    //     //   if (celestialBehaviour.ParentOrbitingBody is not null)
    //     //   {
    //     //     ShipController objectController = c.gameObject.GetComponent<ShipController>();

    //     //     if (objectController.OrbitingBody.Mass > celestialBehaviour.Mass && objectController.OrbitingBody.CelestialBodyType is CelestialBodyType.Planet) return;
    //     //     objectController.SetOrbitingBody(celestialBehaviour.ParentOrbitingBody);
    //     //     return;
    //     //   }

    //     //   c.gameObject.GetComponent<ShipController>().RemoveOrbitingBody();
    //     //   return;
    //     // }

    //     // yield return new WaitForSeconds(5);
    //   // }

    //   // StartCoroutine(CheckBodiesCoroutine());
    // }

    private void OnTriggerExit2D(Collider2D c) 
    {
      if (c.CompareTag("Player"))
      {
        if (celestialBehaviour.ParentOrbitingBody is not null)
        {
          ShipController objectController = c.gameObject.GetComponent<ShipController>();

          if (objectController.OrbitingBody.Mass > celestialBehaviour.Mass && objectController.OrbitingBody.CelestialBodyType is CelestialBodyType.Planet) return;
          objectController.SetOrbitingBody(celestialBehaviour.ParentOrbitingBody);
          return;
        }

          c.gameObject.GetComponent<ShipController>().RemoveOrbitingBody();
        return;
      }
    }

    private void FixedUpdate()
    {
      Gravity();
    }

    private void Gravity()
    {
      for (int i = 0; i < orbitingBodies.Count; i++)
      {
        Rigidbody2D body = orbitingBodies[i];

        float bodyMass = body.mass;
        float starMass = celestialRigidbody.mass;
        float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);

        //Newtons gravitational theory
        float gravitationalForce = (G * bodyMass * starMass) / (distanceToStar * distanceToStar);

        body.AddForce((celestialRigidbody.position - body.position).normalized * gravitationalForce);
      }
    }

    public void ApplyInstantOrbitalVelocity(Rigidbody2D body, bool counterClockwise = false)
    {
      float bodyMass = body.mass;
      float starMass = celestialRigidbody.mass;
      float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);

      Vector2 directionToStar = (celestialRigidbody.position - body.position).normalized;
      Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);

      Vector2 appliedOrbitalVelocity = perpendicularDirection * Mathf.Sqrt((G * starMass) / distanceToStar);


      // if (Vector2.Dot(body.velocity, perpendicularDirection) < 0 || counterClockwise)
      // {
      //   appliedOrbitalVelocity *= -1;
      // }

      //Only apply enough force to orbit the star
      if (body.velocity.magnitude > appliedOrbitalVelocity.magnitude) return;
      
      var deltaVelocity = appliedOrbitalVelocity - body.velocity;
      body.velocity += deltaVelocity;
    }

    public Vector2 GetOrbitalVelocity(Rigidbody2D body)
    {
      float starMass = celestialRigidbody.mass;
      float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);

      Vector2 directionToStar = (celestialRigidbody.position - body.position).normalized;
      Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);

      Vector2 orbitalVelocity = perpendicularDirection * Mathf.Sqrt((G * starMass) / distanceToStar);

      return orbitalVelocity + celestialRigidbody.velocity;
    }

    public float GetThermalGradient(float _objectDistance)
    {
      var distanceNormalized = _objectDistance / celestialBehaviour.MaxOrbitRadius;

      var trueDistance = 1 - distanceNormalized;

      return Mathf.Lerp(0, celestialBehaviour.Temperature, trueDistance);
    }
  }
}