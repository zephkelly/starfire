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
      }
      else if (c.CompareTag("Planet"))
      {
        if (celestialBehaviour.CelestialBodyType == CelestialBodyType.Planet) return;

        Rigidbody2D planetRigidbody = c.gameObject.GetComponent<Rigidbody2D>();
        SetChildOrbitingObject(planetRigidbody);
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

    public void SetParentOrbitingObject(CelestialBehaviour _parentCelestialBehaviour)
    {
        celestialBehaviour.SetOrbitingBody(_parentCelestialBehaviour);
        Rigidbody2D _parentRigidbody = _parentCelestialBehaviour.GetComponent<Rigidbody2D>();

        orbitingBodies.Add(_parentRigidbody);
        ApplyInstantOrbitalVelocity(_parentRigidbody);
    }

    public void SetChildOrbitingObject(CelestialBehaviour _newOrbitingBody)
    {
        _newOrbitingBody.SetOrbitingBody(celestialBehaviour);
        Rigidbody2D _newOrbitingRigidbody = _newOrbitingBody.GetComponent<Rigidbody2D>();

        orbitingBodies.Add(_newOrbitingRigidbody);
        ApplyInstantOrbitalVelocity(_newOrbitingRigidbody);
    }

    public void SetChildOrbitingObject(Rigidbody2D _newOrbitingBody)
    {
        _newOrbitingBody.GetComponent<CelestialBehaviour>().SetOrbitingBody(celestialBehaviour);

        if (!orbitingBodies.Contains(_newOrbitingBody))
        {
            orbitingBodies.Add(_newOrbitingBody);
        }

        ApplyInstantOrbitalVelocity(_newOrbitingBody);
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

    public void ApplyInstantOrbitalVelocity(Rigidbody2D body, bool orbitClockwise = true)
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