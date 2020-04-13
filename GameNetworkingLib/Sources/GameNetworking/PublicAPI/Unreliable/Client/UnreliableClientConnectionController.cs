﻿using System;
using GameNetworking.Commons.Client;
using GameNetworking.Commons.Models;
using GameNetworking.Commons.Models.Client;
using GameNetworking.Messages.Client;
using GameNetworking.Networking.Models;
using Networking.Commons.Models;
using Networking.Models;
using Networking.Sockets;

namespace GameNetworking {
    public class UnreliableClientConnectionController {
        private readonly IGameClientMessageSender client;

        private readonly Action timeOutAction = null;

        private bool isConnecting = false;
        private int retryCount = 0;
        private double startTime = -1F;

        private double elapsedTime => this.Now() - this.startTime;

        public float secondsBetweenRetries { get; set; } = 3F;
        public int maximumNumberOfRetries { get; set; } = 3;

        public UnreliableClientConnectionController(IGameClientMessageSender client, Action timeOutAction) {
            this.client = client;
            this.timeOutAction = timeOutAction;
        }

        public void Connect() {
            if (this.isConnecting) { return; }
            this.isConnecting = true;
            this.retryCount = 0;
            this.startTime = this.Now();

            this.Send();
        }

        public void ReceivedConnected() {
            this.isConnecting = false;
        }

        public void Update() {
            if (this.elapsedTime >= this.secondsBetweenRetries) {
                if (this.retryCount >= this.maximumNumberOfRetries) {
                    this.isConnecting = false;
                    this.DispatchTimeOut();
                    return;
                }

                this.retryCount++;
                this.startTime = this.Now();
                this.Send();
            }
        }

        #region Private Methods

        private void DispatchTimeOut() {
            this.timeOutAction?.Invoke();
        }

        private void Send() {
            this.client.Send(new UnreliableConnectMessage());
        }

        private double Now() {
            return TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
        }

        #endregion
    }
}