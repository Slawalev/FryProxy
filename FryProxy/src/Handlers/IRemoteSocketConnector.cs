﻿using System;
using System.IO;
using System.Net.Sockets;
using FryProxy.Headers;

namespace FryProxy.Handlers
{
    internal interface IRemoteSocketConnector
    {
        Tuple<Socket, Stream> EstablishConnection(HttpRequestHeader requestHeader);
    }
}