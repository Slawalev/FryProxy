﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using FryProxy.IO;
using log4net.Appender;
using log4net.Config;
using Moq;
using NUnit.Framework;

namespace FryProxy.Tests
{
    internal class DefaultProxyTests
    {
        private readonly HttpRequestMessage _clientRequest;

        private readonly Socket _clientSocket, _serverSocket;
        private readonly Mock<IRemoteEndpointResolver> _endpointResolverMock;
        private readonly Mock<IHttpMessageReader> _messageReaderMock;
        private readonly Mock<IHttpMessageWriter> _messageWriterMock;
        private readonly Mock<IRemoteEndpointConnector> _remoteConnectorMock;

        private readonly EndPoint _serverEndPoint;
        private readonly HttpResponseMessage _serverResponse;
        private readonly Stream _serverStream, _clientStream;
        private readonly Mock<IStreamFactory> _streamFactoryMock;

        public DefaultProxyTests()
        {
            BasicConfigurator.Configure(new ConsoleAppender());

            var mockRepository = new MockRepository(MockBehavior.Loose);

            _messageReaderMock = mockRepository.Create<IHttpMessageReader>();
            _messageWriterMock = mockRepository.Create<IHttpMessageWriter>();
            _endpointResolverMock = mockRepository.Create<IRemoteEndpointResolver>();
            _remoteConnectorMock = mockRepository.Create<IRemoteEndpointConnector>();
            _streamFactoryMock = mockRepository.Create<IStreamFactory>();

            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _serverStream = new MemoryStream();
            _clientStream = new MemoryStream();

            _clientRequest = new HttpRequestMessage();
            _serverResponse = new HttpResponseMessage();
            _serverEndPoint = new DnsEndPoint("localhost", 80);
        }

        [SetUp]
        public void SetupMocks()
        {
            _messageReaderMock.Reset();
            _messageWriterMock.Reset();
            _endpointResolverMock.Reset();
            _remoteConnectorMock.Reset();
            _streamFactoryMock.Reset();

            _messageReaderMock
                .Setup(r => r.ReadRequestMessage(_clientStream))
                .Returns(_clientRequest);

            _messageReaderMock
                .Setup(r => r.ReadResponseMessage(_serverStream))
                .Returns(_serverResponse);

            _endpointResolverMock
                .Setup(r => r.Resolve(_clientRequest))
                .Returns(_serverEndPoint);

            _remoteConnectorMock
                .Setup(c => c.Connect(_serverEndPoint))
                .Returns(_serverSocket);

            _streamFactoryMock
                .Setup(f => f.CreateStream(_clientSocket, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
                .Returns(_clientStream);

            _streamFactoryMock
                .Setup(f => f.CreateStream(_serverSocket, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
                .Returns(_serverStream);
        }

        [Test]
        public void ShouldProcessHttpRequest()
        {
            CreateProxy().AcceptSocket(_clientSocket);

            _messageWriterMock.Verify(w => w.Write(_clientRequest, _serverStream));
            _messageWriterMock.Verify(w => w.Write(_serverResponse, _clientStream));
        }

        private DefaultHttpProxy CreateProxy()
        {
            return new DefaultHttpProxy(
                _messageReaderMock.Object,
                _messageWriterMock.Object,
                _endpointResolverMock.Object,
                _remoteConnectorMock.Object,
                _streamFactoryMock.Object);
        }
    }
}