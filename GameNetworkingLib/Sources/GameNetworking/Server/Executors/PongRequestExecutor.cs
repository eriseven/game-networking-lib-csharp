using GameNetworking.Channels;
using GameNetworking.Commons;
using GameNetworking.Messages.Client;
using GameNetworking.Messages.Server;
using GameNetworking.Server;

namespace GameNetworking.Executors.Server {
    internal class PongRequestExecutor<TPlayer> : BaseExecutor<IGameServer<TPlayer>, PongRequestMessage>
        where TPlayer : class, IPlayer {
        private readonly TPlayer player;

        public PongRequestExecutor(IGameServer<TPlayer> server, TPlayer player) : base(server, null) {
            this.player = player;
        }

        public override void Execute() {
            this.instance.pingController.PongReceived(this.player);

            var players = this.instance.playerCollection;
            for (int index = 0; index < players.count; index++) {
                TPlayer player = players[index];
                PingResultRequestMessage message = new PingResultRequestMessage(player.playerId, player.mostRecentPingValue);
                this.player.Send(message, Channel.unreliable);
            }
        }
    }
}