using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Auction.mod
{
    public class AuctionHouse
    {
        public enum SortMode {
            CARD,SELLER,TIME,PRICE
        }
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
        //protected List<Auction> fullList = new List<Auction>();
        protected List<Auction> fullSellOfferList = new List<Auction>();
        protected List<Auction> fullBuyOfferList = new List<Auction>();
        protected List<Auction> sellOfferListFiltered = new List<Auction>();
        protected List<Auction> buyOfferListFiltered = new List<Auction>();
        protected SortMode sortMode = SortMode.TIME;
        /// <summary>
        /// Gets a value indicating whether this <see cref="Auction.mod.AuctionHouse"/> has unseen sell offers.
        /// </summary>
        /// <value><c>true</c> if new sell offers where added; otherwise, <c>false</c>.</value>
        public bool newSellOffers { get; private set;}
        /// <summary>
        /// Gets a value indicating whether this <see cref="Auction.mod.AuctionHouse"/> new unseen buy offers.
        /// </summary>
        /// <value><c>true</c> if new buy offers where added; otherwise, <c>false</c>.</value>
        public bool newBuyOffers { get; private set;}

        public ReadOnlyCollection<Auction> getBuyOffers() {
            newBuyOffers = false;
            if (buyOfferFilter.filtersChanged) {
                buyOfferListFiltered = new List<Auction> (fullBuyOfferList);
                buyOfferListFiltered.RemoveAll (buyOfferFilter.isFiltered);
            }
            return buyOfferListFiltered.AsReadOnly ();
        }

        public ReadOnlyCollection<Auction> getSellOffers() {
            newSellOffers = false;
            if (sellOfferFilter.filtersChanged) {
                sellOfferListFiltered = new List<Auction> (fullSellOfferList);
                sellOfferListFiltered.RemoveAll (sellOfferFilter.isFiltered);
            }
            return buyOfferListFiltered.AsReadOnly ();
        }

        private void addAuction(Auction a) {
            //fullList.Add (a);
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
        public void setSortMode(SortMode sortMode) {
            this.sortMode = sortMode;
        }
        public void addAuctions(List<Auction> list) {
            list.ForEach (addAuction);
            fullBuyOfferList.Sort (Auction.getComparison(sortMode));
            buyOfferListFiltered.Sort (Auction.getComparison(sortMode));
            fullSellOfferList.Sort (Auction.getComparison(sortMode));
            sellOfferListFiltered.Sort (Auction.getComparison(sortMode));

        }
    }
}

