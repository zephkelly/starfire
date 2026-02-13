using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using Starfire.Core;
using Starfire.Network;

namespace Starfire.Demo
{
    public class ConnectionUI : MonoBehaviour
    {
        const int MaxLogLines = 200;
        const ushort Port = 7979;

        public static FixedString64Bytes PlayerUsername;

        string _username = "Player";
        string _serverIP = "127.0.0.1";
        bool _connected;
        bool _serverOnly;
        string _statusMessage = "";
        Coroutine _connectCoroutine;

        readonly System.Collections.Generic.List<LogEntry> _logEntries =
            new System.Collections.Generic.List<LogEntry>(MaxLogLines);
        Vector2 _logScroll;
        bool _autoScroll = true;

        GUIStyle _titleStyle;
        GUIStyle _labelStyle;
        GUIStyle _buttonStyle;
        GUIStyle _textFieldStyle;
        GUIStyle _statusStyle;
        GUIStyle _logStyle;
        GUIStyle _logBoxStyle;
        GUIStyle _headerStyle;

        struct LogEntry
        {
            public string Message;
            public LogType Type;
        }

        void OnEnable()
        {
            Application.logMessageReceived += OnLogMessage;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_logEntries.Count >= MaxLogLines)
                _logEntries.RemoveAt(0);

            _logEntries.Add(new LogEntry { Message = message, Type = type });

            if (_autoScroll)
                _logScroll.y = float.MaxValue;
        }

        void Update()
        {
            var controller = PlayerController.Instance;
            if (_connected && controller != null && controller.EscapePressed)
                Disconnect();
        }

        void OnGUI()
        {
            EnsureStyles();

            if (!_connected)
            {
                DrawMenu();
                return;
            }

            if (_serverOnly)
            {
                DrawServerConsole();
                return;
            }

            DrawEscapeHint();
        }

        void DrawMenu()
        {
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0.02f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            float boxWidth = 320f;
            float boxHeight = 280f;
            float x = (Screen.width - boxWidth) * 0.5f;
            float y = (Screen.height - boxHeight) * 0.5f;

            GUI.Box(new Rect(x - 10f, y - 10f, boxWidth + 20f, boxHeight + 20f), GUIContent.none);

            GUI.Label(new Rect(x, y, boxWidth, 36f), "STARFIRE", _titleStyle);
            y += 44f;

            GUI.Label(new Rect(x, y, 80f, 24f), "Username:", _labelStyle);
            _username = GUI.TextField(new Rect(x + 85f, y, boxWidth - 85f, 24f), _username, 60, _textFieldStyle);
            y += 32f;

            float halfWidth = (boxWidth - 6f) * 0.5f;
            if (GUI.Button(new Rect(x, y, halfWidth, 32f), "Host + Play", _buttonStyle))
                StartHostAndPlay();
            if (GUI.Button(new Rect(x + halfWidth + 6f, y, halfWidth, 32f), "Host Only", _buttonStyle))
                StartHostOnly();
            y += 40f;

            GUI.Label(new Rect(x, y, 80f, 24f), "Server IP:", _labelStyle);
            _serverIP = GUI.TextField(new Rect(x + 85f, y, boxWidth - 85f, 24f), _serverIP, 45, _textFieldStyle);
            y += 30f;

            if (GUI.Button(new Rect(x, y, boxWidth, 32f), "Join Game", _buttonStyle))
                StartClient();
            y += 36f;

            if (!string.IsNullOrEmpty(_statusMessage))
                GUI.Label(new Rect(x, y, boxWidth, 20f), _statusMessage, _statusStyle);
        }

        void DrawServerConsole()
        {
            float margin = 20f;
            float headerHeight = 40f;
            float footerHeight = 36f;
            var headerRect = new Rect(margin, margin, Screen.width - margin * 2f, headerHeight);
            var consoleRect = new Rect(margin, margin + headerHeight + 4f,
                Screen.width - margin * 2f,
                Screen.height - margin * 2f - headerHeight - footerHeight - 8f);
            var footerRect = new Rect(margin, Screen.height - margin - footerHeight,
                Screen.width - margin * 2f, footerHeight);

            GUI.Label(headerRect, $"STARFIRE SERVER  |  Port {Port}  |  {_logEntries.Count} log entries", _headerStyle);

            GUI.Box(consoleRect, GUIContent.none, _logBoxStyle);

            float lineHeight = 16f;
            float contentHeight = _logEntries.Count * lineHeight;
            var viewRect = new Rect(0f, 0f, consoleRect.width - 20f, contentHeight);

            _logScroll = GUI.BeginScrollView(consoleRect, _logScroll, viewRect);

            for (int i = 0; i < _logEntries.Count; i++)
            {
                var entry = _logEntries[i];
                var prevColor = GUI.color;
                GUI.color = GetLogColor(entry.Type);
                GUI.Label(new Rect(4f, i * lineHeight, viewRect.width - 8f, lineHeight),
                    entry.Message, _logStyle);
                GUI.color = prevColor;
            }

            GUI.EndScrollView();

            _autoScroll = GUI.Toggle(new Rect(footerRect.x, footerRect.y, 120f, footerHeight),
                _autoScroll, "Auto-scroll", _labelStyle);

            if (GUI.Button(new Rect(footerRect.x + 130f, footerRect.y + 2f, 80f, footerHeight - 4f),
                "Clear", _buttonStyle))
                _logEntries.Clear();

            if (GUI.Button(new Rect(footerRect.xMax - 160f, footerRect.y + 2f, 160f, footerHeight - 4f),
                "ESC: Back to Menu", _buttonStyle))
                Disconnect();
        }

