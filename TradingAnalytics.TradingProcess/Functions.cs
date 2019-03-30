using System;
using log4net;
using System.Collections.Generic;
using System.Net.Http;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.Application.Services;
using Newtonsoft.Json;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Enums;

namespace TradingAnalytics.TradingProcess
{
    public class Functions
    {
        public void ProcessTrades()
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                logger.Debug("----------------Process Started-------------------");
                Console.WriteLine("----------------Process Started-------------------");

                BinanceService binanceService = new BinanceService();
                TradingServices tradingServices = new TradingServices();

                List<ExchangeInfoSymbol> tradingCoins = binanceService.GetTradingCoins().Result;

                List<TradeOpportunityDTO> tradeOpportunitiesFirstCheck = new List<TradeOpportunityDTO>();
                List<TradeOpportunityDTO> tradeOpportunitiesSecondCheck = new List<TradeOpportunityDTO>();
                List<TradeOpportunityDTO> tradeOpportunitiesThirdCheck = new List<TradeOpportunityDTO>();
                List<TradeOpportunityDTO> tradeOpportunitiesFourthCheck = new List<TradeOpportunityDTO>();

                decimal quoteAssetPriceInDollars = Math.Round(binanceService.GetCurrentPrice(SettingsService.GetQuoteAssetToTrade() + "USDT").Result, 2);
                decimal coinVolumeToConsider = SettingsService.GetCoinVolumeToConsider();
                decimal minQty = 0;
                decimal maxQty = 0;
                decimal stepSize = 0;
                decimal minNotional = 0;
                decimal highestPrice = 0;

                foreach (ExchangeInfoSymbol coin in tradingCoins)
                {
                    var volume = GetCoinDailyVolume(coin.BaseAsset + coin.QuoteAsset, out highestPrice);

                    if (volume < coinVolumeToConsider)
                        continue;

                    var stochastic = tradingServices.GetStochasticRsi(coin.BaseAsset + coin.QuoteAsset);

                    if (stochastic > 20 || stochastic < 0)
                        continue;

                    var filter = coin.Filters.Find(x => x.FilterType.ToString() == "LotSize");

                    if (filter != null)
                    {
                        ExchangeInfoSymbolFilterLotSize filterLotSize = (ExchangeInfoSymbolFilterLotSize)filter;
                        minQty = filterLotSize.MinQty;
                        maxQty = filterLotSize.MaxQty;
                        stepSize = filterLotSize.StepSize;
                    }

                    filter = coin.Filters.Find(x => x.FilterType.ToString() == "MinNotional");

                    if (filter != null)
                    {
                        ExchangeInfoSymbolFilterMinNotional  filterMinNotional = (ExchangeInfoSymbolFilterMinNotional)filter;
                        minNotional = filterMinNotional.MinNotional;
                    }

                    var tradeOpportunityFirstCheck = GetTradeOpportunity(coin.BaseAsset, coin.QuoteAsset, quoteAssetPriceInDollars, minQty, maxQty, stepSize, highestPrice, minNotional);

                    if (tradeOpportunityFirstCheck != null)
                    {
                        tradeOpportunitiesFirstCheck.Add(tradeOpportunityFirstCheck);
                        logger.Debug("First Check: " + tradeOpportunityFirstCheck.BaseAsset + tradeOpportunityFirstCheck.QuoteAsset);
                        Console.WriteLine("First Check: " + tradeOpportunityFirstCheck.BaseAsset + tradeOpportunityFirstCheck.QuoteAsset);
                    }
                }

                logger.Debug("------------First Check Finished------------------");
                Console.WriteLine("------------First Check Finished------------------");

                if (tradeOpportunitiesFirstCheck.Count == 0)
                    return;

                System.Threading.Thread.Sleep(60000);

                foreach (TradeOpportunityDTO tradeOpportunityFirstCheck in tradeOpportunitiesFirstCheck)
                {
                    var tradeOpportunitySecondCheck = GetTradeOpportunity(tradeOpportunityFirstCheck.BaseAsset, tradeOpportunityFirstCheck.QuoteAsset, quoteAssetPriceInDollars, tradeOpportunityFirstCheck.MinQty, tradeOpportunityFirstCheck.MaxQty, tradeOpportunityFirstCheck.StepSize, tradeOpportunityFirstCheck.HighestPrice, tradeOpportunityFirstCheck.MinNotional);

                    if (tradeOpportunitySecondCheck != null)
                    {
                        tradeOpportunitiesSecondCheck.Add(tradeOpportunitySecondCheck);
                        logger.Debug("Second Check: " + tradeOpportunitySecondCheck.BaseAsset + tradeOpportunitySecondCheck.QuoteAsset);
                        Console.WriteLine("Second Check: " + tradeOpportunitySecondCheck.BaseAsset + tradeOpportunitySecondCheck.QuoteAsset);
                    }
                }

                logger.Debug("------------Second Check Finished-----------------");
                Console.WriteLine("------------Second Check Finished-----------------");

                if (tradeOpportunitiesSecondCheck.Count == 0)
                    return;

                System.Threading.Thread.Sleep(60000);

                foreach (TradeOpportunityDTO tradeOpportunitySecondCheck in tradeOpportunitiesSecondCheck)
                {
                    var tradeOpportunityThirdCheck = GetTradeOpportunity(tradeOpportunitySecondCheck.BaseAsset, tradeOpportunitySecondCheck.QuoteAsset, quoteAssetPriceInDollars, tradeOpportunitySecondCheck.MinQty, tradeOpportunitySecondCheck.MaxQty, tradeOpportunitySecondCheck.StepSize, tradeOpportunitySecondCheck.HighestPrice, tradeOpportunitySecondCheck.MinNotional);

                    if (tradeOpportunityThirdCheck != null)
                    {
                        tradeOpportunitiesThirdCheck.Add(tradeOpportunityThirdCheck);
                        logger.Debug("Third Check: " + tradeOpportunityThirdCheck.BaseAsset + tradeOpportunityThirdCheck.QuoteAsset);
                        Console.WriteLine("Third Check: " + tradeOpportunityThirdCheck.BaseAsset + tradeOpportunityThirdCheck.QuoteAsset);
                    }
                }

