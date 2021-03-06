﻿using System.Collections.Concurrent;
using GameNetworking.Channels;
using GameNetworking.Messages.Server;
using GameNetworking.Networking;
using Logging;

namespace GameNetworking.Server {
    public interface IGameServerClientAcceptorListener<TPlayer> : IGameServer<TPlayer>
        where TPlayer : Player {
        void ClientAcceptorPlayerDidConnect(TPlayer player);
        void ClientAcceptorPlayerDidDisconnect(TPlayer player);
    }

    public sealed class GameServerClientAcceptor<TPlayer> where TPlayer : Player, new() {
        private int playerIdCounter = 0;

        private readonly ConcurrentDictionary<ReliableChannel, TPlayer> channelCollection;

        public IGameServerClientAcceptorListener<TPlayer> listener { get; set; }

        public GameServerClientAcceptor() => this.channelCollection = new ConcurrentDictionary<ReliableChannel, TPlayer>();

        public void AcceptClient(TPlayer player) {
            this.listener.ClientAcceptorPlayerDidConnect(player);

            var players = this.listener.playerCollection.values;
            for (int i = 0; i < players.Count; i++) {
                TPlayer each = players[i];

                // Sends the connected player message to all players
                var connectedAll = new ConnectedPlayerMessage {
                    playerId = player.playerId,
                    isMe = player.Equals(each)
                };
                each.Send(connectedAll, Channel.reliable);

                if (each.Equals(player)) { continue; }

                // Sends the existing players to the player that just connected
                var connectedSelf = new ConnectedPlayerMessage {
                    playerId = each.playerId,
                    isMe = false
                };
                player.Send(connectedSelf, Channel.reliable);
            }

            if (Logger.IsLoggingEnabled) { Logger.Log($"(AcceptClient) count {this.listener.playerCollection.count}"); }
        }

        public void Disconnect(TPlayer player) {
            if (Logger.IsLoggingEnabled) { Logger.Log($"(Disconnect) count {this.listener.playerCollection.count}"); }

            if (player != null) {
                this.listener.ClientAcceptorPlayerDidDisconnect(player);
                this.listener.SendBroadcast(new DisconnectedPlayerMessage { playerId = player.playerId }, Channel.reliable);
            }
        }

        public void NetworkServerDidAcceptPlayer(ReliableChannel reliable, UnreliableChannel unreliable) {
            var player = new TPlayer();
            player.Configure(this.playerIdCounter++);
            player.Configure(reliable, unreliable);

            this.channelCollection[reliable] = player;

            this.AcceptClient(player);
        }

        public void NetworkServerPlayerDidDisconnect(ReliableChannel channel) {
            if (this.channelCollection.TryRemove(channel, out TPlayer player)) {
                this.Disconnect(player);
            }
        }
    }
}