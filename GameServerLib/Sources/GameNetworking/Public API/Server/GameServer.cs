﻿using System.Collections.Generic;
using Messages.Coders;
using System;

namespace GameNetworking {
    using Networking;
    using Models;
    using Models.Server;
    using Messages;

    public class GameServer: IGameInstance {
        private readonly NetworkPlayersStorage playersStorage;

        private readonly GameServerClientAcceptor clientAcceptor;
        private readonly GameServerMessageRouter router;

        private WeakReference weakDelegate;
        private WeakReference weakInstanceDelegate;

        internal readonly NetworkingServer networkingServer;

        public readonly GameServerMovementController movementController;

        IMovementController IGameInstance.MovementController { get { return this.movementController; } }

        public IGameServerDelegate Delegate {
            get { return this.weakDelegate?.Target as IGameServerDelegate; }
            set { this.weakDelegate = new WeakReference(value); }
        }

        public IGameInstanceDelegate InstanceDelegate {
            get { return this.weakInstanceDelegate?.Target as IGameInstanceDelegate; }
            set { this.weakInstanceDelegate = new WeakReference(value); }
        }

        public GameServer() {
            this.playersStorage = new NetworkPlayersStorage();

            this.networkingServer = new NetworkingServer();

            this.clientAcceptor = new GameServerClientAcceptor(this);
            this.router = new GameServerMessageRouter(this);

            this.movementController = new GameServerMovementController(this, this.playersStorage);
        }

        public void Listen(int port) {
            this.networkingServer.Listen(port);
        }

        public void StartGame() {
            this.playersStorage.ForEach((each) => {
                this.networkingServer.Send(new StartGameMessage(), each.Client);
            });
        }

        public void Update() {
            this.movementController.Update();
            
            this.networkingServer.AcceptClient();
            this.playersStorage.ForEach((each) => { 
                this.networkingServer.Read(each.Client); 
                this.networkingServer.Flush(each.Client);    
            });
        }

        internal void AddPlayer(NetworkPlayer player) {
            this.playersStorage.Add(player);
        }

        internal void RemovePlayer(NetworkPlayer player) {
            this.playersStorage.Remove(player);
        }

        internal NetworkPlayer FindPlayer(NetworkClient client) {
            return this.playersStorage.Find(player => player.Client == client);
        }

        internal NetworkPlayer FindPlayer(int playerId) {
            return this.playersStorage.Find(player => player.PlayerId == playerId);
        }

        internal List<NetworkPlayer> AllPlayers() {
            return this.playersStorage.Players;
        }

        internal void SendBroadcast(IEncodable message) {
            this.networkingServer.SendBroadcast(message, this.playersStorage.ConvertAll(c => c.Client));
        }

        internal void SendBroadcast(IEncodable message, NetworkClient excludeClient) {
            List<NetworkClient> clientList = this.playersStorage.ConvertFindingAll(
                player => player.Client != excludeClient, 
                player => player.Client
            );
            this.networkingServer.SendBroadcast(message, clientList);
        }

        internal void Send(IEncodable message, NetworkClient client) {
            this.networkingServer.Send(message, client);
        }
    }
}
