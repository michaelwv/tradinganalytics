using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TradingAnalytics.Application.DTO;

namespace TradingAnalytics.Application.Services
{
    public class TelegramService
    {
        private static string telegramToken = Properties.Settings.Default.TelegramToken;
        private static string telegramEndPoint = Properties.Settings.Default.TelegramEndPoint.Replace("<token>", telegramToken);
        private static int telegramChatId = Properties.Settings.Default.TelegramChatId;
        private static readonly Encoding encoding = Encoding.UTF8;

        public async Task<HttpResponseMessage> SendMessageAsync(string messageText)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                SendMessageRequestDTO messageRequest = new SendMessageRequestDTO
                {
                    ChatId = telegramChatId,
                    Text = messageText,
                    DisableNotification = false,
                    DisableWebPagePreview = false,
                    ParseMode = "HTML"
                };

                string postBody = JsonConvert.SerializeObject(messageRequest);

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = await httpClient.PostAsync(telegramEndPoint + "sendMessage", new StringContent(postBody, Encoding.UTF8, "application/json"));

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<HttpResponseMessage> SendImageAsync(FileParameter file)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var multipartContent = new MultipartFormDataContent())
                    {
                        multipartContent.Add(new StringContent(telegramChatId.ToString()), "\"chat_id\"");
                        multipartContent.Add(new ByteArrayContent(file.File), "\"photo\"", "\"" + file.FileName + "\"");

                        HttpResponseMessage response = await httpClient.PostAsync(telegramEndPoint + "sendPhoto", multipartContent);

                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
