using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Starfire.IO;

namespace Starfire
{
    // Responsible for managing the state of the game, current objectives, state of the player, etc.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private GameObject currentPlayerObject;
        private GameObject playerPrefab;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            playerPrefab = Resources.Load<GameObject>("Prefabs/Entities/Player");
        }

        private void Start()
        {
            LoadPlayer();
        }

        private const float respawnTime = 3f;
        private float respawnTimer = 0f;
        private void Update()
        {
            if (currentPlayerObject == null)
            {
                respawnTimer += Time.deltaTime;

                if (respawnTimer >= respawnTime)
                {
                    LoadPlayer();
                    respawnTimer = 0f;
                }
            }
        }

        private void LoadPlayer()
        {
            if (currentPlayerObject == null)
            {
                SpawnnPlayerObject();
            }
            else
            {
                CameraController.Instance.ChangeFocus(currentPlayerObject.transform);
            }
        }

        private void SpawnnPlayerObject()
        {
            currentPlayerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

            CameraController.Instance.ChangeFocus(currentPlayerObject.transform);
            Minimap.Instance.SetNewPlayer(currentPlayerObject.transform);
        }
    }
}
