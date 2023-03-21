using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ChatGPTSample
{
    public partial class MainPage : ContentPage
    {
        private const string ApiKey = "your_OpneAI_api_key";
        private const string TranslatorApiKey = "your_deepL_api_key";

        public MainPage()
        {
            InitializeComponent();
        }

        private async Task<string> MakeRequest(string requestBody)
        {
            // APIにリクエストを送信してレスポンスを取得
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.openai.com/v1/engines/davinci-codex/completions"),
                Headers =
                {
                    { "Accept", "application/json" },
                },
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json"),
            };

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"リクエストが失敗しました。ステータスコード：{response.StatusCode}。エラーメッセージ：{responseBody}");
            }

            return responseBody;
        }

        private async Task<string> TranslateResponse(string responseText)
        {
            try
            {
                var client = new HttpClient();
                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("auth_key", TranslatorApiKey),
                    new KeyValuePair<string, string>("text", responseText),
                    new KeyValuePair<string, string>("source_lang", "en"),
                    new KeyValuePair<string, string>("target_lang", "ja"),
                });
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://api-free.deepl.com/v2/translate"),
                    Content = requestContent,
                };

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = JObject.Parse(responseBody);
                    var translatedText = responseJson["translations"][0]["text"].ToString();
                    return translatedText;
                }
                else
                {
                    throw new Exception($"リクエストが失敗しました。ステータスコード：{response.StatusCode}。エラーメッセージ：{responseBody}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("日本語に翻訳できませんでした。", ex);
            }
        }

        private async void OnSendRequestClicked(object sender, EventArgs e)
        {
            try
            {
                // リクエスト用のJSONデータを作成
                JObject requestData = new JObject(
                    new JProperty("prompt", userInputEntry.Text),
                    new JProperty("max_tokens", 2048),
                    new JProperty("n", 1),
                    new JProperty("stop", "\n")
                );
                string requestBody = requestData.ToString();

                // APIにリクエストを送信してレスポンスを取得
                string response = await MakeRequest(requestBody);
                JObject json = JObject.Parse(response);

                // レスポンスを翻訳して回答のみを表示
                string translatedResponse = await TranslateResponse(json["choices"][0]["text"].ToString());
                outputText.Text = translatedResponse;
            }
            catch (Exception ex)
            {
                outputText.Text = ex.Message;
            }
        }
    }
}