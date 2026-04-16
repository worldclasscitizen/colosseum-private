using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;

namespace Colosseum.Network
{
    public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Session Settings")]
        [SerializeField] private string _sessionName = "ColosseumRoom";
        [SerializeField] private int _maxPlayers = 2;

        [Header("Player")]
        [SerializeField] private NetworkPrefabRef _playerPrefab;

        private NetworkRunner _runner;
        private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

        private void OnGUI()
        {
            if (_runner == null)
            {
                if (GUI.Button(new Rect(20, 20, 200, 40), "Start Host"))
                    StartGame(GameMode.Host);

                if (GUI.Button(new Rect(20, 70, 200, 40), "Join as Client"))
                    StartGame(GameMode.Client);
            }
            else
            {
                GUI.Label(new Rect(20, 20, 400, 40),
                    $"Mode: {_runner.GameMode} | Players: {_spawnedPlayers.Count}");
            }
        }

        private async void StartGame(GameMode mode)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(
                SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = _sessionName,
                PlayerCount = _maxPlayers,
                Scene = sceneInfo,
            });

            if (result.Ok)
                Debug.Log($"[Colosseum] {mode} started successfully!");
            else
                Debug.LogError($"[Colosseum] Failed to start: {result.ShutdownReason}");
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                Vector3 spawnPos = player == runner.LocalPlayer
                    ? new Vector3(-3f, 2f, 0f)
                    : new Vector3(3f, 2f, 0f);

                NetworkObject playerObj = runner.Spawn(
                    _playerPrefab, spawnPos, Quaternion.identity, player);

                _spawnedPlayers.Add(player, playerObj);

                // RoomManager에 플레이어 등록
                var roomManager = FindObjectOfType<Colosseum.Game.RoomManager>();
                if (roomManager != null)
                {
                    if (_spawnedPlayers.Count == 1)
                        roomManager.Player1 = player;
                    else
                        roomManager.Player2 = player;
                }

                Debug.Log($"[Colosseum] Player {player} spawned at {spawnPos}");
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedPlayers.TryGetValue(player, out NetworkObject playerObj))
            {
                runner.Despawn(playerObj);
                _spawnedPlayers.Remove(player);
                Debug.Log($"[Colosseum] Player {player} despawned");
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
