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
        orbitingBodies.Add(c.gameObject.GetComponent<Rigidbody2D>());
      }
      else if (c.CompareTag("Planet"))
      {
        if (celestialBehaviour.CelestialBodyType == CelestialBodyType.Planet) return;

        Rigidbody2D planetRigidbody = c.gameObject.GetComponent<Rigidbody2D>();
        c.gameObject.GetComponent<CelestialBehaviour>().SetOrbitingBody(celestialBehaviour);

        orbitingBodies.Add(planetRigidbody);
        ApplyInstantOrbitalVelocity(planetRigidbody);
      }
    }

    private void OnTriggerExit2D(Collider2D c) 
    {
        if (c.CompareTag("Player") != true) return;
        ShipController playerController = c.gameObject.GetComponent<ShipController>();

        if (celestialBehaviour.ParentOrbitingBody == null) 
        {
            orbitingBodies.Remove(c.gameObject.GetComponent<Rigidbody2D>());
            playerController.RemoveOrbitingBody(); 
            return;
        }

        if (playerController.OrbitingBody == null)
        {
            playerController.SetOrbitingBody(celestialBehaviour.ParentOrbitingBody, isParent: true);
            return;
        }

        if (playerController.OrbitingBody.Mass > celestialBehaviour.Mass && playerController.OrbitingBody.CelestialBodyType is CelestialBodyType.Planet) return;
        playerController.SetOrbitingBody(celestialBehaviour.ParentOrbitingBody, isParent: true);
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

            float bodyMass = GetBodyMass(body);
            float parentBodyMass = GetBodyMass(celestialRigidbody, celestialBehaviour);
            float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);
            float gravitationalForce = G * bodyMass * parentBodyMass / (distanceToStar * distanceToStar);

            body.AddForce((celestialRigidbody.position - body.position).normalized * gravitationalForce);

            Debug.Log("Apply gravity to: " + body.name + " with force: " + gravitationalForce);
        }
    }

    public void ApplyInstantOrbitalVelocity(Rigidbody2D body, bool orbitClockwise = true)
    {
        // float bodyMass = body.mass;
        float parentBodyMass = GetBodyMass(celestialRigidbody, celestialBehaviour);
        float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);

        Vector2 appliedOrbitalVelocity = GetOrbitDirection(body) * Mathf.Sqrt((G * parentBodyMass) / distanceToStar);

        //Only apply enough force to orbit the star
        if (body.velocity.magnitude > appliedOrbitalVelocity.magnitude) return;
        
        var deltaVelocity = appliedOrbitalVelocity - body.velocity;
        body.velocity += deltaVelocity;
    }

    public Vector2 GetOrbitalVelocity(Rigidbody2D body)
    {
        float bodyMass = GetBodyMass(celestialRigidbody, celestialBehaviour);
        float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);

        Vector2 orbitalVelocity = GetOrbitDirection(body) * Mathf.Sqrt((G * bodyMass) / distanceToStar);
        return orbitalVelocity + celestialRigidbody.velocity;
    }

    public Vector2 GetOrbitDirection(Rigidbody2D body)
    {
        Vector2 directionToStar = (celestialRigidbody.position - body.position).normalized;
        Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);

        if (Vector2.Dot(body.velocity, perpendicularDirection) < 0)
        {
            // return -perpendicularDirection;
        }

        return perpendicularDirection;
    }

    public float GetThermalGradient(float _objectDistance)
    {
        var distanceNormalized = _objectDistance / celestialBehaviour.MaxOrbitRadius;

        var trueDistance = 1 - distanceNormalized;

        return Mathf.Lerp(0, celestialBehaviour.Temperature, trueDistance);
    }

    private float GetBodyMass(Rigidbody2D body, CelestialBehaviour celestialBehaviour = null)
    {
        float bodyMass = body.mass;

        if (celestialBehaviour != null && celestialBehaviour.CelestialBodyType == CelestialBodyType.Star)
        {
            bodyMass *= 10;
        }

        return bodyMass;
    }
  }
}