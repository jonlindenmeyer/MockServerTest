using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MockServerTest
{
    [TestClass]
    public class ExampleTest
    {
        private const string MOCKURL = "http://localhost:1080/";
        private HttpClient httpClient;

        [TestInitialize]
        public void TestInitialize()
        {
            HttpHelper.Reset(MOCKURL);
            httpClient = new HttpClient();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            HttpHelper.Reset(MOCKURL);
        }

        [TestMethod]
        public async Task SendExpectationAndVerify_Success()
        {
            // Arrange
            var dummy = new { Id = Guid.NewGuid(), Name = "Dummy Mock Test" };

            var path = "/mocktest";

            var method = "GET";

            var expectation = await HttpHelper.CreateExpectationAsync(
                MOCKURL,
                method,
                path,
                responseBody: dummy,
                statusCode: 200);

            // Act
            // aqui deveria ter a chamada do client da sua aplicacao, ou envio de evento, ou algo que dispare o uso do mockserver
            // vamos simular com um simples get
            await httpClient.GetAsync($"{MOCKURL}{path}");

            // Assert
            Assert.IsNotNull(expectation, "Expectation null");

            var verify = await HttpHelper.VerifyAsync(MOCKURL, method, path);

            Assert.IsTrue(verify, "Mock verify failure");
        }
    }
}
