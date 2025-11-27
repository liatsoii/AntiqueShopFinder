using System;

namespace AntiqueShopFinder.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ShopId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }
}