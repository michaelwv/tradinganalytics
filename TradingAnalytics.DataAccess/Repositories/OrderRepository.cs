using System;
using System.Collections.Generic;
using TradingAnalytics.Domain.Entities;

namespace TradingAnalytics.DataAccess
{
    public class OrderRepository
    {
        public bool OrderExists(string baseAsset, string quoteAsset, string buyStatus, string sellStatus)
        {
            try
            {
                MySqlDataAccess mySqlDataAccess = new MySqlDataAccess();
                string cmd = "SELECT COUNT(*) FROM orders WHERE BaseAsset = @baseAsset AND QuoteAsset = @quoteAsset AND BuyStatus = @buyStatus AND SellStatus = @sellStatus";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
                {
                    { "@baseAsset", baseAsset },
                    { "@quoteAsset", quoteAsset },
                    { "@buyStatus", buyStatus },
                    { "@sellStatus", sellStatus }
                };

                var result = mySqlDataAccess.ExecuteScalar(cmd, arrParam);

                return Convert.ToInt32(result) > 0;
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
                string cmd = "INSERT INTO orders (BaseAsset, QuoteAsset, Quantity, BuyPrice, BuyStatus, BuyClientOrderId, BuyIncDate, SellPrice) VALUES (@baseAsset, @quoteAsset, @quantity, @buyPrice, @buyStatus, @buyClientOrderId, @buyIncDate, @sellPrice)";

                Dictionary<string, object> arrParam = new Dictionary<string, object>()
            {
                { "@baseAsset", order.BaseAsset },
                { "@quoteAsset", order.QuoteAsset },
                { "@quantity", order.Quantity },
                { "@buyPrice", order.BuyPrice },
                { "@buyStatus", order.BuyStatus },
                { "@buyClientOrderId", order.BuyClientOrderId },
                { "@buyIncDate", order.BuyIncDate },
                { "@sellPrice", order.SellPrice }
            };

                int result = mySqlDataAccess.ExecuteNonQuery(cmd, arrParam);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
