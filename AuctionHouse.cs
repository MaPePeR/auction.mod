using System;
using System.Collections.Generic;
namespace Auction.mod
{
    public class AuctionHouse
    {
        #region Singleton-Pattern
        private static readonly AuctionHouse instance = new AuctionHouse();

        public static AuctionHouse Instance
        {
            get 
            {
                return instance; 
            }
        }
        #endregion
        private AuctionHouse () {
        }

        public readonly AuctionFilter sellOfferFilter = new AuctionFilter();
        public readonly AuctionFilter buyOfferFilter = new AuctionFilter();
        protected List<Auction> fullList = new List<Auction>();
        protected List<Auction> fullSellOfferList = new List<Auction>();
        protected List<Auction> fullBuyOfferList = new List<Auction>();
        protected List<Auction> sellOfferListFiltered = new List<Auction>();
        protected List<Auction> buyOfferListFiltered = new List<Auction>();
        protected bool filtersChanged = false;

        public void addAuction(Auction a) {
            fullList.Add (a);
            if (a.offer == Auction.OfferType.BUY) {
                fullBuyOfferList.Add (a);
                if (!buyOfferFilter.isFiltered(a)) {
                    buyOfferListFiltered.Add (a);
                }
            } else if (a.offer == Auction.OfferType.SELL) {
                fullSellOfferList.Add (a);
                if (!sellOfferFilter.isFiltered(a)) {
                    sellOfferListFiltered.Add (a);
                }
            }
        }
        public void addAuctions(List<Auction> list) {
            list.ForEach (addAuction);
        }
    }
}

