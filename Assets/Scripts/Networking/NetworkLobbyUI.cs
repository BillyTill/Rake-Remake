using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using kcp2k;
using Mirror;
using UnityEngine;

namespace Lake.Networking
{
    /// <summary>
    /// Минимальное лобби для M0: хост/подключение по IPv4, без никнеймов.
    /// В билде можно стартовать автоматически: -host или -join 192.168.1.10.
    /// </summary>
    public sealed class NetworkLobbyUI : MonoBehaviour
    {
        [SerializeField] private ushort port = 7777;

        [Tooltip("Через сколько секунд считать старт неуспешным, если ничего не поднялось.")]
        [SerializeField] private float startFailureTimeout = 2f;

        /// <summary>Свой адрес — только для раздачи другим: он не попадает в поле Join.</summary>
        private string _localAddress = "127.0.0.1";

        /// <summary>Адрес хоста, к которому подключаемся. Дефолт — локальный тест, не свой IP: иначе клиент без правки поля подключался сам к себе.</summary>
        private string _address = "127.0.0.1";
        private string _status = string.Empty;
        private bool _autoStartTried;
        private bool _startPending;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Резолв один раз в Start: вызывать его из OnGUI нельзя — GUI
            // рисуется минимум дважды за кадр, и на машинах с VPN или медленным
            // DNS это фрикило лобби на десятки-сотни миллисекунд.
            _localAddress = GetLocalIPv4();
            ApplyPort();
            StartCoroutine(AutoStartFromCommandLine());
        }

        private void Update()
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                // Успешный старт снимает ожидание результата.
                _startPending = false;
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
                StartHost();
            else if (Input.GetKeyDown(KeyCode.F2))
                StartClient();
        }

        private IEnumerator AutoStartFromCommandLine()
        {
            yield return null;
            if (_autoStartTried)
                yield break;

            _autoStartTried = true;
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-host")
                {
                    StartHost();
                    yield break;
                }

                if (args[i] == "-join" && i + 1 < args.Length)
                {
                    _address = args[i + 1];
                    StartClient();
                    yield break;
                }
            }
        }

        private void OnGUI()
        {
            NetworkManager manager = NetworkManager.singleton;
            if (manager == null)
            {
                _status = "NetworkManager не найден: запусти Tools/Lake/Network/Setup M0 Scene.";
                DrawStatusOnly();
                return;
            }

            if (NetworkServer.active || NetworkClient.active)
            {
                DrawConnected(manager);
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 20f, 380f, 280f), GUI.skin.box);
            GUILayout.Label("Lake — network test (M0)");
            GUILayout.Space(4f);
            GUILayout.Label($"Local IPv4: {_localAddress}");
            GUILayout.Label($"Port: {port}    Max players: {manager.maxConnections}");
            GUILayout.Space(4f);
            GUILayout.Label("Host address for clients:");
            _address = GUILayout.TextField(_address, 40);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Host (F1)", GUILayout.Height(30f)) && !_startPending)
                StartHost();
            if (!_startPending && GUILayout.Button("Join (F2)", GUILayout.Height(30f)))
                StartClient();
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_status))
                GUILayout.Label(_status);

            GUILayout.Space(4f);
            GUILayout.Label("Host: F1. Client: enter host IPv4, then F2.");
            GUILayout.EndArea();
        }

        private void DrawConnected(NetworkManager manager)
        {
            GUILayout.BeginArea(new Rect(20f, 20f, 380f, 110f), GUI.skin.box);
            string role = NetworkServer.active ? "Host" : "Client";
            GUILayout.Label($"Lake — {role}");
            GUILayout.Label($"Address: {manager.networkAddress}:{port}");

            if (NetworkServer.active)
                GUILayout.Label($"Connections: {NetworkServer.connections.Count}/{manager.maxConnections}");
            else if (NetworkClient.connection != null)
                GUILayout.Label("Connected to host");

            if (GUILayout.Button("Disconnect"))
            {
                if (NetworkServer.active)
                    manager.StopHost();
                else
                    manager.StopClient();
            }

            GUILayout.EndArea();
        }

        private void DrawStatusOnly()
        {
            GUILayout.BeginArea(new Rect(20f, 20f, 380f, 60f), GUI.skin.box);
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private void StartHost()
        {
            if (NetworkServer.active || NetworkClient.active || _startPending)
                return;

            if (NetworkManager.singleton == null)
            {
                _status = "NetworkManager не найден: запусти Tools/Lake/Network/Setup M0 Scene.";
                return;
            }

            ApplyPort();
            _startPending = true;
            _status = "Запуск хоста...";
            NetworkManager.singleton.StartHost();
            StartCoroutine(WatchStartResult("Хост не поднялся: порт, скорее всего, занят, или транспорт отключён."));
        }

        private void StartClient()
        {
            if (NetworkServer.active || NetworkClient.active || _startPending)
                return;

            if (NetworkManager.singleton == null)
            {
                _status = "NetworkManager не найден: запусти Tools/Lake/Network/Setup M0 Scene.";
                return;
            }

            string address = _address != null ? _address.Trim() : string.Empty;
            if (address.Length == 0)
            {
                _status = "Укажи адрес хоста (IPv4), потом F2.";
                return;
            }

            ApplyPort();
            NetworkManager.singleton.networkAddress = address;
            _startPending = true;
            _status = $"Подключение к {address}:{port}...";
            NetworkManager.singleton.StartClient();
            StartCoroutine(WatchStartResult("Не удалось подключиться: проверь адрес, порт и что хост запущен."));
        }

        /// <summary>
        /// Mirror не кидает отдельное событие на неудачный StartHost/StartClient:
        /// из-за этого занятый порт раньше оставлял статус «Starting...» навсегда.
        /// </summary>
        private IEnumerator WatchStartResult(string failureText)
        {
            yield return new WaitForSeconds(startFailureTimeout);

            if (NetworkServer.active || NetworkClient.active)
                yield break; // успело подняться — Update снимет _startPending

            _startPending = false;
            _status = failureText;
        }

        private void ApplyPort()
        {
            if (NetworkManager.singleton == null)
                return;

            if (NetworkManager.singleton.transport is KcpTransport kcp)
                kcp.port = port;
            else
                Debug.LogWarning("[Lake] Порт применяется только к KcpTransport: при смене транспорта задай порт в инспекторе.");
        }

        private static string GetLocalIPv4()
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                for (int i = 0; i < addresses.Length; i++)
                {
                    if (addresses[i].AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addresses[i]))
                    {
                        return addresses[i].ToString();
                    }
                }
            }
            catch
            {
                // Ниже — запасной адрес для локального теста.
            }

            return "127.0.0.1";
        }
    }
}