                logger.Debug("-------------Third Check Finished-----------------");
                Console.WriteLine("-------------Third Check Finished-----------------");

                if (tradeOpportunitiesThirdCheck.Count == 0)
                    return;

                System.Threading.Thread.Sleep(60000);

                foreach (TradeOpportunityDTO tradeOpportunityThirdCheck in tradeOpportunitiesThirdCheck)
                {
                    var tradeOpportunityFourthCheck = GetTradeOpportunity(tradeOpportunityThirdCheck.BaseAsset, tradeOpportunityThirdCheck.QuoteAsset, quoteAssetPriceInDollars, tradeOpportunityThirdCheck.MinQty, tradeOpportunityThirdCheck.MaxQty, tradeOpportunityThirdCheck.StepSize, tradeOpportunityThirdCheck.HighestPrice, tradeOpportunityThirdCheck.MinNotional);

                    if (tradeOpportunityFourthCheck != null)
                    {
                        tradeOpportunitiesFourthCheck.Add(tradeOpportunityFourthCheck);
                        logger.Debug("Fourth Check: " + tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset);
                        Console.WriteLine("Fourth Check: " + tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset);
                    }
                }

                logger.Debug("-------------Fourth Check Finished----------------");
                Console.WriteLine("-------------Fourth Check Finished----------------");

                foreach (TradeOpportunityDTO tradeOpportunityFourthCheck in tradeOpportunitiesFourthCheck)
                {
                    if (tradeOpportunityFourthCheck.BuyQuantity <= 0 || tradeOpportunityFourthCheck.SellQuantity <= 0)
                    {
                        logger.Error(tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset + ": Error - Invalid quantity.");
                        Console.WriteLine(tradeOpportunityFourthCheck.BaseAsset + tradeOpportunityFourthCheck.QuoteAsset + ": Error - Invalid quantity.");
                        continue;
                    }

                    bool orderSet = binanceService.SetNewOrder(0, tradeOpportunityFourthCheck.BaseAsset, tradeOpportunityFourthCheck.QuoteAsset, tradeOpportunityFourthCheck.BaseAssetPrecision, tradeOpportunityFourthCheck.MinQty, tradeOpportunityFourthCheck.MaxQty, tradeOpportunityFourthCheck.StepSize, tradeOpportunityFourthCheck.MinNotional, tradeOpportunityFourthCheck.QuoteAssetPriceInUsd, tradeOpportunityFourthCheck.LastBaseAssetPrice, tradeOpportunityFourthCheck.BuyPrice, tradeOpportunityFourthCheck.BuyQuantity, tradeOpportunityFourthCheck.SellPrice, tradeOpportunityFourthCheck.SellQuantity, tradeOpportunityFourthCheck.MinimumSellPrice, OrderSide.Buy, OrderType.Limit).Result;

                    if (orderSet)
                    {
                        ChartServices chartServices = new ChartServices();

                        FileParameter chartImage = chartServices.GenerateOrderBookChartImage(quoteAssetPriceInDollars, tradeOpportunityFourthCheck);

                        TelegramService telegramService = new TelegramService();
                        HttpResponseMessage response = telegramService.SendImageAsync(chartImage).Result;

                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                            Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                        }
                    }
                }

                logger.Debug("-----------------Process Finished-----------------");
                Console.WriteLine("-----------------Process Finished-----------------");
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));

                TelegramService telegramService = new TelegramService();
                HttpResponseMessage response = telegramService.SendMessageAsync(JsonConvert.SerializeObject(e)).Result;
            }
        }

        private TradeOpportunityDTO GetTradeOpportunity(string baseAsset, string quoteAsset, decimal quoteAssetPriceInDollars, decimal minQty, decimal maxQty, decimal stepSize, decimal highestPrice, decimal minNotional)
        {
            BinanceService binanceService = new BinanceService();
            TradingServices tradingServices = new TradingServices();

            OrderBookResponse orderBook = binanceService.GetOrderBook(baseAsset + quoteAsset).Result;

            decimal lastBaseAssetPrice = binanceService.GetCurrentPrice(baseAsset + quoteAsset).Result;

            int baseAssetPrecision = tradingServices.GetAssetPrecision(orderBook);

            if (baseAssetPrecision <= 0 || baseAssetPrecision > 8)
                return null;

            decimal baseAssetPriceInDollars = Math.Round(lastBaseAssetPrice * quoteAssetPriceInDollars, baseAssetPrecision);

            return tradingServices.ValidateTradeOpportunity(orderBook, lastBaseAssetPrice, baseAssetPriceInDollars, quoteAssetPriceInDollars, baseAssetPrecision, baseAsset, quoteAsset, minQty, maxQty, stepSize, highestPrice, minNotional);
        }

        public decimal GetCoinDailyVolume(string symbol, out decimal highestPrice)
        {
            BinanceService binanceService = new BinanceService();
            decimal dailyVolume = 0;
            highestPrice = 0;

            var dailyTicker = binanceService.GetDailyTicker(symbol).Result;

            if (dailyTicker != null)
            {
                dailyVolume = dailyTicker.QuoteVolume;
                highestPrice = dailyTicker.HighPrice;
            }

            return dailyVolume;
        }
    }
}
