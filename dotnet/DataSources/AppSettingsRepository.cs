﻿namespace ReviewsRatings.DataSources
{
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using ReviewsRatings.Models;
    using ReviewsRatings.Services;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public class AppSettingsRepository : IAppSettingsRepository
    {
        private const string REVIEWS_BUCKET = "productReviews";
        private const string SETTINGS = "appSettings";
        private const string HEADER_VTEX_CREDENTIAL = "X-Vtex-Credential";
        private const string APPLICATION_JSON = "application/json";
        private readonly IVtexEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _applicationName;
        private string AUTHORIZATION_HEADER_NAME;


        public AppSettingsRepository(IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory)
        {
            this._environmentVariableProvider = environmentVariableProvider ??
                                                throw new ArgumentNullException(nameof(environmentVariableProvider));

            this._httpContextAccessor = httpContextAccessor ??
                                        throw new ArgumentNullException(nameof(httpContextAccessor));

            this._clientFactory = clientFactory ??
                               throw new ArgumentNullException(nameof(clientFactory));

            this._applicationName =
                $"{this._environmentVariableProvider.ApplicationVendor}.{this._environmentVariableProvider.ApplicationName}";

            AUTHORIZATION_HEADER_NAME = "Authorization";
        }

        public async Task<AppSettings> GetAppSettingAsync()
        {
            Console.WriteLine($"GetAppSettingAsync called with {this._environmentVariableProvider.Account},{this._environmentVariableProvider.Workspace},{this._environmentVariableProvider.ApplicationName},{this._environmentVariableProvider.ApplicationVendor},{this._environmentVariableProvider.Region}");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._environmentVariableProvider.Account}/{this._environmentVariableProvider.Workspace}/buckets/{this._applicationName}/{REVIEWS_BUCKET}/files/{SETTINGS}"),
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(AUTHORIZATION_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            AppSettings appSettings = JsonConvert.DeserializeObject<AppSettings>(responseContent);
            return appSettings;
        }

        public async Task SaveAppSettingsAsync(AppSettings appSettings)
        {
            Console.WriteLine($"        >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> SaveAppSettingsAsync ");

            if (appSettings == null)
            {
                appSettings = new AppSettings();
            }

            var jsonSerializedAppSettings = JsonConvert.SerializeObject(appSettings);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._environmentVariableProvider.Account}/{this._environmentVariableProvider.Workspace}/buckets/{this._applicationName}/{REVIEWS_BUCKET}/files/{SETTINGS}"),
                Content = new StringContent(jsonSerializedAppSettings, Encoding.UTF8, APPLICATION_JSON)
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(AUTHORIZATION_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            // Console.WriteLine($"        >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> {response.IsSuccessStatusCode} {response.ReasonPhrase}");
        }
    }
}
