using System;
using UnityEngine;

namespace Starfire
{
    public enum CelestialBodyType
    {
        Planet,
        Star,
        Moon,
        Asteroid
    }

    [RequireComponent(typeof(OrbitingController))]
    public abstract class CelestialBehaviour : MonoBehaviour, ICelestialBody
    { 
        protected CelestialBodyType _celestialBodyType;
        protected OrbitingController _orbitController;
        protected CelestialParallaxLayer _celestialParallaxLayer;
        protected Material[] _celestialMaterials;
        [SerializeField] public GameObject[] celestialComponents;

        protected Rigidbody2D _celestialRigidBody;
        protected Transform _celestialTransform;
        protected CelestialBehaviour _parentOrbitingBody;
        protected CelestialBehaviour _childOrbitingBody;

        public CelestialBodyType CelestialBodyType => _celestialBodyType;
        public OrbitingController OrbitController => _orbitController;
        public CelestialBehaviour ParentOrbitingBody => _parentOrbitingBody;
        public CelestialBehaviour ChildOrbitingBody => _childOrbitingBody;
        public CelestialParallaxLayer GetParallaxLayer { get => _celestialParallaxLayer; }
        public Vector2 WorldPosition => _celestialTransform.position;

        public void NewPixelAmount(float amount) => SetPixel(amount);

        public string Name { get; protected set; }
        public float Radius { get; protected set; }
        public float Mass { get; protected set; }
        public float Temperature { get; private set; }
        public bool IsOrbiting { get; private set; }
        
        public void SetupCelestialBehaviour(CelestialBodyType type, float radius, float mass, string name)
        {
            _celestialBodyType = type;
            Radius = radius;
            Name = name;
            Mass = mass;
            _celestialTransform = transform;
        }

        public void SetOrbitingBody(CelestialBehaviour newOrbitingBody)
        {
            _parentOrbitingBody = newOrbitingBody;
        }

        public void RemoveOrbitingBody()
        {
            _parentOrbitingBody = null;
        }

        protected virtual void Awake()
        {
            _celestialParallaxLayer = GetComponent<CelestialParallaxLayer>();
            _orbitController = GetComponent<OrbitingController>();
            _celestialRigidBody = GetComponent<Rigidbody2D>();
            _celestialRigidBody.mass = Mass;
            _celestialTransform = transform;
            
            _celestialMaterials = new Material[celestialComponents.Length];
            
            for (int i = 0; i < celestialComponents.Length; i++)
            {
                _celestialMaterials[i] = celestialComponents[i].GetComponent<SpriteRenderer>().material;
            }
        }

        protected virtual void Start() { }

        protected virtual void Update()
        {
            if (ParentOrbitingBody is not null && ParentOrbitingBody.CelestialBodyType is CelestialBodyType.Star)
            {
                SetLight(ParentOrbitingBody.WorldPosition);
            }
        }

        protected virtual void UpdateTime(float time)
        {
            for (int i = 0; i < _celestialMaterials.Length; i++)
            {
                _celestialMaterials[i].SetFloat(ShaderProperties.Key_time, time);
            }
        }

        protected virtual void SetPixel(float amount)
        {
            for (int i = 0; i < _celestialMaterials.Length; i++)
            {
                _celestialMaterials[i].SetFloat(ShaderProperties.Key_Pixels, amount);
            }
        }

        public void SetLight(Vector2 lightPosition, float lightModifier = 1f)
        {
            Vector2 relativePos = lightPosition - (Vector2)transform.position;
            relativePos = relativePos.normalized * lightModifier;

            Vector2 viewportPos = relativePos * 0.5f + new Vector2(0.5f, 0.5f);

            for (int i = 0; i < _celestialMaterials.Length; i++)
            {
                _celestialMaterials[i].SetVector(ShaderProperties.Key_Light_origin, viewportPos);
            }
        }
    }
}