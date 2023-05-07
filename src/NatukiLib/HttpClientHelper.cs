namespace NatukiLib
{
    public sealed class HttpClientHelper : IDisposable
    {
        public HttpClientHelper(string? userAgent = null, int? timeOut = null)
        {
            UserAgent = userAgent ?? CommonUtil.DefaultHttpClientUserAgent;
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            if (timeOut is not null) HttpClient.Timeout = TimeSpan.FromMilliseconds(timeOut.Value);
        }

        /// <summary>
        /// ユーザーエージェント
        /// </summary>
        /// <remarks>//chrome://version/</remarks>
        public string UserAgent { get; init; }

        private HttpClient HttpClient { get; set; }

        public async Task<string> GetAsync(string uri, int? timeOut = null)
        {
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri)
            };

            if (timeOut is not null && timeOut.Value != HttpClient.Timeout.Milliseconds)
            {
                HttpClient.Dispose();
                HttpClient = new HttpClient();
                HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                HttpClient.Timeout = TimeSpan.FromMilliseconds(timeOut.Value);
            }
            var response = await HttpClient.SendAsync(request);
            CommonUtil.Logger.Info($"ファイルを取得しました。URL:{uri}");
            return await response.Content.ReadAsStringAsync();
        }

        public void Dispose() => HttpClient.Dispose();
    }
}
