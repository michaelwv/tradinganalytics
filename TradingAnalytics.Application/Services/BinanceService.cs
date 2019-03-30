using BinanceExchange.API.Client;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Extensions;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TradingAnalytics.Application.DTO;
using TradingAnalytics.DataAccess;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.Application.Services
{
    public class BinanceService
    {
        public async Task<List<ExchangeInfoSymbol>> GetTradingCoins()
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
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
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public async Task<CancelOrderResponse> CancelOrder(string symbol, string clientOrderId)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                CancelOrderResponse cancelOrder = await client.CancelOrder(new CancelOrderRequest()
                {
                    Symbol = symbol,
                    OriginalClientOrderId = clientOrderId
                });

                return cancelOrder;
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public async Task<OrderResponse> GetOrder(string symbol, string clientOrderId)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                OrderResponse order = await client.QueryOrder(new QueryOrderRequest()
                {
                    Symbol = symbol,
                    OriginalClientOrderId = clientOrderId
                });

                if (order != null)
                    return order;
                else
                    throw new Exception("Unexpected error when getting open order for symbol " + symbol + ".", null);
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public void SetSalesOrder(string symbol, decimal sellPrice)
        {
            throw new NotImplementedException();
        }

        public void UpdateSellOrderUrderWall(string symbol, string clientOrderId)
        {
            throw new NotImplementedException();
        }

        public async Task<OrderBookResponse> GetOrderBook(string symbol)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                OrderBookResponse orderBook = await client.GetOrderBook(symbol, false, 10);

                if (orderBook != null)
                    return orderBook;
                else
                    throw new Exception("Unexpected error when getting order book.", null);
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public async Task<List<KlineCandleStickResponse>> GetKlineCandleStick(string symbol)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));
            var klineLimit = SettingsService.GetKlineCandleLimit();

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                var klineCandleStick = await client.GetKlinesCandlesticks(new GetKlinesCandlesticksRequest()
                {
                    Symbol = symbol,
                    Interval = KlineInterval.FifteenMinutes,
                    Limit = klineLimit
                });

                if (klineCandleStick != null)
                    return klineCandleStick;
                else
                    throw new Exception("Unexpected error when getting kline candle stick.", null);
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public async Task<SymbolPriceChangeTickerResponse> GetDailyTicker(string symbol)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                SymbolPriceChangeTickerResponse dailyTicker = await client.GetDailyTicker(symbol);

                if (dailyTicker != null)
                    return dailyTicker;
                else
                    throw new Exception("Unexpected error when getting daily ticker.", null);
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public async Task<decimal> GetCurrentPrice(string symbol)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

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
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public async Task<bool> SetNewOrder(int orderId, string baseAsset, string quoteAsset, int baseAssetPrecision, decimal baseAssetMinQuantity, decimal baseAssetMaxQuantity, decimal baseAssetStepSize, decimal minNotional, decimal quoteAssetPriceInUsd, decimal lastBaseAssetPrice, decimal buyPrice, decimal buyQuantity, decimal sellPrice, decimal sellQuantity, decimal minimumSellPrice, OrderSide side, OrderType type)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                OrderRepository orderRepository = new OrderRepository();
                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                if (side == OrderSide.Buy)
                {
                    string[] pendingStatus = new string[] { EnumExtensions.GetEnumMemberValue(OrderStatus.New), EnumExtensions.GetEnumMemberValue(OrderStatus.PartiallyFilled) };

                    if (orderRepository.PendingOrderExists(baseAsset, quoteAsset, pendingStatus))
                        return false;

                    decimal availableQuantity = GetAvailableQuantity(quoteAsset);

                    if (availableQuantity < (buyQuantity * buyPrice))
                    {
                        logger.Debug(baseAsset + quoteAsset + ": Insufficient balance to insert new buy order.");
                        Console.WriteLine(baseAsset + quoteAsset + ": Insufficient balance to insert new buy order.");
                        return false;
                    }
                }
                else
                {
                    TradingServices tradingServices = new TradingServices();

                    decimal availableQuantity = GetAvailableQuantity(baseAsset);

                    sellQuantity = tradingServices.GetRoundedSellQuantity(availableQuantity, baseAssetStepSize);
                }

                if (((buyQuantity * buyPrice) < minNotional) || ((sellQuantity * sellPrice) < minNotional))
                {
                    logger.Debug(baseAsset + quoteAsset + ": Order total price is lower than the min notional value.");
                    Console.WriteLine(baseAsset + quoteAsset + ": Order total price is lower than the min notional value.");

                    if (side == OrderSide.Sell)
                    {
                        if (orderRepository.UpdateSellOrderWithPendingStatus(orderId) > 0)
                        {
                            logger.Debug(baseAsset + quoteAsset + ": SELL ORDER UPDATED TO PENDING CREATION STATUS.");
                            Console.WriteLine(baseAsset + quoteAsset + ": SELL ORDER UPDATED TO PENDING CREATION STATUS.");
                        }
                        else
                        {
                            logger.Error(baseAsset + quoteAsset + ": ERROR UPDATING SELL ORDER TO PENDING CREATION STATUS.");
                            Console.WriteLine(baseAsset + quoteAsset + ": ERROR UPDATING SELL ORDER TO PENDING CREATION STATUS.");
                        }
                    }

                    return false;
                }

                var binanceResult = await client.CreateOrder(new CreateOrderRequest()
                {
                    Symbol = baseAsset + quoteAsset,
                    Side = side,
                    Type = type,
                    Quantity = (side == OrderSide.Buy ? buyQuantity : sellQuantity),
                    Price = (side == OrderSide.Buy ? buyPrice : sellPrice),
                    NewOrderResponseType = NewOrderResponseType.Result,
                    TimeInForce = TimeInForce.GTC
                });

                if (binanceResult.ClientOrderId != null)
                {
                    if (side == OrderSide.Buy)
                    {
                        Order order = new Order()
                        {
                            Id = 0,
                            BaseAsset = baseAsset,
                            QuoteAsset = quoteAsset,
                            AssetPrecision = baseAssetPrecision,
                            BaseAssetStepSize = baseAssetStepSize,
                            BaseAssetMinNotional = minNotional,
                            BaseAssetMinQuantity = baseAssetMinQuantity,
                            BaseAssetMaxQuantity = baseAssetMaxQuantity,
                            BuyQuantity = buyQuantity,
                            BuyPrice = buyPrice,
                            BuyStatus = EnumExtensions.GetEnumMemberValue(OrderStatus.New),
                            BuyStatusDate = DateTime.Now,
                            BuyClientOrderId = binanceResult.ClientOrderId,
                            BuyIncDate = DateTime.Now,
                            QuoteAssetPriceAtBuy = quoteAssetPriceInUsd,
                            SellQuantity = sellQuantity,
                            SellPrice = sellPrice,
                            MinimumSellPrice = minimumSellPrice,
                            LastPrice = lastBaseAssetPrice,
                            LastPriceDate = DateTime.Now
                        };

                        if (orderRepository.CreateOrder(order) > 0)
                        {
                            logger.Debug("NEW BUY ORDER: " + baseAsset + quoteAsset + " - Buy Price: " + buyPrice + " - Sell Price: " + sellPrice + " - ID: " + binanceResult.ClientOrderId);
                            Console.WriteLine("NEW BUY ORDER: " + baseAsset + quoteAsset + " - Buy Price: " + buyPrice + " - Sell Price: " + sellPrice + " - ID: " + binanceResult.ClientOrderId);
                            return true;
                        }
                        else
                        {
                            logger.Error("Unable to create new buy order.");
                            Console.WriteLine("Unable to create new buy order.");
                            return false;
                        }
                    }
                    else
                    {
                        Order order = new Order()
                        {
                            Id = orderId,
                            SellQuantity = sellQuantity,
                            SellPrice = sellPrice,
                            SellStatus = EnumExtensions.GetEnumMemberValue(OrderStatus.New),
                            SellStatusDate = DateTime.Now,
                            SellClientOrderId = binanceResult.ClientOrderId,
                            SellIncDate = DateTime.Now,
                            QuoteAssetPriceAtSell = quoteAssetPriceInUsd
                        };

                        if (orderRepository.UpdateOrder(order) > 0)
                        {
                            logger.Debug("NEW SELL ORDER: " + baseAsset + quoteAsset + " - Buy Price: " + buyPrice + " - Sell Price: " + sellPrice + " - ID: " + binanceResult.ClientOrderId);
                            Console.WriteLine("NEW SELL ORDER: " + baseAsset + quoteAsset + " - Buy Price: " + buyPrice + " - Sell Price: " + sellPrice + " - ID: " + binanceResult.ClientOrderId);
                            return true;
                        }
                        else
                        {
                            logger.Error("Unable to create new sell order.");
                            Console.WriteLine("Unable to create new sell order.");
                            return false;
                        }
                    }
                }
                else
                {
                    logger.Error("Unable to create new order.");
                    Console.WriteLine("Unable to create new order.");
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }

        public decimal GetAvailableQuantity(string symbol)
        {
            var logger = LogManager.GetLogger(typeof(BinanceService));

            try
            {
                decimal availableQuantity = 0;

                SecurityDTO binanceKeys = SettingsService.GetBinanceKeys();

                var client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = binanceKeys.ApiKey,
                    SecretKey = binanceKeys.SecretKey,
                    Logger = logger,
                });

                var binanceResult = client.GetAccountInformation().Result;
                var ethBalance = binanceResult.Balances.Find(x => x.Asset.Equals(symbol));

                if (ethBalance != null)
                    availableQuantity = ethBalance.Free;

                return availableQuantity;
            }
            catch (Exception e)
            {
                logger.Error(JsonConvert.SerializeObject(e));
                throw e;
            }
        }
    }
}

