namespace MatKinh.ViewModel
{
    public class ProductCatalogFilterVm
    {
        // Danh mục hiện tại (context)
        public int? CategoryId { get; set; }

        // Bộ lọc
        public string Keyword { get; set; }
        public int? BrandId { get; set; }
        public string PriceRange { get; set; }

        // Phân trang
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
    }
}