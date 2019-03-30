using System;

namespace TradingAnalytics.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }
        public int AssetPrecision { get; set; }
        public decimal BaseAssetStepSize { get; set; }
        public decimal BaseAssetMinNotional { get; set; }
        public decimal BaseAssetMinQuantity { get; set; }
        public decimal BaseAssetMaxQuantity { get; set; }
        public decimal BuyQuantity { get; set; }
        public decimal BuyPrice { get; set; }
        public string BuyStatus { get; set; }
        public DateTime BuyStatusDate { get; set; }
        public string BuyClientOrderId { get; set; }
        public DateTime BuyIncDate { get; set; }
        public decimal QuoteAssetPriceAtBuy { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal SellPrice { get; set; }
        public decimal MinimumSellPrice { get; set; }
        public string SellStatus { get; set; }
        public DateTime? SellStatusDate { get; set; }
        public string SellClientOrderId { get; set; }
        public DateTime? SellIncDate { get; set; }
        public decimal? QuoteAssetPriceAtSell { get; set; }
        public decimal? LastPrice { get; set; }
        public DateTime? LastPriceDate { get; set; }
    }
}
