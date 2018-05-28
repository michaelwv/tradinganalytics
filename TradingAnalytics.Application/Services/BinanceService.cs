using BinanceExchange.API.Client;
using BinanceExchange.API.Models.Response;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TradingAnalytics.Application.DTO;

namespace TradingAnalytics.Application.Services
{
    public class BinanceService
    {
        public async Task<List<ExchangeInfoSymbol>> GetTradingCoins()
        {
            try
            {
                var logger = LogManager.GetLogger(typeof(BinanceService));

                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                ExchangeInfoResponse exchangeInfo = await client.GetExchangeInfo();

                if (exchangeInfo != null)
                {
                    if (exchangeInfo.Symbols.Count > 0)
                        return exchangeInfo.Symbols.FindAll(x => x.Status == "TRADING" && x.QuoteAsset == SettingsService.GetQuoteAssetToTrade());
                    else
                        throw new Exception("Unexpected error when getting trading coins.", null);
                }
                else
                    throw new Exception("Unexpected error when getting trading coins.", null);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<OrderBookResponse> GetOrderBook(string symbol)
        {
            try
            {
                var logger = LogManager.GetLogger(typeof(BinanceService));

                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                OrderBookResponse orderBook = await client.GetOrderBook(symbol, false, 10);

                if(orderBook != null)
                    return orderBook;
                else
                    throw new Exception("Unexpected error when getting order book.", null);
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        public async Task<decimal> GetCurrentPrice(string symbol)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await httpClient.GetAsync(SettingsService.GetBinanceEndPoint() + "api/v3/ticker/price?symbol=" + symbol);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonMessage = response.Content.ReadAsStringAsync();
                        var price = JsonConvert.DeserializeObject<SymbolPriceResponse>(jsonMessage.Result);
                        return price.Price;
                    }
                    else
                    {
                        throw new Exception("Unexpected error when getting price ticker.", null);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}

