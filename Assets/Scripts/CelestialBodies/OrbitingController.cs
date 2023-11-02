using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
  public class OrbitingController : MonoBehaviour
  {
    private ICelestialBody celestialController;

    private Rigidbody2D celestialRigidbody;
    private List<Rigidbody2D> orbitingBodies = new List<Rigidbody2D>();

    private const float G = 0.2f;   //Newtons Gravity constant

    private void Awake()
    {
      celestialController = gameObject.GetComponent<ICelestialBody>();
      celestialRigidbody = gameObject.GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D c) 
    {
      if (c.CompareTag("Player"))
      {
        c.gameObject.GetComponent<ShipController>().SetOrbitingBody(celestialController);
        Debug.Log("Setting orbiting body around: " + celestialController.CelestialBodyType);
        return;
      }
      else if (c.CompareTag("Planet"))
      {
        if (celestialController.CelestialBodyType == CelestialBodyType.Planet) return;

        Rigidbody2D planetRigidbody = c.gameObject.GetComponent<Rigidbody2D>();
        c.gameObject.GetComponent<ICelestialBody>().SetOrbitingBody(celestialController);

        orbitingBodies.Add(planetRigidbody);
        ApplyInstantOrbitalVelocity(planetRigidbody);
        return;
      }
    }

    private void OnTriggerExit2D(Collider2D c) 
    {
      if (c.CompareTag("Player"))
      {
        if (celestialController.ParentOrbitingBody is not null)
        {
          c.gameObject.GetComponent<ShipController>().SetOrbitingBody(celestialController.ParentOrbitingBody);
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

    public void ApplyInstantOrbitalVelocity(Rigidbody2D body)
    {
      float bodyMass = body.mass;
      float starMass = celestialRigidbody.mass;
      float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);

      Vector2 directionToStar = (celestialRigidbody.position - body.position).normalized;
      Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);

      Vector2 appliedOrbitalVelocity = perpendicularDirection * Mathf.Sqrt((G * starMass) / distanceToStar);

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
      var distanceNormalized = _objectDistance / celestialController.MaxOrbitRadius;

      var trueDistance = 1 - distanceNormalized;

      return Mathf.Lerp(0, celestialController.Temperature, trueDistance);
    }
  }
}