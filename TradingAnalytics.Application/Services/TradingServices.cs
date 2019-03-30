using BinanceExchange.API.Models.Response;
using System;
using System.Collections.Generic;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.DataAccess;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.Application.Services
{
    public class TradingServices
    {
        public TradeOpportunityDTO ValidateTradeOpportunity(OrderBookResponse orderBook, decimal lastBaseAssetPrice, decimal baseAssetPriceInDollars, decimal quoteAssetPriceInDollars, int assetPrecision, string baseAsset, string quoteAsset, decimal minQty, decimal maxQty, decimal stepSize, decimal highestPrice, decimal minNotional)
        {
            decimal buyPrice = GetBuyPrice(orderBook, assetPrecision, quoteAssetPriceInDollars);
            decimal buyQuantity = 0;
            decimal sellQuantity = 0;
            decimal minimumSellPrice = 0;
            decimal maximumSellPrice = 0;

            if (buyPrice == 0)
                return null;

            decimal sellPrice = GetSellPrice(orderBook, buyPrice, assetPrecision, lastBaseAssetPrice, baseAssetPriceInDollars, quoteAssetPriceInDollars, false, minQty, maxQty, stepSize, minNotional, out buyQuantity, out sellQuantity, out minimumSellPrice, out maximumSellPrice);

            if ((buyPrice * buyQuantity) < minNotional)
                return null;

            if (sellPrice > highestPrice)
                return null;

            if (buyPrice > 0 && sellPrice > 0)
            {
                return new TradeOpportunityDTO()
                {
                    BaseAsset = baseAsset,
                    QuoteAsset = quoteAsset,
                    BuyPrice = buyPrice,
                    BuyQuantity = buyQuantity,
                    SellPrice = sellPrice,
                    SellQuantity = sellQuantity,
                    MinimumSellPrice = minimumSellPrice,
                    MaximumSellPrice = maximumSellPrice,
                    LastBaseAssetPrice = lastBaseAssetPrice,
                    BaseAssetPriceInUsd = baseAssetPriceInDollars,
                    QuoteAssetPriceInUsd = quoteAssetPriceInDollars,
                    BaseAssetPrecision = assetPrecision,
                    OrderBook = orderBook,
                    MinQty = minQty,
                    MaxQty = maxQty,
                    StepSize = stepSize,
                    HighestPrice = highestPrice,
                    MinNotional = minNotional
                };
            }
            else
                return null;
        }

        public decimal GetStochasticRsi(string symbol)
        {
            BinanceService binanceService = new BinanceService();

            var klineCandleStick = binanceService.GetKlineCandleStick(symbol).Result;
            decimal lowestLow = 0;
            decimal highestHigh = 0;
            decimal currentClose = 0;
            decimal stochastic = -1;

            if (klineCandleStick.Count > 0)
            {
                klineCandleStick.Sort((x, y) => y.OpenTime.CompareTo(x.OpenTime));
                currentClose = klineCandleStick[0].Close;

                klineCandleStick.Sort((x, y) => x.Low.CompareTo(y.Low));
                lowestLow = klineCandleStick[0].Low;

                klineCandleStick.Sort((x, y) => y.High.CompareTo(x.High));
                highestHigh = klineCandleStick[0].High;

                stochastic = (currentClose - lowestLow) / (highestHigh - lowestLow) * 100;
            }

            return stochastic;
        }

        public int UpdateOrderStatus(string clientOrderId, string side, string status, decimal lastPrice, decimal quoteAssetPriceInDollars)
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.UpdateOrderStatus(clientOrderId, side, status, lastPrice, quoteAssetPriceInDollars);
        }

        public int UpdateLastPrice(string clientOrderId, string side, decimal lastPrice)
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.UpdateLastPrice(clientOrderId, side, lastPrice);
        }

        public bool LowerWallStillExists(OrderBookResponse orderBook, int assetPrecision, decimal quoteAssetPriceInDollars,  decimal buyPrice)
        {
            decimal bidValueToConsiderWall = SettingsService.GetBidValueToConsiderWall();
            int rangeToFind = SettingsService.GetRangeToFind();
            decimal wallPrice = Math.Round(buyPrice - (1 / (decimal)Math.Pow(10, assetPrecision)), assetPrecision);
            int unitsToConsiderAtBuy = SettingsService.GetUnitsToConsiderAtBuy();

            bool lowerWallStillExists = false;

            foreach (var bid in orderBook.Bids.GetRange(0, rangeToFind + 1))
            {
                decimal totalValue = Math.Round(bid.Price * bid.Quantity * quoteAssetPriceInDollars, 2);

                //Check if the total value of the orders is considered a wall
                if (totalValue >= bidValueToConsiderWall)
                {
                    //Check if the buy wall price is higher than the current buy price
                    if (bid.Price > buyPrice)
                    {
                        return false;
                    }
                    else if (bid.Price < buyPrice)
                    {
                        //Check if the wall price is higher than the current buy price added with unitsToConsiderAtBuy value
                        if (bid.Price < Math.Round(buyPrice - (unitsToConsiderAtBuy / (decimal)Math.Pow(10, assetPrecision)), assetPrecision))
                            return false;
                        else
                            return true;
                    }
                }
            }

            return lowerWallStillExists;
        }

        public List<Order> GetOpenOrders()
        {
            OrderRepository orderRepository = new OrderRepository();

            return orderRepository.GetOpenOrders();
        }

        public bool UpperWallFormedBeforeDesiredProfit(OrderBookResponse orderBook, decimal quoteAssetPriceInDollars, decimal minimumSellPrice, decimal sellPrice, out decimal wallPrice)
        {
            decimal askValueToConsiderWall = SettingsService.GetAskValueToConsiderWall();
            int rangeToFind = SettingsService.GetRangeToFind();

            wallPrice = 0;

            foreach (var ask in orderBook.Asks.GetRange(0, rangeToFind + 1))
            {
                decimal totalValue = Math.Round(ask.Price * ask.Quantity * quoteAssetPriceInDollars, 2);

                if (totalValue >= askValueToConsiderWall && ask.Price > minimumSellPrice) //Sell Wall Identified
                    if (wallPrice == 0 || ask.Price < wallPrice)
                        wallPrice = ask.Price;
            }

            return wallPrice != 0;
        }

        public decimal GetBuyPrice(OrderBookResponse orderBook, int assetPrecision, decimal quoteAssetPriceInDollars)
        {
            decimal bidValueToConsiderWall = SettingsService.GetBidValueToConsiderWall();
            int rangeToFind = SettingsService.GetRangeToFind();
            decimal buyPrice = 0;

            foreach (var bid in orderBook.Bids.GetRange(0, rangeToFind))
            {
                decimal totalValue = Math.Round(bid.Price * bid.Quantity * quoteAssetPriceInDollars, 2);

                if (totalValue >= bidValueToConsiderWall) //Buy Wall Identified
                    return Math.Round(bid.Price + (1 / (decimal)Math.Pow(10, assetPrecision)), assetPrecision);
            }

            return buyPrice;
        }

        public decimal GetSellPrice(OrderBookResponse orderBook, decimal buyPrice, int baseAssetPrecision, decimal lastBaseAssetPrice, decimal baseAssetPriceInDollars, decimal quoteAssetPriceInDollars, bool adjustingSellPrice, decimal minQty, decimal maxQty, decimal stepSize, decimal minNotional, out decimal buyQuantity, out decimal sellQuantity, out decimal minimumSellPrice, out decimal maximumSellPrice)
        {
            decimal lossPercentage = 0;

            buyQuantity = 0;
            sellQuantity = 0;
            minimumSellPrice = 0;
            maximumSellPrice = 0;

            GetOrderQuantityAndPrice(baseAssetPrecision, lastBaseAssetPrice, baseAssetPriceInDollars, minQty, maxQty, stepSize, buyPrice, out buyQuantity, out sellQuantity, out minimumSellPrice, out lossPercentage, out maximumSellPrice);

            decimal askValueToConsiderWall = SettingsService.GetAskValueToConsiderWall();
            int rangeToFind = SettingsService.GetRangeToFind();
            decimal sellPrice = 0;
            decimal currentPrice = 0;
            bool limitToProfit = SettingsService.GetLimitToProfit();

            foreach (var ask in orderBook.Asks.GetRange(0, rangeToFind))
            {
                decimal totalValue = Math.Round(ask.Price * ask.Quantity * quoteAssetPriceInDollars, 2);
                currentPrice = ask.Price;

                if (totalValue >= askValueToConsiderWall) //Sell Wall Identified
                {
                    sellPrice = ask.Price - (1 / (decimal)Math.Pow(10, baseAssetPrecision));
                    break;
                }
            }

            if (sellPrice == 0)
                sellPrice = maximumSellPrice;

            if (sellPrice >= maximumSellPrice)
            {
                if (limitToProfit)
                    sellPrice = maximumSellPrice;
            }
            else if (!adjustingSellPrice)
                sellPrice = 0;

            if ((sellPrice * sellQuantity) < minNotional)
                sellPrice = 0;

            return Math.Round(sellPrice, baseAssetPrecision);
        }

        public int GetAssetPrecision(OrderBookResponse orderBook)
        {
            bool precisionFound = false;
            string zerosToFind = "";
            int zerosFound = 0;
            int defaultAssetPrecision = 0;

            while (!precisionFound)
            {
                zerosToFind += "0";

                for (var index = 0; index < orderBook.Bids.Count; index++)
                {
                    decimal price = orderBook.Bids[index].Price;

                    if (defaultAssetPrecision == 0)
                    {
                        int dotIndex = price.ToString().LastIndexOf(",");

                        if (dotIndex != -1)
                            defaultAssetPrecision = price.ToString().Substring(dotIndex).Length - 1;
                    }

                    precisionFound = price.ToString().Substring(price.ToString().Length - zerosToFind.Length, zerosToFind.Length) != zerosToFind;

                    if (precisionFound)
                        break;
                }

                if (!precisionFound)
                    zerosFound++;
            }

            return defaultAssetPrecision - zerosFound;
        }

        public decimal GetMaxQuantityToSell(decimal quantity, decimal stepSize)
        {
            decimal taxes = SettingsService.GetBinanceTaxes();
            decimal qtyToSell = quantity - (quantity * taxes / 100);

            qtyToSell = GetRoundedSellQuantity(qtyToSell, stepSize);

            return qtyToSell;
        }

        public void GetOrderQuantityAndPrice(int baseAssetPrecision, decimal lastBaseAssetPrice, decimal priceInUsd, decimal minQty, decimal maxQty, decimal stepSize, decimal buyPrice, out decimal buyOrderQuantity, out decimal sellOrderQuantity, out decimal minimumSellPrice, out decimal lossPercentage, out decimal maxSellPrice)
        {
            if (priceInUsd == 0)
                priceInUsd = (decimal)0.01;

            int qtyAssetPrecision = GetValueAssetPrecision(stepSize);

            decimal binanceTaxes = SettingsService.GetBinanceTaxes();
            decimal desiredProfitPercentage = SettingsService.GetDesiredProfitPercentage() + binanceTaxes;
            decimal dollarsToInvest = SettingsService.GetDollarsToInvest();
            decimal buyQty = Math.Round(dollarsToInvest / priceInUsd, qtyAssetPrecision);

            if (buyQty < minQty)
                buyQty = minQty;

            if (buyQty > maxQty)
                buyQty = maxQty;

            buyOrderQuantity = buyQty;
            sellOrderQuantity = GetMaxQuantityToSell(buyQty, stepSize);

            //Considerando Loss Rate
            //minimumSellPrice = Math.Round(buyPrice * buyOrderQuantity / sellOrderQuantity, baseAssetPrecision);

            //Considerando apenas a taxa de comissão da Binance
            minimumSellPrice = Math.Round(buyPrice + (buyPrice * binanceTaxes / 100), baseAssetPrecision);

            lossPercentage = 0;//100 - (buyPrice / minimumSellPrice * 100);

            maxSellPrice = Math.Round(buyPrice + (buyPrice * (lossPercentage + desiredProfitPercentage) / 100), baseAssetPrecision);

            if (maxSellPrice < lastBaseAssetPrice)
                maxSellPrice = lastBaseAssetPrice;
        }

        public int GetValueAssetPrecision(decimal minQty)
        {
            bool precisionFound = false;
            string zerosToFind = "";
            int zerosFound = 0;
            int defaultAssetPrecision = 0;

            while (!precisionFound)
            {
                zerosToFind += "0";

                decimal price = minQty;

                if (defaultAssetPrecision == 0)
                {
                    int dotIndex = price.ToString().LastIndexOf(",");

                    if (dotIndex != -1)
                        defaultAssetPrecision = price.ToString().Substring(dotIndex).Length - 1;
                }

                precisionFound = price.ToString().Substring(price.ToString().Length - zerosToFind.Length, zerosToFind.Length) != zerosToFind;

                if (!precisionFound)
                    zerosFound++;
            }

            return defaultAssetPrecision - zerosFound;
        }

        public decimal GetRoundedSellQuantity(decimal availableQuantity, decimal stepSize)
        {
            int decimalPlaces = GetValueAssetPrecision(stepSize);
            double adjustment = Math.Pow(10, decimalPlaces);

            return Math.Floor(availableQuantity * (decimal)adjustment) / (decimal)adjustment;
        }
    }
}
