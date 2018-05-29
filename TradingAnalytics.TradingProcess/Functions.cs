using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Extensions;
using BinanceExchange.API.Models.Response;
using Microsoft.Azure.WebJobs;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.Application.Services;

namespace TradingAnalytics.TradingProcess
{
    public class Functions
    {
        public async System.Threading.Tasks.Task ProcessMethodAsync([TimerTrigger("00:01:00", RunOnStartup = true)] TimerInfo timerInfo, TextWriter log)
        {
            BinanceService binanceService = new BinanceService();
            TradingServices tradingServices = new TradingServices();
            ChartServices chartServices = new ChartServices();

            SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

            List<ExchangeInfoSymbol> tradingCoins = await binanceService.GetTradingCoins();

            decimal quoteAssetPriceInDollars = Math.Round(await binanceService.GetCurrentPrice(SettingsService.GetQuoteAssetToTrade() + "USDT"), 2);

            foreach (ExchangeInfoSymbol coin in tradingCoins)
            {
                Console.WriteLine("SYMBOL: " + coin.Symbol);

                OrderBookResponse orderBook = await binanceService.GetOrderBook(coin.Symbol);

                decimal baseAssetPriceInDollars = Math.Round(await binanceService.GetCurrentPrice(coin.Symbol) * quoteAssetPriceInDollars, 2);

                int baseAssetPrecision = tradingServices.GetAssetPrecision(orderBook, coin.BaseAssetPrecision);

                TradeOpportunityDTO tradeOpportunity = tradingServices.GetTradeOpportunity(orderBook, baseAssetPriceInDollars, quoteAssetPriceInDollars, baseAssetPrecision, coin.BaseAsset, coin.QuoteAsset);

                if (tradeOpportunity != null)
                {
                    bool orderSet = binanceService.SetNewOrder(tradeOpportunity, OrderSide.Buy, OrderType.Limit);

                    if (orderSet)
                    {
                        FileParameter chartImage = chartServices.GenerateOrderBookChartImage(orderBook, baseAssetPriceInDollars, quoteAssetPriceInDollars, baseAssetPrecision, tradeOpportunity);

                        TelegramService telegramService = new TelegramService();
                        HttpResponseMessage response = await telegramService.SendImageAsync(chartImage);

                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                            throw new Exception(response.ReasonPhrase);
                    }
                }
            }
        }
    }
}
