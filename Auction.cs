using System;

namespace Auction.mod
{
    public class Auction
    {
        public enum OfferType {
            BUY,SELL
        }
        public readonly string seller;
        public readonly DateTime time;
        public readonly OfferType offer;
        public readonly Card card;
        /// <summary>
        /// The price. Negative value indicates unknown.
        /// </summary>
        public readonly int price;
        /// <summary>
        /// The amount offered. Negative value indicates unknown.
        /// </summary>
        public readonly int amountOffered;
        public Auction (String seller, DateTime time, OfferType offer, Card c) : this(seller, time, offer, c, -1) {
        }
        public Auction (String seller, DateTime time, OfferType offer, Card c, int price) : this (seller, time, offer, c, price, -1) {
        }
        public Auction (String seller, DateTime time, OfferType offer, Card card, int price, int amountOffered)
        {
            this.seller = seller;
            this.time = time;
            this.offer = offer;
            this.card = card;
            this.price = price;
            this.amountOffered = amountOffered;

        }
    }
}

