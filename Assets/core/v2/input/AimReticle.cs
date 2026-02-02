using UnityEngine;

namespace StarfireV2
{
    public class AimReticle : MonoBehaviour
    {
        [SerializeField] private float maxDistance = 15f;

        private Transform owner;
        private Vector2 stickInput;
        private Vector2 lastDirection = Vector2.up;
        private float lastDistance;
        private SpriteRenderer spriteRenderer;

        public Vector2 WorldPosition => transform.position;
        public float MaxDistance => maxDistance;

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            SetVisible(false);
        }

        public void SetOwner(Transform ownerTransform)
        {
            owner = ownerTransform;
            if (owner != null)
            {
                float rad = owner.eulerAngles.z * Mathf.Deg2Rad;
                lastDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                lastDistance = maxDistance * 0.5f;
            }
        }

        public void SetStickInput(Vector2 input)
        {
            stickInput = input;
        }

        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;

            Cursor.visible = !visible;
        }

        private void Update()
        {
            if (owner == null) return;

            if (stickInput.sqrMagnitude > 0.01f)
            {
                lastDirection = stickInput.normalized;
                lastDistance = stickInput.magnitude * maxDistance;
            }

            transform.position = (Vector2)owner.position + lastDirection * lastDistance;
        }
    }
}
