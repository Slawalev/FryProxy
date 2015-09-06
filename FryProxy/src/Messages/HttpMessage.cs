﻿using System;
using System.Diagnostics.Contracts;
using System.IO;
using FryProxy.Headers;
using FryProxy.IO;

namespace FryProxy.Messages
{
    public abstract class HttpMessage : IHttpMessageHeader
    {
        private HttpMessageHeader _messageHeader;

        protected HttpMessage()
        {
        }

        protected HttpMessage(HttpMessageHeader messageHeader)
        {
            Contract.Requires<ArgumentNullException>(messageHeader != null, "messageHeader");

            _messageHeader = messageHeader;
        }

        protected HttpMessage(HttpMessageHeader messageHeader, Stream body)
        {
            Contract.Requires<ArgumentNullException>(messageHeader != null, "messageHeader");

            _messageHeader = messageHeader;

            Body = body;
        }

        public HttpMessageHeader MessageHeader
        {
            get { return _messageHeader; }
        }

        public Stream Body { get; set; }

        public bool Chunked
        {
            get { return _messageHeader.Chunked; }
        }

        public string StartLine
        {
            get { return _messageHeader.StartLine; }
        }

        public HttpHeaders Headers
        {
            get { return _messageHeader.Headers; }
        }

        public GeneralHeaders GeneralHeaders
        {
            get { return _messageHeader.GeneralHeaders; }
        }

        public EntityHeaders EntityHeaders
        {
            get { return _messageHeader.EntityHeaders; }
        }

        public void Read(Stream stream)
        {
            Contract.Requires<ArgumentNullException>(stream != null, "stream");
            Contract.Requires<ArgumentException>(stream.CanRead, "stream");

            _messageHeader = ReadHeader(stream);

            Body = stream;
        }

        protected virtual HttpMessageHeader ReadHeader(Stream stream)
        {
            Contract.Requires<ArgumentNullException>(stream != null, "stream");
            Contract.Requires<ArgumentException>(stream.CanRead, "stream");

            return new HttpHeaderReader(new NonBufferedStreamReader(stream)).ReadHttpMessageHeader();
        }

        public void Write(Stream stream)
        {
            Contract.Requires<ArgumentNullException>(stream != null, "stream");
            Contract.Requires<ArgumentException>(stream.CanWrite, "stream");

            var writer = new HttpContentWriter(stream);

            writer.WriteHttpMessageHeader(MessageHeader.StartLine, MessageHeader.Headers.Lines);

            if (Body != null)
            {
                WriteBody(writer);
            }
        }

        protected virtual void WriteBody(HttpContentWriter writer)
        {
            if (MessageHeader.Chunked)
            {
                writer.WriteChunckedHttpMessageBody(Body);
            }
            else if (MessageHeader.EntityHeaders.ContentLength.HasValue)
            {
                writer.WritePlainHttpMessageBody(Body, MessageHeader.EntityHeaders.ContentLength.Value);
            }
        }
    }
}