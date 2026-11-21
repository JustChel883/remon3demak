using System.Windows;
using System.Windows.Media.Imaging;

namespace remon
{
    public class ProductDisplay
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string Supplier { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public double DiscountValue { get; set; }
        public string DiscountText { get; set; }
        public bool IsHighDiscount { get; set; }
        public bool CanDelete { get; set; }
        public BitmapImage Image { get; set; }
        public string OldPriceText { get; set; }
        public string FinalPriceText { get; set; }
        public Visibility OldPriceVisibility { get; set; }
    }
}