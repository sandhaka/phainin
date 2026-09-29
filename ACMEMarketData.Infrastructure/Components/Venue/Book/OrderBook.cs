using System.Runtime.CompilerServices;
using ACMEMarketData.Infrastructure.Components.Participant;

[assembly: InternalsVisibleTo("ACMEMarketData.Tests")]

namespace ACMEMarketData.Infrastructure.Components.Venue.Book;

internal class PriceLevels
{
    // Keep Sell and Buy orders per asset,
    // order with the same price will be processed by arrival time
    public readonly SortedList<decimal, List<Order>> Buy = new();
    public readonly SortedList<decimal, List<Order>> Sell = new();

    public bool TryGetBestBid(out Order? order)
    {
        order = null;
        if (Buy.Count == 0)
            return false;
        var bestBuy = Buy.Keys.Max();
        order = Buy[bestBuy].First();
        return true;
    }
    
    public bool TryGetBestAsk(out Order? order)
    {
        order = null;
        if (Sell.Count == 0)
            return false;
        var bestSell = Sell.Keys.Min(); 
        order = Sell[bestSell].First();
        return true;
    }
}

internal class OrderBook
{
    private readonly Dictionary<string, PriceLevels> _internalOrderBookData = new();

    public PriceLevels GetOrAdd(string assetCode, Func<PriceLevels> factory)
    {
        if (!_internalOrderBookData.ContainsKey(assetCode))
            _internalOrderBookData.Add(assetCode, factory.Invoke());
        return _internalOrderBookData[assetCode];
    }
    
    public void AddBuy(Order order)
    {
        var prices = GetOrAdd(order.Instrument, () => new PriceLevels());
        
        if (!prices.Buy.TryGetValue(order.Price, out List<Order>? buyOrders))
            prices.Buy.Add(order.Price, new List<Order>([order]));
        else
            buyOrders.Add(order);
    }

    public void RemoveBuy(Order order)
    {
        if (!_internalOrderBookData.ContainsKey(order.Instrument) || !_internalOrderBookData[order.Instrument].Buy.ContainsKey(order.Price))
            return;
        _internalOrderBookData[order.Instrument].Buy[order.Price].Remove(order);
    }

    public void AddSell(Order order)
    {
        var prices = GetOrAdd(order.Instrument, () => new PriceLevels());
        
        if (!prices.Sell.TryGetValue(order.Price, out List<Order>? sellOrders))
            prices.Sell.Add(order.Price, new List<Order>([order]));
        else
            sellOrders.Add(order);
    }

    public void RemoveSell(Order order)
    {
        if(!_internalOrderBookData.ContainsKey(order.Instrument) || !_internalOrderBookData[order.Instrument].Sell.ContainsKey(order.Price))
            return;
        _internalOrderBookData[order.Instrument].Sell[order.Price].Remove(order);
    }
    
    internal bool TryGetPrices(string assetCode, out PriceLevels? pl)
    {
        if (_internalOrderBookData.TryGetValue(assetCode, out var res))
        {
            pl = res;
            return true;
        }

        pl = null;
        return false;
    }
}