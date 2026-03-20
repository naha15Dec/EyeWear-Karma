using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MatKinh.Models.DTO
{
    public class ProductResponse
    {
        [JsonProperty("san_pham_goi_y")]
        public List<string> SanPhamGoiY { get; set; }
    }
}