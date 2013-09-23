using System;
using System.Collections.Generic;
namespace Auction.mod
{
    public class AuctionEqualityComparer :IEqualityComparer<Auction>
    {
        public AuctionEqualityComparer () {
        }

        public bool Equals(Auction a1, Auction a2)
        {
            return a1.offer.Equals(a2.offer) && a1.price.Equals (a2.price) && a1.seller.Equals (a2.seller) && a1.card.getCardType ().id.Equals (a2.card.getCardType ().id);
        }


        public int GetHashCode(Auction a)
        {
            int hCode = (((251*a.seller.GetHashCode()) + a.card.getCardType().id)*251 + a.price)*251 + a.offer.GetHashCode();
            return hCode.GetHashCode();
        }
    }
}

