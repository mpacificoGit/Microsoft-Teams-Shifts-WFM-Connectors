// <copyright file="ApiHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Service
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Newtonsoft.Json.Linq;
    using RestSharp;

    /// <summary>
    /// API helper Class.
    /// </summary>
    public sealed class ApiHelper : IApiHelper
    {
        /// <summary>
        /// Send Soap Post request.
        /// </summary>
        /// <param name="endpointUrl">End point URL.</param>
        /// <param name="soapEnvOpen">Soap ENv open.</param>
        /// <param name="reqXml">Request XML.</param>
        /// <param name="soapEnvClose">Soap Env Close.</param>
        /// <param name="jSession">Session Id.</param>
        /// <returns>Soap request response.</returns>
        public async Task<Tuple<string, string>> SendSoapPostRequestAsync(
            Uri endpointUrl,
            string soapEnvOpen,
            string reqXml,
            string soapEnvClose,
            string jSession)
        {
            string soapString = $"{soapEnvOpen}{reqXml}{soapEnvClose}";

            HttpResponseMessage response = await this.PostXmlRequestAsync(endpointUrl, soapString, jSession).ConfigureAwait(false);
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(jSession))
            {
                jSession = response.Headers.Where(x => x.Key == "Set-Cookie")
                    .FirstOrDefault().Value
                    .FirstOrDefault()
                    .ToString(CultureInfo.InvariantCulture);
            }

            return new Tuple<string, string>(content, jSession);
        }

        /// <summary>
        /// Post XMl request.
        /// </summary>
        /// <param name="baseUrl">Base URL.</param>
        /// <param name="xmlString">XML string.</param>
        /// <param name="jSession">Session Id.</param>
        /// <returns>Response message.</returns>
        private async Task<HttpResponseMessage> PostXmlRequestAsync(Uri baseUrl, string xmlString, string jSession)
        {
            string authToken = this.GetAuthToken();

            if (string.IsNullOrEmpty(jSession))
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                    using (var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml"))
                    {
                        httpContent.Headers.Add("SOAPAction", ApiConstants.SoapAction);
                        return await httpClient.PostAsync(baseUrl, httpContent).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                using (var httpClientHandler = new HttpClientHandler { UseCookies = false })
                {
                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                        using (var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml"))
                        {
                            httpContent.Headers.Add("SOAPAction", ApiConstants.SoapAction);
                            httpContent.Headers.Add("Cookie", jSession);
                            return await httpClient.PostAsync(baseUrl, httpContent).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private string GetAuthToken()
        {
            string authToken = "XXXXX";

            var client = new RestClient("https://dev.api.tjx.com/gies/v1/oauth2/accesstoken?grant_type=client_credentials");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic Uko4OWR4dXVHODdKT3dBV3JyaGtQR1hKQVVQcmp0Sjk6aFI1ZlJjWkxjZUo2aWw2UQ==");
            request.AddParameter("text/plain", string.Empty, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            string source = response.Content;
            dynamic data = JObject.Parse(source);

            authToken = data.access_token;
            return authToken;
        }
    }
}