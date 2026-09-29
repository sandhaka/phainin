using System.Runtime.CompilerServices;
using ACMEMarketData.Infrastructure.Components.Participant;
using ACMEMarketData.Infrastructure.Components.Venue.Book;

[assembly: InternalsVisibleTo("ACMEMarketData.Tests")]

namespace ACMEMarketData.Infrastructure.Components.Venue.Matching;

internal sealed class MatchingEngine
{
    private readonly OrderBook _orderBook = new();

    public bool TryGetPrices(string assetCode, out PriceLevels? prices) =>
        _orderBook.TryGetPrices(assetCode, out prices);
    
    public void ProcessOrder(Order order)
    {
        if (order.ActionType == ParticipantActionType.Sell)
            HandleSellOrder(order);
        if (order.ActionType == ParticipantActionType.Buy)
            HandleBuyOrder(order);
    }

    private void HandleBuyOrder(Order buy)
    {
        var reg = _orderBook.GetOrAdd(buy.Instrument, () => new PriceLevels());

        while (!buy.FulFilled)
        {
            if (reg.TryGetBestAsk(out var ask))
            {
                // Matching asking price
                if (buy.Price > ask!.Price)
                {
                    int tradeQuantity = 0;

                    if (ask.RemainingQuantity <= buy.RemainingQuantity)
                    {
                        // Trades all the bid quantity, order not fulfilled
                        tradeQuantity = ask.RemainingQuantity;
                        buy.RemainingQuantity -= buy.RemainingQuantity;
                        
                        // Sell Order in the book closed
                        ask.RemainingQuantity = 0;
                        
                        // Remove from the book
                        _orderBook.RemoveSell(ask);
                    }
                    else
                    {
                        // Incoming order fulfilled
                        tradeQuantity = buy.RemainingQuantity;
                        ask.RemainingQuantity -= buy.RemainingQuantity;
                        
                        // Order closed
                        buy.RemainingQuantity = 0;
                    }

                    var trade = new Trade
                    {
                        Asset = buy.Instrument,
                        Price = ask.Price,
                        Quantity = tradeQuantity
                    };
                    
                    // TODO: trade emit
                    Console.WriteLine(trade);
                }
                else
                {
                    // There's no match, insert the order in the book
                    _orderBook.AddBuy(buy);
                    break;
                }
            }
            else
            {
                // There's no match, insert the order in the book
                _orderBook.AddBuy(buy);
                break;
            }
        }
    }

    private void HandleSellOrder(Order sell)
    {
        var reg = _orderBook.GetOrAdd(sell.Instrument, () => new PriceLevels());
        
        while (!sell.FulFilled)
        {
            // Match search
            if (reg.TryGetBestBid(out var bid))
            {
                // Matching asking price
                if (sell.Price <= bid!.Price)
                {
                    // Match found make a trade
                    int tradeQuantity = 0;

                    if (bid.RemainingQuantity <= sell.RemainingQuantity)
                    {
                        // Trades all the bid quantity, order not fulfilled
                        tradeQuantity = bid.RemainingQuantity;
                        sell.RemainingQuantity -= bid.RemainingQuantity;
                        
                        // Buy Order in the book closed
                        bid.RemainingQuantity = 0;
                        
                        // Remove from the book
                        _orderBook.RemoveBuy(bid);
                    }
                    else
                    {
                        // Incoming order fulfilled
                        tradeQuantity = sell.RemainingQuantity;
                        bid.RemainingQuantity -= sell.RemainingQuantity;
                        
                        // Order closed
                        sell.RemainingQuantity = 0;
                    }

                    var trade = new Trade
                    {
                        Asset = sell.Instrument,
                        Price = bid.Price,
                        Quantity = tradeQuantity
                    };
                    
                    // TODO: trade emit
                    Console.WriteLine(trade);
                }
                else
                {
                    // There's no match, insert the order in the book
                    _orderBook.AddSell(sell);
                    break;
                }
            }
            else
            {
                // Order book empty on buy side
                _orderBook.AddSell(sell);
                break;
            }
        }
    }
}