        void DrawEscapeHint()
        {
            GUI.Label(new Rect(10f, Screen.height - 24f, 200f, 20f), "ESC: Back to Menu", _statusStyle);
        }

        void StartHostAndPlay()
        {
            PlayerUsername = new FixedString64Bytes(_username);

            StarfireBootstrap.DestroyAllWorlds();
            var (serverWorld, clientWorld) = StarfireBootstrap.CreateHostWorlds();

            Listen(serverWorld);

            _connected = true;
            _serverOnly = false;
            _statusMessage = "Waiting for server...";
            _connectCoroutine = StartCoroutine(WaitForServerThenConnect(serverWorld, clientWorld));
        }

        IEnumerator WaitForServerThenConnect(World serverWorld, World clientWorld)
        {
            float timeout = 5f;
            float elapsed = 0f;
            bool ready = false;

            while (serverWorld.IsCreated && elapsed < timeout)
            {
                var q = serverWorld.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<Starfire.Sim.SpawnConfig>());
                ready = !q.IsEmpty;
                q.Dispose();
                if (ready) break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (ready && clientWorld != null && clientWorld.IsCreated)
            {
                Connect(clientWorld, NetworkEndpoint.LoopbackIpv4.WithPort(Port));
                _statusMessage = "";
                Debug.Log($"[ConnectionUI] Host+Play as '{_username}' on port {Port}");
            }
            else
            {
                _statusMessage = "Error: Server failed to initialize";
                _connected = false;
            }

            _connectCoroutine = null;
        }

        void StartHostOnly()
        {
            PlayerUsername = new FixedString64Bytes(_username);

            StarfireBootstrap.DestroyAllWorlds();
            var serverWorld = StarfireBootstrap.CreateDedicatedServer();

            Listen(serverWorld);

            _connected = true;
            _serverOnly = true;
            Debug.Log($"[ConnectionUI] Dedicated server on port {Port}");
        }

        void StartClient()
        {
            if (string.IsNullOrWhiteSpace(_serverIP))
            {
                _statusMessage = "Enter a server IP address";
                return;
            }

            if (!NetworkEndpoint.TryParse(_serverIP, Port, out var endpoint))
            {
                _statusMessage = $"Invalid IP: {_serverIP}";
                return;
            }

            PlayerUsername = new FixedString64Bytes(_username);

            StarfireBootstrap.DestroyAllWorlds();
            var clientWorld = StarfireBootstrap.CreateClientOnly();

            Connect(clientWorld, endpoint);

            _connected = true;
            _serverOnly = false;
            Debug.Log($"[ConnectionUI] Joining '{_serverIP}:{Port}' as '{_username}'");
        }

        void Disconnect()
        {
            Debug.Log("[ConnectionUI] Disconnecting...");

            if (_connectCoroutine != null)
            {
                StopCoroutine(_connectCoroutine);
                _connectCoroutine = null;
            }

            StarfireBootstrap.DestroyAllWorlds();

            var minimap = FindFirstObjectByType<MinimapRenderer>();
            if (minimap != null) minimap.ClearDisplay();

            _connected = false;
            _serverOnly = false;
            _statusMessage = "";
        }

        static void Listen(World serverWorld)
        {
            var entity = serverWorld.EntityManager.CreateEntity();
            serverWorld.EntityManager.AddComponentData(entity,
                new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4.WithPort(Port) });
        }

        static void Connect(World clientWorld, NetworkEndpoint endpoint)
        {
            var entity = clientWorld.EntityManager.CreateEntity();
            clientWorld.EntityManager.AddComponentData(entity,
                new NetworkStreamRequestConnect { Endpoint = endpoint });
        }

        static Color GetLogColor(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception: return new Color(1f, 0.3f, 0.3f);
                case LogType.Warning: return new Color(1f, 0.85f, 0.3f);
                case LogType.Assert: return new Color(1f, 0.5f, 0.2f);
                default: return new Color(0.8f, 0.8f, 0.8f);
            }
        }

        void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = new Color(0.2f, 0.8f, 1f, 1f);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            _labelStyle.normal.textColor = Color.white;

            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            _textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 14 };

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            _statusStyle.normal.textColor = new Color(1f, 0.4f, 0.4f, 1f);

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _headerStyle.normal.textColor = new Color(0.2f, 0.8f, 1f, 1f);

            var logBg = new Texture2D(1, 1);
            logBg.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.08f, 0.95f));
            logBg.Apply();

            _logBoxStyle = new GUIStyle(GUI.skin.box);
            _logBoxStyle.normal.background = logBg;

            _logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
        }
    }
}
