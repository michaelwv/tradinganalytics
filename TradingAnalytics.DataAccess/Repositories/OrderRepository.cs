using System;
using System.Collections.Generic;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.DataAccess
{
    public class OrderRepository
    {
        public bool PendingOrderExists(string baseAsset, string quoteAsset, string[] pendingStatus)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "SELECT COUNT(*) FROM orders " +
                             "WHERE BaseAsset = @baseAsset " +
                             "AND QuoteAsset = @quoteAsset " +
                             "AND (";

                for (int i = 0; i < pendingStatus.Length; i++)
                {
                    if (i > 0)
                        cmd += " OR ";

                    cmd += "BuyStatus = '" + pendingStatus[i] + "' OR SellStatus = '" + pendingStatus[i] + "'";
                }

                cmd += ")";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@baseAsset", baseAsset },
                    { "@quoteAsset", quoteAsset }
                };

                var result = mySqlDataAccess.ExecuteScalar(cmd, arrParam);

                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int UpdateOrder(Order order)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "UPDATE orders SET" +
                             "  SellQuantity = @sellQuantity, SellPrice = @sellPrice, SellStatus = @sellStatus, SellStatusDate = @sellStatusDate, SellClientOrderId = @sellClientOrderId, SellIncDate = @sellIncDate, QuoteAssetPriceAtSell = @quoteAssetPriceAtSell" +
                             " WHERE ID = @id";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@sellQuantity", order.SellQuantity },
                    { "@sellPrice", order.SellPrice },
                    { "@sellStatus", order.SellStatus },
                    { "@sellStatusDate", order.SellStatusDate },
                    { "@sellClientOrderId", order.SellClientOrderId },
                    { "@sellIncDate", order.SellIncDate },
                    { "@quoteAssetPriceAtSell", order.QuoteAssetPriceAtSell },
                    { "@id", order.Id }
                };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int CreateOrder(Order order)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "INSERT INTO orders (BaseAsset, QuoteAsset, AssetPrecision, BaseAssetStepSize, BaseAssetMinNotional, BaseAssetMinQuantity, BaseAssetMaxQuantity, BuyQuantity, BuyPrice, BuyStatus, BuyStatusDate, BuyClientOrderId, BuyIncDate, QuoteAssetPriceAtBuy, SellQuantity, SellPrice, MinimumSellPrice) VALUES (@baseAsset, @quoteAsset, @assetPrecision, @baseAssetStepSize, @baseAssetMinNotional, @baseAssetMinQuantity, @baseAssetMaxQuantity, @buyQuantity, @buyPrice, @buyStatus, @buyStatusDate, @buyClientOrderId, @buyIncDate, @quoteAssetPriceAtBuy, @sellQuantity, @sellPrice, @minimumSellPrice)";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@baseAsset", order.BaseAsset },
                    { "@quoteAsset", order.QuoteAsset },
                    { "@assetPrecision", order.AssetPrecision },
                    { "@baseAssetStepSize", order.BaseAssetStepSize },
                    { "@baseAssetMinNotional", order.BaseAssetMinNotional },
                    { "@baseAssetMinQuantity", order.BaseAssetMinQuantity },
                    { "@baseAssetMaxQuantity", order.BaseAssetMaxQuantity },
                    { "@buyQuantity", order.BuyQuantity },
                    { "@buyPrice", order.BuyPrice },
                    { "@buyStatus", order.BuyStatus },
                    { "@buyStatusDate", order.BuyStatusDate },
                    { "@buyClientOrderId", order.BuyClientOrderId },
                    { "@buyIncDate", order.BuyIncDate },
                    { "@quoteAssetPriceAtBuy", order.QuoteAssetPriceAtBuy },
                    { "@sellQuantity", order.SellQuantity },
                    { "@sellPrice", order.SellPrice },
                    { "@minimumSellPrice", order.MinimumSellPrice }
                };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int UpdateOrderStatus(string clientOrderId, string side, string status, decimal lastPrice, decimal quoteAssetPriceInDollars)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "UPDATE orders SET" +
                             (side.ToUpper() == "BUY" ? " BuyStatus" : " SellStatus") + " = @status" +
                             ", " + (side.ToUpper() == "BUY" ? " BuyStatusDate" : " SellStatusDate") + " = @statusDate" +
                             ", LastPrice = @lastPrice" +
                             ", LastPriceDate = @lastPriceDate";

                if (status.ToUpper() == "FILLED")
                {
                    cmd += ", " + (side.ToUpper() == "BUY" ? " QuoteAssetPriceAtBuy" : " QuoteAssetPriceAtSell") + " = @quoteAssetPriceInDollars";
                }

                cmd += " WHERE " + (side.ToUpper() == "BUY" ? " BuyClientOrderId" : " SellClientOrderId") + " = @clientOrderId;";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@status", status },
                    { "@statusDate", DateTime.Now },
                    { "@lastPrice", lastPrice },
                    { "@lastPriceDate", DateTime.Now },
                    { "@clientOrderId", clientOrderId },
                    { "@quoteAssetPriceInDollars", quoteAssetPriceInDollars }
                };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int UpdateLastPrice(string clientOrderId, string side, decimal lastPrice)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "UPDATE orders SET" +
                             "  LastPrice = @lastPrice" +
                             ", LastPriceDate = @lastPriceDate" +
                             " WHERE " + (side.ToUpper() == "BUY" ? " BuyClientOrderId" : " SellClientOrderId") + " = @clientOrderId;";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@lastPrice", lastPrice },
                    { "@lastPriceDate", DateTime.Now },
                    { "@clientOrderId", clientOrderId }
                };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Order> GetOpenOrders()
        {
            MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();

            try
            {
                List<Order> orders = new List<Order>();
                string cmd = " SELECT * FROM orders WHERE (BuyStatus IN('NEW', 'PARTIALLY_FILLED') OR SellStatus IN ('NEW', 'PARTIALLY_FILLED', 'PENDING_CREATION'))";

                var result = mySqlDataAccess.ExecuteReader(cmd, null);

                while (result.Read())
                {
                    var order = new Order();

                    order.Id = result.GetInt32("Id");
                    order.BaseAsset = result.GetString("BaseAsset");
                    order.QuoteAsset = result.GetString("QuoteAsset");
                    order.AssetPrecision = result.GetInt16("AssetPrecision");
                    order.BaseAssetStepSize = result.GetDecimal("BaseAssetStepSize");
                    order.BaseAssetMinNotional = result.GetDecimal("BaseAssetMinNotional");
                    order.BaseAssetMinQuantity = result.GetDecimal("BaseAssetMinQuantity");
                    order.BaseAssetMaxQuantity = result.GetDecimal("BaseAssetMaxQuantity");
                    order.BuyQuantity = result.GetDecimal("BuyQuantity");
                    order.BuyPrice = result.GetDecimal("BuyPrice");
                    order.BuyStatus = result.GetString("BuyStatus");
                    order.BuyStatusDate = result.GetDateTime("BuyStatusDate");
                    order.BuyClientOrderId = result.GetString("BuyClientOrderId");
                    order.BuyIncDate = result.GetDateTime("BuyIncDate");
                    order.QuoteAssetPriceAtBuy = result.GetDecimal("QuoteAssetPriceAtBuy");
                    order.SellQuantity = result.GetDecimal("SellQuantity");
                    order.SellPrice = result.GetDecimal("SellPrice");
                    order.MinimumSellPrice = result.GetDecimal("MinimumSellPrice");
                    order.SellStatus = (result.IsDBNull(result.GetOrdinal("SellStatus"))) ? null : result.GetString("SellStatus");

                    if (!result.IsDBNull(result.GetOrdinal("SellStatusDate")))
                        order.SellStatusDate = result.GetDateTime("SellStatusDate");
                    else
                        order.SellStatusDate = null;

                    order.SellClientOrderId = (result.IsDBNull(result.GetOrdinal("SellClientOrderId"))) ? null : result.GetString("SellClientOrderId");

                    if (!result.IsDBNull(result.GetOrdinal("SellIncDate")))
                        order.SellIncDate = result.GetDateTime("SellIncDate");
                    else
                        order.SellIncDate = null;

                    if (!result.IsDBNull(result.GetOrdinal("QuoteAssetPriceAtSell")))
                        order.QuoteAssetPriceAtSell = result.GetDecimal("QuoteAssetPriceAtSell");
                    else
                        order.QuoteAssetPriceAtSell = null;

                    if (!result.IsDBNull(result.GetOrdinal("LastPrice")))
                        order.LastPrice = result.GetDecimal("LastPrice");
                    else
                        order.LastPrice = null;

                    if (!result.IsDBNull(result.GetOrdinal("LastPriceDate")))
                        order.LastPriceDate = result.GetDateTime("LastPriceDate");
                    else
                        order.LastPriceDate = null;

                    orders.Add(order);
                }

                mySqlDataAccess.CloseConnection();

                return orders;
            }
            catch (Exception ex)
            {
                mySqlDataAccess.CloseConnection();

                throw ex;
            }
        }

        public int UpdateSellOrderWithPendingStatus(int orderId)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "UPDATE orders SET" +
                             "  SellStatus = 'PENDING_CREATION', SellStatusDate = NOW()" +
                             " WHERE ID = " + orderId.ToString();

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, null);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
