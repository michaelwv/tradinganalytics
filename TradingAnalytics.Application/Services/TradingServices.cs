using BinanceExchange.API.Models.Response;
using System;
using TradingAnalytics.Application.DTO;

namespace TradingAnalytics.Application.Services
{
    public class TradingServices
    {
        public TradeOpportunityDTO GetTradeOpportunity(OrderBookResponse orderBook, decimal quoteAssetPriceInDollars, int assetPrecision, string symbol)
        {
            decimal buyPrice = GetBuyPrice(orderBook, assetPrecision, quoteAssetPriceInDollars);

            if (buyPrice == 0)
                return null;

            decimal sellPrice = GetSellPrice(orderBook, buyPrice, assetPrecision, quoteAssetPriceInDollars);

            if (buyPrice > 0 && sellPrice > 0)
            {
                return new TradeOpportunityDTO()
                {
                    BuyPrice = buyPrice,
                    SellPrice = sellPrice,
                    Symbol = symbol
                };
            }
            else
                return null;
        }

        private decimal GetBuyPrice(OrderBookResponse orderBook, int assetPrecision, decimal quoteAssetPriceInDollars)
        {
            decimal bidValueToConsiderWall = SettingsService.GetBidValueToConsiderWall();
            decimal buyPrice = 0;

            foreach (var bid in orderBook.Bids.GetRange(0, 6))
            {
                decimal totalValue = Math.Round(bid.Price * bid.Quantity * quoteAssetPriceInDollars, 2);

                if (totalValue >= bidValueToConsiderWall) //Buy Wall Identified
                    return Math.Round(bid.Price + (1 / (decimal)Math.Pow(10, assetPrecision)), assetPrecision);
            }

            return buyPrice;
        }

        private decimal GetSellPrice(OrderBookResponse orderBook, decimal buyPrice, int assetPrecision, decimal quoteAssetPriceInDollars)
        {
            decimal askValueToConsiderWall = SettingsService.GetAskValueToConsiderWall();
            decimal sellPrice = 0;
            decimal currentPrice = 0;
            decimal desiredProfitPercentage = SettingsService.GetDesiredProfitPercentage();
            bool limitToProfit = SettingsService.GetLimitToProfit();

            foreach (var ask in orderBook.Asks.GetRange(0, 6))
            {
                decimal totalValue = Math.Round(ask.Price * ask.Quantity * quoteAssetPriceInDollars, 2);
                currentPrice = ask.Price;

                if (totalValue >= askValueToConsiderWall) //Sell Wall Identified
                {
                    sellPrice = ask.Price - (1 / (decimal)Math.Pow(10, assetPrecision));
                    break;
                }
            }

            if (sellPrice == 0)
                sellPrice = currentPrice;

            if ((sellPrice * 100 / buyPrice) - 100 > desiredProfitPercentage)
            {
                if (limitToProfit)
                    sellPrice = buyPrice + (buyPrice * 1 / 100);
            }
            else
                sellPrice = 0;

            return Math.Round(sellPrice, assetPrecision);
        }

        public int GetAssetPrecision(OrderBookResponse orderBook, int defaultAssetPrecision)
        {
            bool precisionFound = false;
            string zerosToFind = "";
            int zerosFound = 0;

            while (!precisionFound)
            {
                zerosToFind += "0";

                for (var index = 0; index < orderBook.Bids.Count; index++)
                {
                    decimal price = orderBook.Bids[index].Price;
                    precisionFound = price.ToString().Substring(price.ToString().Length - zerosToFind.Length, zerosToFind.Length) != zerosToFind;

                    if (precisionFound)
                        break;
                }

                if (!precisionFound)
                    zerosFound++;
            }

            return defaultAssetPrecision - zerosFound;
        }
    }
}
