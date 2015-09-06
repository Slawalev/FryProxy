﻿using System.IO;
using System.Text;
using FryProxy.Headers;
using FryProxy.Messages;
using NUnit.Framework;
using OpenQA.Selenium;

namespace FryProxy.Tests.Integration
{
    public class InterceptionTests : AbstractIntegrationTestsFixture
    {
        protected override IWebDriver CreateDriver(Proxy proxy)
        {
            return CreateChromeDriver(proxy);
        }

        [TearDown]
        public void DetachHandlers()
        {
            HttpProxyServer.Proxy.OnProcessingComplete = null;
            HttpProxyServer.Proxy.OnRequestReceived = null;
            HttpProxyServer.Proxy.OnResponseReceived = null;
            HttpProxyServer.Proxy.OnResponseSent = null;
            HttpProxyServer.Proxy.OnServerConnected = null;
        }

        [Test]
        public void ShouldRewriteRequest()
        {
            HttpProxyServer.Proxy.OnRequestReceived = context =>
            {
                var header = context.RequestHeader;

                if (!header.Host.Contains("wikipedia.org"))
                {
                    return;
                }

                header.RequestURI = "/";
                header.Host = "example.com";
            };

            WebDriver.Navigate().GoToUrl("http://www.wikipedia.org");

            Assert.That(WebDriver.Title, Contains.Substring("Example Domain"));
        }

        [Test]
        public void ShouldReplaceResponse()
        {
            var responseBody = new StringBuilder()
                .AppendLine("<html>")
                .AppendLine("<head><title>Fry Rocks!</title></head>")
                .AppendLine("<body><h1>Fry Proxy</h1></body>")
                .AppendLine("</html>")
                .ToString();

            HttpProxyServer.Proxy.OnRequestReceived = context =>
            {
                if (!context.RequestHeader.Host.Contains("wikipedia.org"))
                {
                    return;
                }

                context.StopProcessing();

                var responseStream = new MemoryStream(Encoding.ASCII.GetBytes(responseBody), false);
                var responseHeader = new HttpResponseHeader(200, "OK", "1.1");

                responseHeader.EntityHeaders.ContentType = "text/html";
                responseHeader.EntityHeaders.ContentEncoding = "us-ascii";
                responseHeader.EntityHeaders.ContentLength = responseStream.Length;

                new HttpResponseMessage(responseHeader, responseStream).Write(context.ClientStream);
            };

            WebDriver.Navigate().GoToUrl("http://www.wikipedia.org");

            Assert.That(WebDriver.Title, Contains.Substring("Fry"));
        }
    }
}