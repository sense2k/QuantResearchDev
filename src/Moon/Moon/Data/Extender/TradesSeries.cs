﻿using Binance.Net.Objects;
using Moon.Data.Model;
using Moon.Data.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moon.Data.Extender
{
    public enum TradesCollectionMode
    {
        Consolidated,
        AllTicks
    }
    public interface ITradeFactory
    {
        TradesCollectionMode Mode { get; set; }
    }

    public class TradesSeries : ITradeFactory
    {
        public int Index
        {
            get; set;
        }
        private int index = 0;
        public List<BinanceOrderBookEntry> Asks_BinanceStreamOrderBook { get; set; } = new List<BinanceOrderBookEntry>();
        public List<BinanceOrderBookEntry> Bids_BinanceStreamOrderBook { get; set; } = new List<BinanceOrderBookEntry>();
        public List<BinanceStreamTrade> Asks_Trades { get; set; } = new List<BinanceStreamTrade>();
        public List<BinanceStreamTrade> Bids_Trades { get; set; } = new List<BinanceStreamTrade>();

        public int overallorderparsed = 0;
        public Dictionary<string, int> MostActiveAskZone { get; set; } = new Dictionary<string, int>();
        public TradesCollectionMode Mode { get; set; } = TradesCollectionMode.Consolidated;
        public TradesSeries()
        {
            this.MostActiveAskZone.Add(0.ToString(), 0);
            this.MostActiveAskZone.Add(1.ToString(), 0);
            this.MostActiveAskZone.Add(2.ToString(), 0);
            this.MostActiveAskZone.Add(3.ToString(), 0);
            this.MostActiveAskZone.Add(4.ToString(), 0);
            this.MostActiveAskZone.Add(5.ToString(), 0);
            this.MostActiveAskZone.Add(6.ToString(), 0);
            this.MostActiveAskZone.Add(7.ToString(), 0);
            this.MostActiveAskZone.Add(8.ToString(), 0);
            this.MostActiveAskZone.Add(9.ToString(), 0);

        }
        public void ConnectBinance(BinanceProvier Source)
        {
            Source.BBookData.CollectionChanged += BBookData_CollectionChanged;
            Source.BDataTradeBuyer.CollectionChanged += BDataTradeBuyer_CollectionChanged;
            Source.BDataTradeSeller.CollectionChanged += BDataTradeSeller_CollectionChanged;
        }

        private void BDataTradeSeller_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var Seller = (BinanceStreamTrade)e.NewItems[0];
                this.Asks_Trades.Add(Seller);
                //Console.WriteLine("Seller filled Moving Average Price : {0}", this.Asks_Trades.Select((y) => y.Price).ToList().Mean());
                //Console.WriteLine("Seller filled Moving Average Quantity : {0}", this.Asks_Trades.Select((y) => y.Quantity).ToList().Mean());
                //Console.WriteLine("Seller filled Max Quantity : {0}", this.Asks_Trades.Select((y) => y.Quantity).ToList().Max());
                if(this.Asks_Trades.Count() > 13)
                {
                    var sellerrsiq = this.Asks_Trades.Select((y) => y.Quantity.ChangeType<double>()).ToList().GetLastRSI();
                    var sellerrsi = this.Asks_Trades.Select((y) => y.Price.ChangeType<double>()).ToList().GetLastRSI();

                    Console.WriteLine("Seller - Quantity - GetLastRSI : {0}", sellerrsiq.Last());
                    Console.WriteLine("Seller - Price -  GetLastRSI : {0}", sellerrsi.Last());

                }
            }
        }

        private void BDataTradeBuyer_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var Buyer = (BinanceStreamTrade)e.NewItems[0];
                this.Bids_Trades.Add(Buyer);
                //Console.WriteLine("Buyer filled Moving Average Price : {0}", this.Bids_Trades.Select((y) => y.Price).ToList().Mean());
                //Console.WriteLine("Buyer filled Moving Average Quantity : {0}", this.Bids_Trades.Select((y) => y.Quantity).ToList().Mean());
                //Console.WriteLine("Buyer filled Max Quantity : {0}", this.Bids_Trades.Select((y) => y.Quantity).ToList().Max());
                if (this.Bids_Trades.Count() > 13)
                {
                    var buyerrsiq = this.Bids_Trades.Select((y) => y.Quantity.ChangeType<double>()).ToList().GetLastRSI();
                    var buyerrsi = this.Bids_Trades.Select((y) => y.Price.ChangeType<double>()).ToList().GetLastRSI();

                    Console.WriteLine("Buyer - Quantity - GetLastRSI : {0}", buyerrsiq.Last());
                    Console.WriteLine("Buyer - Price -  GetLastRSI : {0}", buyerrsi.Last());

                }

            }
        }

        private void BBookData_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var NIT = (BinanceStreamOrderBook)e.NewItems[0];
                overallorderparsed++;
                index++;
                if (this.Asks_BinanceStreamOrderBook.Count() > 0 || this.Bids_BinanceStreamOrderBook.Count() > 0)
                {
                    for (int i = 0; i < this.Asks_BinanceStreamOrderBook.Count(); i++)
                    {
                        var correspondingBookAskLine = this.Asks_BinanceStreamOrderBook[i];
                        var correspondingBookBidLine = this.Bids_BinanceStreamOrderBook[i];
                       
                        var AskLine = NIT.Asks[i];
                        var BidLine = NIT.Bids[i];


                        switch (AskLine.CompareWith(correspondingBookAskLine))
                        {
                            case List.OrderBookUpdateType.PriceChange:

                                //Console.WriteLine("Ask line {0} - Price change : {1}",i, AskLine.GetPriceChange(correspondingBookAskLine).ToString("P"));
                                this.Asks_BinanceStreamOrderBook[i] = AskLine;
                                break;
                            case List.OrderBookUpdateType.QuantityChange:
                                MostActiveAskZone[i.ToString()] += 1;
                                var getPeaker = int.Parse(MostActiveAskZone.Where(y => y.Value == MostActiveAskZone.Values.Max()).Select(y => y.Key).FirstOrDefault());
                                var valuepeak = MostActiveAskZone.Values.ToArray()[getPeaker];
                                //Console.WriteLine("Ask - Most Active Layer : {0} at rate : {1} with price {2}", getPeaker,valuepeak, correspondingBookAskLine.Price);
                                //Console.WriteLine("Ask line {0} - Quantity change : from {1} to {2}", i, correspondingBookAskLine.Quantity, AskLine.Quantity);
                                //Console.WriteLine("Ask line {0} - Quantity change : {1}",i, AskLine.GetQuantityChange(correspondingBookAskLine).ToString("P"));
                                this.Asks_BinanceStreamOrderBook[i] = AskLine;
                                break;
                            case List.OrderBookUpdateType.NoChange:
                                //Console.WriteLine("No movement on AskLine {0} : {1} : at quant {2}",i, AskLine.Price, AskLine.Quantity);
                                break;
                        }

                        switch (BidLine.CompareWith(correspondingBookBidLine))
                        {
                            case List.OrderBookUpdateType.PriceChange:
                                this.Bids_BinanceStreamOrderBook[i] = AskLine;
                                break;
                            case List.OrderBookUpdateType.QuantityChange:
                                this.Bids_BinanceStreamOrderBook[i] = AskLine;
                                break;
                            case List.OrderBookUpdateType.NoChange:
                                //Console.WriteLine("No movement on BidLine {0} : {0} : at quant {1}", i, BidLine.Price, BidLine.Quantity);
                                break;
                        }

                        //if (AskLine.Quantity != correspondingBookAskLine.Quantity || AskLine.Price != correspondingBookAskLine.Price)
                        //{
                        //    this.Asks_BinanceStreamOrderBook[i] = AskLine;
                        //    Console.WriteLine("TradesSeries - Updating Askline : {0} - with P : {1} and Q :{2}", i, AskLine.Price, AskLine.Quantity);

                        //}
                        //if (BidLine.Quantity != correspondingBookBidLine.Quantity || BidLine.Price != correspondingBookBidLine.Price)
                        //{
                        //    Console.WriteLine("TradesSeries - Updating Bidline : {0} - with P : {1} and Q :{2}", i, BidLine.Price, BidLine.Quantity);
                        //    this.Bids_BinanceStreamOrderBook[i] = BidLine;

                        //}
                    }
                }
                else
                {
                    NIT.Asks.ForEach(y =>
                    {
                        this.Asks_BinanceStreamOrderBook.Add(y);
                    });
                    NIT.Bids.ForEach(y =>
                    {
                        this.Bids_BinanceStreamOrderBook.Add(y);
                    });

                }



            }
        }



    }
}