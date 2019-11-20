using System;
using System.Collections.Generic;
using Commons;

namespace GameNetworking.Models {
    using Server;

    public interface INetworkPlayerStorageChangeDelegate {
        void PlayerStorageDidAdd(NetworkPlayer player);
        void PlayerStorageDidRemove(NetworkPlayer player);
    }

    public class NetworkPlayersStorage: WeakDelegates<INetworkPlayerStorageChangeDelegate> {
        private List<NetworkPlayer> players;

        public List<NetworkPlayer> Players {
            get { return this.players; }
        }

        public NetworkPlayersStorage() {
            this.players = new List<NetworkPlayer>();
        }

        public void Add(NetworkPlayer player) {
            this.players.Add(player);
            this.ForEach((INetworkPlayerStorageChangeDelegate del) => {
                del.PlayerStorageDidAdd(player);
            });
        }

        public void Remove(NetworkPlayer player) {
            this.players.Remove(player);
            this.ForEach((INetworkPlayerStorageChangeDelegate del) => {
                del.PlayerStorageDidRemove(player);
            });
        }

        public void ForEach(Action<NetworkPlayer> action) {
            this.players.ForEach(action);
        }

        public void ForEachConverted<TOutput>(Converter<NetworkPlayer, TOutput> converter, Action<TOutput> action) {
            this.players.ForEach(player => {
                action.Invoke(converter.Invoke(player));
            });
        }

        public List<TOutput> ConvertAll<TOutput>(Converter<NetworkPlayer, TOutput> converter) {
            return this.players.ConvertAll(converter);
        }

        public NetworkPlayer Find(Predicate<NetworkPlayer> predicate) {
            return this.players.Find(predicate);
        }

        public List<NetworkPlayer> FindAll(Predicate<NetworkPlayer> predicate) {
            return this.players.FindAll(predicate);
        }

        public List<TOutput> ConvertFindingAll<TOutput>(Predicate<NetworkPlayer> predicate, Converter<NetworkPlayer, TOutput> converter) {
            List<TOutput> output = new List<TOutput>();
            this.players.ForEach(player => { if (predicate.Invoke(player)) { output.Add(converter.Invoke(player)); } });
            return output;
        }
    }
}