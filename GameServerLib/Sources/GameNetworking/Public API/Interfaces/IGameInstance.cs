using UnityEngine;

namespace GameNetworking {
    public interface IGameInstanceDelegate {
        void GameInstanceMovePlayer(Models.Server.NetworkPlayer player, Vector3 direction, Vector3 position);
    }

    public interface IGameInstance {
        IGameInstanceDelegate InstanceDelegate { get; set; }
    }
}