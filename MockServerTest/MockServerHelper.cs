﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MockServerTest
{
    public static class MockServerHelper
    {
        private static List<(string method, string path)> Expectations = new List<(string method, string path)>();  

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private static readonly HttpClient HttpClient = new HttpClient();

        public static void Reset(params string[] urls)
        {
            urls.ToList()
                .ForEach(url => HttpClient.PutAsync(new Uri($"{url}/reset"), null));

            Expectations.Clear();
        }

        public static async Task<object> CreateExpectationAsync(
            string endpoint,
            string method,
            string path,
            IEnumerable<(string, string)> headers = null,
            IEnumerable<(string, string)> query = null,
            object requestBody = null,
            object responseBody = null,
            int remainingTimes = 0,
            int statusCode = 200)
        {
            var expectation = new
            {
                HttpRequest = CreateHttpRequest(
                    method,
                    path,
                    headers ?? Enumerable.Empty<(string, string)>(),
                    query ?? Enumerable.Empty<(string, string)>(),
                    requestBody),
                HttpResponse = CreateHttpResponse(responseBody, statusCode),
                Times = new
                {
                    RemainingTimes = remainingTimes,
                    Unlimited = remainingTimes == 0
                }
            };

            using (var content = new StringContent(JsonConvert.SerializeObject(expectation, JsonSerializerSettings)))
            {
                var response = await HttpClient.PutAsync(new Uri(new Uri(endpoint), "/expectation"), content);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new InvalidOperationException("Failed to setup expectation");
                }

                Expectations.Add((method, path));

                return expectation;
            }
        }

        private static object CreateHttpResponse(object body, int statusCode) => new
        {
            StatusCode = statusCode,
            Headers = new[]
                {
                    new { Name = "content-type", Values = new[] { "application/json" } }
                },
            Body = body != null ? new { Type = "JSON", Json = JsonConvert.SerializeObject(body) } : null
        };

        private static object CreateHttpRequest(
            string method,
            string path,
            IEnumerable<(string, string)> headers,
            IEnumerable<(string, string)> query,
            object body) => new
            {
                Method = method,
                Path = path,
                Headers = headers.Select(t => new { Name = t.Item1, Values = new[] { t.Item2 } }),
                QueryStringParameters = query.Select(t => new { Name = t.Item1, Values = new[] { t.Item2 } }),
                KeepAlive = true,
                Secure = false,
                Body = body != null ? new { Type = "JSON", Json = JsonConvert.SerializeObject(body) } : null
            };

        public static async Task<bool> VerifyAsync(string endpoint, string method, string path)
        {
            var verify = new
            {
                HttpRequest = new { path, method },
                Times = new { atLeast = 1 }
            };

            using (var content = new StringContent(JsonConvert.SerializeObject(verify, JsonSerializerSettings), Encoding.UTF8))
            {
                var response = await HttpClient.PutAsync(new Uri($"{endpoint}/mockserver/verify"), content);
                return response.IsSuccessStatusCode;
            }
        }

        public static async Task<bool> VerifyAllExpectations(string mockUrl)
        {
            foreach (var expectation in Expectations)
            {
                if (! await VerifyAsync(mockUrl, expectation.method, expectation.path))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
