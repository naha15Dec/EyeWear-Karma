using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BanGiay.Models;
using System.ComponentModel.DataAnnotations;
namespace BanGiay.ViewModel
{
    public class Cart
    {
        public string IDProduct { set; get; }
        public string NameProduct { set; get; }
        public string ImageProduct { set; get; }
        public int Price { set; get; }
        public int Discount { set; get; }
        public int Quantity { set; get; }
        public int Total { set; get; }
    }
}