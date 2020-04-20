﻿using System.Collections.Generic;

namespace Networking {
    using Commons;
    using Commons.Models;
    using Logging;
    using Models;
    using Networking.IO;
    using Sockets;

    public interface IReliableSocketListener {
        void NetworkingDidConnect(ReliableNetClient client);
        void NetworkingConnectDidTimeout();
        void NetworkingDidDisconnect(ReliableNetClient client);
    }

    public interface IReliableSocket : INetworking<ITCPSocket, ReliableNetClient> {
        IReliableSocketListener listener { get; set; }

        ReliableNetClient Accept();

        void Connect(string host, int port);
        void Disconnect(ReliableNetClient client);
    }

    public sealed class ReliableSocket : IReliableSocket {
        private readonly ITCPSocket socket;
        private readonly Queue<ITCPSocket> acceptedQueue;
        private bool isAccepting = false;

        public int port { get; private set; }

        public IReliableSocketListener listener { get; set; }

        public ReliableSocket(ITCPSocket socket) {
            this.socket = socket;
            this.acceptedQueue = new Queue<ITCPSocket>();
        }

        public void Start(string host, int port) {
            this.port = port;
            this.socket.Bind(new GameNetworking.Sockets.NetEndPoint(host, port));
            this.socket.Listen(10);
        }

        private void AcceptNewClient() {
            if (this.isAccepting) { return; }

            this.isAccepting = true;

            this.socket.Accept((accepted) => {
                if (accepted != null) { this.acceptedQueue.Enqueue(accepted); }
                this.isAccepting = false;
            });
        }

        public ReliableNetClient Accept() {
            this.AcceptNewClient();

            if (this.acceptedQueue.Count > 0) {
                return this.CreateNetClient(this.acceptedQueue.Dequeue());
            }
            return null;
        }

        public void Stop() {
            this.socket.Close();
        }

        public void Connect(string host, int port) {
            ReliableNetClient client = this.CreateNetClient(this.socket);
            GameNetworking.Sockets.NetEndPoint ep = new GameNetworking.Sockets.NetEndPoint(host, port);
            client.Connect(ep, () => {
                if (client.isConnected) {
                    this.listener?.NetworkingDidConnect(client);
                } else {
                    this.socket.Close();
                    this.listener?.NetworkingConnectDidTimeout();
                }
            });

            Logger.Log($"Trying to connect to {host}-{port}");
        }

        public void Disconnect(ReliableNetClient client) {
            client.Disconnect(() => this.listener?.NetworkingDidDisconnect(client));
        }

        public void Read(ReliableNetClient client) {
            client.reader.Receive();
        }

        public void Send(ReliableNetClient client, byte[] message) {
            client.writer.Write(message);
        }

        public void Flush(ReliableNetClient client) {
            if (client.isConnected) {
                client.writer.Flush();
            } else {
                this.listener?.NetworkingDidDisconnect(client);
            }
        }

        private ReliableNetClient CreateNetClient(ITCPSocket socket) {
            return new ReliableNetClient(socket, new ReliableNetworkingReader(socket), new ReliableNetworkingWriter(socket));
        }
    }
}
