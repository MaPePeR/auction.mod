using System;
using System.Collections.Generic;
namespace Auction.mod
{
    public class AuctionFilter
    {
        public AuctionFilter ()
        {
            filtersChanged = false;
        }
        public bool filtersChanged { get; private set;}

        public bool isFiltered(Auction a) {
            return isBeyondPriceRange(a) || isFilteredByCardFilter (a) || isIgnoredSellerName (a);
        }

        #region PriceFilter
        int priceUpperBound = -1;
        int priceLowerBound = -1;
        public void setPriceUpperBound(string upper) {
            int parsed;
            if (!Int32.TryParse(upper,out parsed) || parsed < 0) {
                parsed = -1;
            }
            if (parsed != this.priceUpperBound) {
                this.priceUpperBound = parsed;
                filtersChanged = true;
            }
        }
        public void setPriceLowerBound(string lower) {
            int parsed;
            if (!Int32.TryParse(lower,out parsed) || parsed < 0) {
                parsed = -1;
            }
            if (parsed != this.priceLowerBound) {
                this.priceLowerBound = parsed;
                filtersChanged = true;
            }
        }
        public bool isBeyondPriceRange(Auction a) {
            if (priceLowerBound >= 0 && priceUpperBound >= 0) {
                return a.price < priceLowerBound || priceUpperBound < a.price;
            } else if (priceLowerBound >= 0) {
                return a.price < priceLowerBound; //Price is lower than lower Bound
            } else if (priceUpperBound >= 0) {
                return priceUpperBound < a.price; //price is higher than upper bound
            } else {
                return true;
            }
        }
        #endregion

        #region IgnoredSellersFilter
        List<string> ignoredSellers = new List<string>();
        String ignoredSellersString = "";
        public void setIgnoredSellers(String ignoredSellersString) {
            if (!this.ignoredSellersString.Equals (ignoredSellersString)) {
                ignoredSellers.Clear ();
                string[] s = ignoredSellersString.Split (new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(string seller in s) {
                    ignoredSellers.Add (seller.ToLower ());
                }
                filtersChanged = true;
            }
        }
        private bool isIgnoredSellerName(Auction a) {
            return ignoredSellers.Contains (a.seller.ToLower ());
        }
        #endregion

        #region CardFilter
        CardFilter cardFilter = new CardFilter("");
        string cardFilterString = "";
        public void setCardFilter(string cardFilterString) {
            if (!this.cardFilterString.Equals (cardFilterString)) {
                cardFilter = new CardFilter (cardFilterString);
                filtersChanged = true;
            }
        }
        private bool isFilteredByCardFilter(Auction a) {
            return !cardFilter.isIncluded (a.card);
        }
        #endregion
    }
}

