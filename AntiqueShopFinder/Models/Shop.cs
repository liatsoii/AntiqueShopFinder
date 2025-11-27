using Stripe;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace AntiqueShopFinder.Models
{
    public class Shop
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }
        public string ShopType { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        public List<Review> Reviews { get; set; } = new List<Review>();
    }
}