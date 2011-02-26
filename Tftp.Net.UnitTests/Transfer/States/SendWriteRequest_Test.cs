﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Tftp.Net.Transfer.States;
using System.IO;

namespace Tftp.Net.UnitTests
{
    [TestFixture]
    class SendWriteRequest_Test
    {
        private TransferStub transfer;

        [SetUp]
        public void Setup()
        {
            transfer = new TransferStub(new MemoryStream(new byte[5000]));
            transfer.SetState(new SendWriteRequest(transfer));
        }

        [Test]
        public void CanCancel()
        {
            transfer.Cancel();
            Assert.IsInstanceOf<Closed>(transfer.State);
            Assert.IsTrue(transfer.CommandWasSent(typeof(Error)));
        }

        [Test]
        public void SendsWriteRequest()
        {
            TransferStub transfer = new TransferStub(new MemoryStream(new byte[5000]));
            transfer.SetState(new SendWriteRequest(transfer));
            Assert.IsTrue(transfer.CommandWasSent(typeof(WriteRequest)));
        }

        [Test]
        public void HandlesAcknowledgement()
        {
            transfer.OnCommand(new Acknowledgement(0));
            Assert.IsInstanceOf<Sending>(transfer.State);
        }

        [Test]
        public void IgnoresWrongAcknowledgement()
        {
            transfer.OnCommand(new Acknowledgement(5));
            Assert.IsInstanceOf<SendWriteRequest>(transfer.State);
        }

        [Test]
        public void HandlesOptionAcknowledgement()
        {
            transfer.Options.Add("blub", "bla");
            Assert.IsFalse(transfer.Options.First().IsAcknowledged);
            transfer.OnCommand(new OptionAcknowledgement(transfer.Options));
            Assert.IsTrue(transfer.Options.First().IsAcknowledged);
            Assert.IsInstanceOf<Sending>(transfer.State);
        }

        [Test]
        public void HandlesMissingOptionAcknowledgement()
        {
            transfer.Options.Add("blub", "bla");
            Assert.IsFalse(transfer.Options.First().IsAcknowledged);
            transfer.OnCommand(new Acknowledgement(0));
            Assert.AreEqual(0, transfer.Options.Count());
            Assert.IsInstanceOf<Sending>(transfer.State);
        }

        [Test]
        public void HandlesError()
        {
            bool onErrorWasCalled = false;
            transfer.OnError += delegate(ITftpTransfer t, ushort code, string error) { onErrorWasCalled = true; };

            Assert.IsFalse(onErrorWasCalled);
            transfer.OnCommand(new Error(123, "Test Error"));
            Assert.IsTrue(onErrorWasCalled);

            Assert.IsInstanceOf<Closed>(transfer.State);
        }

        [Test]
        public void ResendsRequest()
        {
            TransferStub transferWithLowTimeout = new TransferStub(new MemoryStream());
            transferWithLowTimeout.Timeout = new TimeSpan(0);
            transferWithLowTimeout.SetState(new SendWriteRequest(transferWithLowTimeout));

            Assert.IsTrue(transferWithLowTimeout.CommandWasSent(typeof(WriteRequest)));
            transferWithLowTimeout.SentCommands.Clear();

            transferWithLowTimeout.OnTimer();
            Assert.IsTrue(transferWithLowTimeout.CommandWasSent(typeof(WriteRequest)));
        }
    }
}
