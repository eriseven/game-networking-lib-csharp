﻿using Messages.Coders;
using Messages.Models;

namespace GameNetworking.Messages.Server {
    public class UnreliableConnectResponseMessage : ITypedMessage {
        int ITypedMessage.type => (int)MessageType.connect;

        void IDecodable.Decode(IDecoder decoder) { }

        void IEncodable.Encode(IEncoder encoder) { }
    }
}