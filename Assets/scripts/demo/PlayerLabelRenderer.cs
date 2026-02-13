using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Starfire.Entity;

namespace Starfire.Demo
{
    public class PlayerLabelRenderer : MonoBehaviour
    {
        [SerializeField] float _labelOffsetY = 40f;
        [SerializeField] int _fontSize = 14;

        Camera _camera;
        GUIStyle _labelStyle;
        GUIStyle _shadowStyle;
        EntityQuery _playerQuery;
        World _cachedWorld;

        void OnGUI()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            if (_cachedWorld != world)
            {
                _playerQuery = default;
                _cachedWorld = world;
            }

            if (_playerQuery == default)
                _playerQuery = world.EntityManager.CreateEntityQuery(
                    typeof(GhostOwner), typeof(PlayerName), typeof(PlayerColor), typeof(LocalTransform));

            if (_playerQuery.IsEmpty)
                return;

            EnsureStyles();

            var em = world.EntityManager;

            int localNetId = 0;
            var networkIdQuery = world.EntityManager.CreateEntityQuery(typeof(NetworkId));
            if (!networkIdQuery.IsEmpty)
            {
                var networkIdEntities = networkIdQuery.ToEntityArray(Allocator.Temp);
                if (networkIdEntities.Length > 0)
                    localNetId = em.GetComponentData<NetworkId>(networkIdEntities[0]).Value;
                networkIdEntities.Dispose();
            }

            var entities = _playerQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var ghostOwner = em.GetComponentData<GhostOwner>(entity);

                if (ghostOwner.NetworkId == localNetId)
                    continue;

                var playerName = em.GetComponentData<PlayerName>(entity);
                var transform = em.GetComponentData<LocalTransform>(entity);
                var playerColor = em.GetComponentData<PlayerColor>(entity);

                var nameStr = playerName.Value.ToString();
                if (string.IsNullOrEmpty(nameStr))
                    nameStr = $"Pilot-{ghostOwner.NetworkId}";

                var worldPos = new Vector3(transform.Position.x, transform.Position.y, 0f);
                var screenPos = _camera.WorldToScreenPoint(worldPos);

                if (screenPos.z < 0f)
                    continue;

                var color = new Color(playerColor.Value.x, playerColor.Value.y, playerColor.Value.z, 1f);
                if (playerColor.Value.x == 0f && playerColor.Value.y == 0f && playerColor.Value.z == 0f)
                    color = Color.white;

                float guiY = Screen.height - screenPos.y - _labelOffsetY;
                var content = new GUIContent(nameStr);
                var size = _labelStyle.CalcSize(content);
                var rect = new Rect(screenPos.x - size.x * 0.5f, guiY - size.y * 0.5f, size.x, size.y);

                var shadowRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height);
                GUI.Label(shadowRect, content, _shadowStyle);

                _labelStyle.normal.textColor = color;
                GUI.Label(rect, content, _labelStyle);
            }

            entities.Dispose();
        }

        void EnsureStyles()
        {
            if (_labelStyle != null)
                return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _shadowStyle = new GUIStyle(_labelStyle);
            _shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
        }
    }
}
