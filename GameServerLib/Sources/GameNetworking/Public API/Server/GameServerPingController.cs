using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameNetworking {
    using Models;
    using Models.Server;
    using Messages.Server;
    using Commons;

    public class GameServerPingController: BaseWorker<GameServer>, INetworkPlayerStorageChangeDelegate {
        private readonly List<PingPlayer> pingPlayers = new List<PingPlayer>();

        public GameServerPingController(GameServer instance, NetworkPlayersStorage storage) : base(instance) {
            storage.Add(this);
        }

        public float GetPingValue(NetworkPlayer player) {
            return this.pingPlayers.Find(each => each.Player == player).PingValue;
        }

        internal void Update() {
            this.pingPlayers.ForEach(ping => {
                if (!ping.PingSent && ping.CanSendNextPing) {
                    ping.SendingPing();
                    this.Instance.Send(new PingRequestMessage(), ping.Player.Client);
                }
            });
        }

        public void PongReceived(NetworkPlayer player) {
            var ping = this.pingPlayers.Find(each => each.Player == player);
            ping?.ReceivedPong();
        }

        void INetworkPlayerStorageChangeDelegate.PlayerStorageDidAdd(NetworkPlayer player) {
            this.pingPlayers.Add(new PingPlayer(player));
        }

        void INetworkPlayerStorageChangeDelegate.PlayerStorageDidRemove(NetworkPlayer player) {
            var index = this.pingPlayers.FindIndex(0, this.pingPlayers.Count, each => each.Player == player);
            if (index >= 0) { this.pingPlayers.RemoveAt(index); }
        }
    }

    internal class PingPlayer: WeakReference {
        private float pingSentTime;

        private float PingElapsedTime {  get { return Time.time - this.pingSentTime; } }

        internal bool PingSent { get; private set; }
        internal bool CanSendNextPing { get { return this.PingElapsedTime > 0.5F; } }
        internal float PingValue { get; private set; }

        internal NetworkPlayer Player { get { return this.Target as NetworkPlayer; } }

        internal PingPlayer(NetworkPlayer instance) : base(instance) { }

        internal void SendingPing() {
            this.PingSent = true;
            this.pingSentTime = Time.time;
        }

        internal void ReceivedPong() {
            this.PingSent = false;
            this.PingValue = this.PingElapsedTime;
        }

        public override bool Equals(object obj) {
            if (obj is NetworkPlayer) {
                return this.Player == (NetworkPlayer)obj;
            }
            return Equals(this, obj);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}