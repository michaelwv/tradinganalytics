using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingAnalytics.Application.DTO
{
    public class TradeOpportunityDTO
    {
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal BaseAssetPriceInUsd { get; set; }
    }
}