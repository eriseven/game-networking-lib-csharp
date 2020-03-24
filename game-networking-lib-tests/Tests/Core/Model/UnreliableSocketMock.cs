﻿using System;
using System.Collections.Generic;
using Networking.Commons.Models;
using Networking.Sockets;

namespace Test.Core.Model {
    class IdentifiableBytes {
        public readonly int fromSocketId;
        public readonly NetEndPoint fromEndPoint;
        public readonly NetEndPoint toEndPoint;
        public readonly List<byte> bytes;

        public IdentifiableBytes(int id, NetEndPoint from, NetEndPoint to, List<byte> bytes) {
            this.fromSocketId = id;
            this.fromEndPoint = from;
            this.toEndPoint = to;
            this.bytes = bytes;
        }
    }

    class UnreliableSocketMock : IUDPSocket, IEquatable<UnreliableSocketMock> {
        private static int sharedSocketCounter = 0;
        private readonly static List<IdentifiableBytes> writtenBytes = new List<IdentifiableBytes>();

        private readonly int socketId;

        private readonly Dictionary<NetEndPoint, UnreliableSocketMock> socketMapping = new Dictionary<NetEndPoint, UnreliableSocketMock>();

        internal NetEndPoint selfEndPoint { get; private set; }
        internal NetEndPoint talkingToEndPoint { get; private set; }

        public bool isCommunicable { get; private set; }
        public bool isBound { get; private set; }

        public UnreliableSocketMock() {
            this.socketId = sharedSocketCounter;
            sharedSocketCounter++;
        }

        public UnreliableSocketMock(int id) {
            this.socketId = id;
        }

        public void Bind(NetEndPoint endPoint) {
            this.selfEndPoint = endPoint;
            this.isBound = true;
            this.isCommunicable = true;
        }

        public void BindToRemote(NetEndPoint endPoint) {
            this.talkingToEndPoint = endPoint;
            this.isCommunicable = true;
        }

        public void Close() {
            this.isBound = false;
            this.isCommunicable = false;
        }

        public void Read(Action<byte[], IUDPSocket> callback) {
            var identifiable = writtenBytes.Find(id => id.toEndPoint == this.selfEndPoint);
            if (identifiable == null) {
                callback?.Invoke(null, null);
                return; 
            }
            var bytes = identifiable.bytes.ToArray();
            if (this.socketMapping.TryGetValue(identifiable.toEndPoint, out UnreliableSocketMock value)) {
                callback?.Invoke(bytes, value);
            } else {
                var newSocket = new UnreliableSocketMock(identifiable.fromSocketId) { selfEndPoint = this.selfEndPoint };
                newSocket.BindToRemote(identifiable.fromEndPoint);
                this.socketMapping.Add(identifiable.toEndPoint, newSocket);
                callback?.Invoke(bytes, newSocket);
            }
            identifiable.bytes.Clear();
        }

        public void Write(byte[] bytes, Action<int> callback) {
            var identifiable = writtenBytes.Find(id => id.toEndPoint == this.talkingToEndPoint);
            if (identifiable == null) {
                identifiable = new IdentifiableBytes(this.socketId, this.selfEndPoint, this.talkingToEndPoint, new List<byte>());
                writtenBytes.Add(identifiable);
            }
            identifiable.bytes.AddRange(bytes);
            callback?.Invoke(bytes.Length);
        }

        bool IEquatable<UnreliableSocketMock>.Equals(UnreliableSocketMock other) {
            return this.socketId == other.socketId;
        }
    }
}