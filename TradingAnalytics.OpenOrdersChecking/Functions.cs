using BinanceExchange.API.Enums;
using BinanceExchange.API.Extensions;
using BinanceExchange.API.Models.Response;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using TradingAnalytics.Application.Services;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.OpenOrdersChecking
{
    public class Functions
    {
        public void CheckOpenOrders()
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            logger.Debug("-----------------Process Started------------------");
            Console.WriteLine("-----------------Process Started------------------");

            try
            {
                BinanceService binanceService = new BinanceService();
                TradingServices tradingServices = new TradingServices();

                List<Order> openOrders = tradingServices.GetOpenOrders();

                if (openOrders.Count == 0)
                {
                    logger.Debug("No order found.");
                    Console.WriteLine("No order found.");
                }
                else
                {

                    foreach (var order in openOrders)
                    {
                        string clientOrderId = "";
                        string orderSide = "";
                        decimal price = 0;
                        string status = "";
                        string message = "";
                        int orderId = order.Id;

                        TelegramService telegramService = new TelegramService();
                        HttpResponseMessage response;

                        if (order.BuyStatus == "NEW" || order.BuyStatus == "PARTIALLY_FILLED")
                        {
                            clientOrderId = order.BuyClientOrderId;
                            price = order.BuyPrice;
                            orderSide = "BUY";
                            status = order.BuyStatus;
                        }
                        else
                        {
                            clientOrderId = order.SellClientOrderId;
                            price = order.SellPrice;
                            orderSide = "SELL";
                            status = order.SellStatus;
                        }

                        decimal lastBaseAssetPrice = binanceService.GetCurrentPrice(order.BaseAsset + order.QuoteAsset).Result;

                        logger.Debug(order.BaseAsset + order.QuoteAsset + " current price: " + lastBaseAssetPrice.ToString());

                        decimal quoteAssetPriceInDollars = Math.Round(binanceService.GetCurrentPrice(SettingsService.GetQuoteAssetToTrade() + "USDT").Result, 2);

                        if (!string.IsNullOrEmpty(clientOrderId))
                        {
                            var queryOrder = binanceService.GetOrder(order.BaseAsset + order.QuoteAsset, clientOrderId).Result;

                            //Check if the order status has changed
                            if (status.ToUpper() != EnumExtensions.GetEnumMemberValue(queryOrder.Status).ToUpper())
                            {
                                tradingServices.UpdateOrderStatus(clientOrderId, orderSide, EnumExtensions.GetEnumMemberValue(queryOrder.Status), lastBaseAssetPrice, quoteAssetPriceInDollars);

                                if (queryOrder.Status == OrderStatus.Filled)
                                {
                                    message = orderSide + " ORDER FILLED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + price + " - Quantity: " + (orderSide == "BUY" ? order.BuyQuantity : order.SellQuantity);
                                    logger.Debug(message);
                                    Console.WriteLine(message);

                                    response = telegramService.SendMessageAsync(message).Result;

                                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                    {
                                        logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                        Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                    }

                                    if (queryOrder.Side == OrderSide.Buy)
                                    {
                                        bool orderSet = binanceService.SetNewOrder(orderId, order.BaseAsset, order.QuoteAsset, order.AssetPrecision, order.BaseAssetMinQuantity, order.BaseAssetMaxQuantity, order.BaseAssetStepSize, order.BaseAssetMinNotional, quoteAssetPriceInDollars, lastBaseAssetPrice, order.BuyPrice, order.BuyQuantity, order.SellPrice, order.SellQuantity, order.MinimumSellPrice, OrderSide.Sell, OrderType.Limit).Result;

                                        if (orderSet)
                                        {
                                            message = "SELL ORDER CREATED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + order.SellPrice + " - Quantity: " + order.SellQuantity;
                                            logger.Debug(message);
                                            Console.WriteLine(message);

                                            response = telegramService.SendMessageAsync(message).Result;

                                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                            {
                                                logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                            }
                                        }
                                        else
                                        {
                                            message = "Unexpected error when creating sell order for symbol " + order.BaseAsset + order.QuoteAsset + ".";
                                            logger.Error(message);
                                            Console.WriteLine(message);

                                            response = telegramService.SendMessageAsync(message).Result;

                                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                            {
                                                logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                            }

                                            continue;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                tradingServices.UpdateLastPrice(clientOrderId, orderSide, lastBaseAssetPrice);

                                if (status == "PARTIALLY_FILLED")
                                    continue;

                                OrderBookResponse orderBook = binanceService.GetOrderBook(order.BaseAsset + order.QuoteAsset).Result;

                                if (orderSide == "BUY")
                                {
                                    if (!tradingServices.LowerWallStillExists(orderBook, order.AssetPrecision, quoteAssetPriceInDollars, order.BuyPrice))
                                    {
                                        var cancelOrder = binanceService.CancelOrder(order.BaseAsset + order.QuoteAsset, order.BuyClientOrderId).Result;

                                        if (cancelOrder != null)
                                        {
                                            if (cancelOrder.Status == OrderStatus.Cancelled)
                                            {
                                                tradingServices.UpdateOrderStatus(clientOrderId, orderSide, EnumExtensions.GetEnumMemberValue(OrderStatus.Cancelled), lastBaseAssetPrice, quoteAssetPriceInDollars);

                                                message = "BUY ORDER CANCELED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + price + " - Quantity: " + order.BuyQuantity;
                                                logger.Debug(message);
                                                Console.WriteLine(message);

                                                response = telegramService.SendMessageAsync(message).Result;

                                                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                {
                                                    logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                    Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                }
                                            }
                                            else
                                            {
                                                message = "Unexpected error when cancelling buy order for symbol " + order.BaseAsset + order.QuoteAsset + " and ID " + clientOrderId + ".";
                                                logger.Error(message);
                                                Console.WriteLine(message);

                                                response = telegramService.SendMessageAsync(message).Result;

                                                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                {
                                                    logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                    Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                }

                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            message = "Unexpected error when cancelling buy order for symbol " + order.BaseAsset + order.QuoteAsset + " and ID " + clientOrderId + ".";
                                            logger.Error(message);
                                            Console.WriteLine(message);

                                            response = telegramService.SendMessageAsync(message).Result;

                                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                            {
                                                logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                            }

                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    decimal sellWallPrice = 0;

                                    if (tradingServices.UpperWallFormedBeforeDesiredProfit(orderBook, quoteAssetPriceInDollars, order.MinimumSellPrice, order.SellPrice, out sellWallPrice))
                                    {
                                        decimal sellPrice = Math.Round(sellWallPrice - (1 / (decimal)Math.Pow(10, order.AssetPrecision)), order.AssetPrecision);

                                        if (sellPrice != order.SellPrice)
                                        {
                                            var cancelOrder = binanceService.CancelOrder(order.BaseAsset + order.QuoteAsset, order.SellClientOrderId).Result;

                                            if (cancelOrder != null)
                                            {
                                                if (cancelOrder.Status == OrderStatus.Cancelled)
                                                {
                                                    tradingServices.UpdateOrderStatus(clientOrderId, orderSide, EnumExtensions.GetEnumMemberValue(OrderStatus.Cancelled), lastBaseAssetPrice, quoteAssetPriceInDollars);

                                                    message = "UPPER WALL FORMED - SELL ORDER CANCELED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + price + " - Quantity: " + order.SellQuantity;
                                                    logger.Debug(message);
                                                    Console.WriteLine(message);

                                                    var orderRecreated = binanceService.SetNewOrder(order.Id, order.BaseAsset, order.QuoteAsset, order.AssetPrecision, order.BaseAssetMinQuantity, order.BaseAssetMaxQuantity, order.BaseAssetStepSize, order.BaseAssetMinNotional, quoteAssetPriceInDollars, lastBaseAssetPrice, order.BuyPrice, order.BuyQuantity, sellPrice, order.SellQuantity, order.MinimumSellPrice, OrderSide.Sell, OrderType.Limit).Result;

                                                    if (orderRecreated)
                                                    {
                                                        message = "UPPER WALL FORMED - SELL ORDER RECREATED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + sellPrice + " - Quantity: " + order.SellQuantity;
                                                        logger.Debug(message);
                                                        Console.WriteLine(message);

                                                        //response = telegramService.SendMessageAsync(message).Result;

                                                        //if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                        //{
                                                        //    logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                        //    Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                        //}
                                                    }
                                                    else
                                                    {
                                                        message = "Unexpected error when recreating sell order for symbol " + order.BaseAsset + order.QuoteAsset + ".";
                                                        logger.Error(message);
                                                        Console.WriteLine(message);

                                                        response = telegramService.SendMessageAsync(message).Result;

                                                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                        {
                                                            logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                            Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                        }

                                                        continue;
                                                    }
                                                }
                                                else
                                                {
                                                    message = "Unexpected error when cancelling sell order for symbol " + order.BaseAsset + order.QuoteAsset + " and ID " + clientOrderId + ".";
                                                    logger.Error(message);
                                                    Console.WriteLine(message);

                                                    response = telegramService.SendMessageAsync(message).Result;

                                                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                    {
                                                        logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                        Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                    }

                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                message = "Unexpected error when cancelling sell order for symbol " + order.BaseAsset + order.QuoteAsset + " and ID " + clientOrderId + ".";
                                                logger.Error(message);
                                                Console.WriteLine(message);

                                                response = telegramService.SendMessageAsync(message).Result;

                                                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                {
                                                    logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                    Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                }

                                                continue;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        decimal baseAssetPriceInDollars = Math.Round(lastBaseAssetPrice * quoteAssetPriceInDollars, order.AssetPrecision);
                                        decimal buyQuantity = 0;
                                        decimal sellQuantity = 0;
                                        decimal minimumSellPrice = 0;
                                        decimal maximumSellPrice = 0;

                                        decimal newSellPrice = tradingServices.GetSellPrice(orderBook, order.BuyPrice, order.AssetPrecision, lastBaseAssetPrice, baseAssetPriceInDollars, quoteAssetPriceInDollars, true, order.BaseAssetMinQuantity, order.BaseAssetMaxQuantity, order.BaseAssetStepSize, order.BaseAssetMinNotional, out buyQuantity, out sellQuantity, out minimumSellPrice, out maximumSellPrice);

                                        if (newSellPrice > order.BuyPrice && newSellPrice != order.SellPrice && newSellPrice >= minimumSellPrice)
                                        {
                                            var cancelOrder = binanceService.CancelOrder(order.BaseAsset + order.QuoteAsset, order.SellClientOrderId).Result;

                                            if (cancelOrder != null)
                                            {
                                                if (cancelOrder.Status == OrderStatus.Cancelled)
                                                {
                                                    tradingServices.UpdateOrderStatus(clientOrderId, orderSide, EnumExtensions.GetEnumMemberValue(OrderStatus.Cancelled), lastBaseAssetPrice, quoteAssetPriceInDollars);

                                                    message = "SELL ORDER CANCELED TO BE ADJUSTED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + order.SellPrice + " - Quantity: " + order.SellQuantity;
                                                    logger.Debug(message);
                                                    Console.WriteLine(message);

                                                    var orderAdjusted = binanceService.SetNewOrder(order.Id, order.BaseAsset, order.QuoteAsset, order.AssetPrecision, order.BaseAssetMinQuantity, order.BaseAssetMaxQuantity, order.BaseAssetStepSize, order.BaseAssetMinNotional, quoteAssetPriceInDollars, lastBaseAssetPrice, order.BuyPrice, order.BuyQuantity, newSellPrice, order.SellQuantity, order.MinimumSellPrice, OrderSide.Sell, OrderType.Limit).Result;

                                                    if (orderAdjusted)
                                                    {
                                                        message = "SELL ORDER ADJUSTED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + newSellPrice + " - Quantity: " + order.SellQuantity;
                                                        logger.Debug(message);
                                                        Console.WriteLine(message);

                                                        //response = telegramService.SendMessageAsync(message).Result;

                                                        //if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                        //{
                                                        //    logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                        //    Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                        //}
                                                    }
                                                    else
                                                    {
                                                        message = "Unexpected error when adjusting sell order for symbol " + order.BaseAsset + order.QuoteAsset + ".";
                                                        logger.Error(message);
                                                        Console.WriteLine(message);

                                                        response = telegramService.SendMessageAsync(message).Result;

                                                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                        {
                                                            logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                            Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                        }

                                                        continue;
                                                    }
                                                }
                                                else
                                                {
                                                    message = "Unexpected error when cancelling sell order for symbol " + order.BaseAsset + order.QuoteAsset + " and ID " + clientOrderId + ".";
                                                    logger.Error(message);
                                                    Console.WriteLine(message);

                                                    response = telegramService.SendMessageAsync(message).Result;

                                                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                    {
                                                        logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                        Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                    }

                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                message = "Unexpected error when cancelling sell order for symbol " + order.BaseAsset + order.QuoteAsset + " and ID " + clientOrderId + ".";
                                                logger.Error(message);
                                                Console.WriteLine(message);

                                                response = telegramService.SendMessageAsync(message).Result;

                                                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                                {
                                                    logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                                    Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                                }

                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (status.ToUpper() == "PENDING_CREATION" && orderSide == "SELL")
                            {
                                decimal baseAssetPriceInDollars = Math.Round(lastBaseAssetPrice * quoteAssetPriceInDollars, order.AssetPrecision);
                                decimal buyQuantity = 0;
                                decimal sellQuantity = 0;
                                decimal minimumSellPrice = 0;
                                decimal maximumSellPrice = 0;

                                OrderBookResponse orderBook = binanceService.GetOrderBook(order.BaseAsset + order.QuoteAsset).Result;

                                decimal newSellPrice = tradingServices.GetSellPrice(orderBook, order.BuyPrice, order.AssetPrecision, lastBaseAssetPrice, baseAssetPriceInDollars, quoteAssetPriceInDollars, true, order.BaseAssetMinQuantity, order.BaseAssetMaxQuantity, order.BaseAssetStepSize, order.BaseAssetMinNotional, out buyQuantity, out sellQuantity, out minimumSellPrice, out maximumSellPrice);

                                if (newSellPrice > order.BuyPrice && newSellPrice != order.SellPrice && newSellPrice >= minimumSellPrice)
                                {
                                    var orderCreated = binanceService.SetNewOrder(order.Id, order.BaseAsset, order.QuoteAsset, order.AssetPrecision, order.BaseAssetMinQuantity, order.BaseAssetMaxQuantity, order.BaseAssetStepSize, order.BaseAssetMinNotional, quoteAssetPriceInDollars, lastBaseAssetPrice, order.BuyPrice, order.BuyQuantity, newSellPrice, order.SellQuantity, order.MinimumSellPrice, OrderSide.Sell, OrderType.Limit).Result;

                                    if (orderCreated)
                                    {
                                        message = "PENDING SELL ORDER CREATED: " + order.BaseAsset + order.QuoteAsset + " - Price: " + newSellPrice + " - Quantity: " + order.SellQuantity;
                                        logger.Debug(message);
                                        Console.WriteLine(message);

                                        response = telegramService.SendMessageAsync(message).Result;

                                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                        {
                                            logger.Error("Error sending Telegram message. " + response.ReasonPhrase);
                                            Console.WriteLine("Error sending Telegram message. " + response.ReasonPhrase);
                                        }
                                    }
                                }
                            }
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
                HttpResponseMessage response = telegramService.SendMessageAsync(e.Message).Result;
            }
        }
    }
}
