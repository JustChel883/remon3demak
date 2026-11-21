using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using remon.models;

namespace remon
{
    public partial class MainWindow : Window
    {
        private users currentUser;
        private List<ProductDisplay> allProducts = new List<ProductDisplay>();
        private bool isEditOpen = false;

        public MainWindow(users user)
        {
            InitializeComponent();
            currentUser = user;
            SetupUI();
            LoadManufacturers();
            LoadProducts();
        }

        private void SetupUI()
        {
            if (currentUser == null)
            {
                UserFioTb.Text = "Гость";
                FilterPanel.Visibility = Visibility.Collapsed;
                return;
            }
            UserFioTb.Text = currentUser.Surname + " " + currentUser.Name + " " + currentUser.Patronymic;
            if (currentUser.id_role == 1 || currentUser.id_role == 2)
            {
                FilterPanel.Visibility = Visibility.Visible;
            }
            if (currentUser.id_role == 1)
            {
                AddProductBtn.Visibility = Visibility.Visible;
            }
        }

        private BitmapImage LoadImage(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = new Uri(path);
            img.EndInit();
            img.Freeze();
            return img;
        }

        private void LoadManufacturers()
        {
            try
            {
                using (var db = new remonEntities1())
                {
                    var list = db.manufactor.Select(m => m.name).ToList();
                    list.Insert(0, "Все производители");
                    ManufacturerCb.ItemsSource = list;
                    ManufacturerCb.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void LoadProducts()
        {
            try
            {
                string folder = AppDomain.CurrentDomain.BaseDirectory;
                using (var db = new remonEntities1())
                {
                    var productsFromDb = db.products
                        .Include("id_category")
                        .Include("manufactor")
                        .Include("supplement")
                        .Include("measurments")
                        .ToList();
                    allProducts.Clear();
                    foreach (var p in productsFromDb)
                    {
                        string discStr = "Нет скидки";
                        bool high = false;
                        double discVal = 0;
                        if (p.discount.HasValue && p.discount > 0)
                        {
                            discVal = p.discount.Value * 100;
                            discStr = "Скидка:\n" + discVal.ToString("0") + "%";
                            if (discVal > 15)
                            {
                                high = true;
                            }
                        }

                        string imgPath = Path.Combine(folder, "picture.png");
                        if (!string.IsNullOrWhiteSpace(p.photo))
                        {
                            string fullPath = Path.Combine(folder, p.photo.Replace(",", "."));
                            if (File.Exists(fullPath))
                            {
                                imgPath = fullPath;
                            }
                        }

                        decimal finalPrice = p.price - p.price * (decimal)(p.discount ?? 0);

                        allProducts.Add(new ProductDisplay
                        {
                            Id = p.id_product,
                            Name = p.article,
                            Category = p.id_category != null ? p.id_category.catergory_name : "",
                            Description = p.description,
                            Manufacturer = p.manufactor != null ? p.manufactor.name : "",
                            Supplier = p.supplement != null ? p.supplement.name : "",
                            Price = p.price,
                            Unit = p.measurments != null ? p.measurments.name : "",
                            Quantity = p.quantity,
                            DiscountValue = discVal,
                            DiscountText = discStr,
                            IsHighDiscount = high,
                            CanDelete = currentUser != null && currentUser.id_role == 1,
                            Image = LoadImage(imgPath),
                            OldPriceText = p.price.ToString("0.00") + " руб.",
                            FinalPriceText = finalPrice.ToString("0.00") + " руб.",
                            OldPriceVisibility = (p.discount.HasValue && p.discount > 0) ? Visibility.Visible : Visibility.Collapsed
                        });
                    }
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товаров:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (SearchTb == null || ManufacturerCb == null || SortCb == null || ProductsList == null) return;

            var result = allProducts.AsEnumerable();
            if (ManufacturerCb.SelectedItem != null && ManufacturerCb.SelectedItem.ToString() != "Все производители")
            {
                string manuf = ManufacturerCb.SelectedItem.ToString();
                result = result.Where(p => p.Manufacturer == manuf);
            }
            string search = SearchTb.Text.Trim().ToLower();
            if (search != "")
            {
                result = result.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(search)) ||
                    (p.Category != null && p.Category.ToLower().Contains(search)) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)) ||
                    (p.Manufacturer != null && p.Manufacturer.ToLower().Contains(search)) ||
                    (p.Supplier != null && p.Supplier.ToLower().Contains(search)));
            }
            if (SortCb.SelectedIndex > 0 && SortCb.SelectedItem != null)
            {
                string sort = ((ComboBoxItem)SortCb.SelectedItem).Content.ToString();
                if (sort == "Кол-во (возр.)")
                {
                    result = result.OrderBy(p => p.Quantity);
                }
                else if (sort == "Кол-во (убыв.)")
                {
                    result = result.OrderByDescending(p => p.Quantity);
                }
                else if (sort == "Цена (возр.)")
                {
                    result = result.OrderBy(p => p.Price);
                }
                else if (sort == "Цена (убыв.)")
                {
                    result = result.OrderByDescending(p => p.Price);
                }
                else if (sort == "Скидка (возр.)")
                {
                    result = result.OrderBy(p => p.DiscountValue);
                }
                else if (sort == "Скидка (убыв.)")
                {
                    result = result.OrderByDescending(p => p.DiscountValue);
                }
            }
            ProductsList.ItemsSource = result.ToList();
        }

        private void SearchTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ManufacturerCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SortCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void AddProductBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isEditOpen)
            {
                return;
            }

            isEditOpen = true;
            ProductEditWindow w = new ProductEditWindow(0);
            w.Owner = this;
            w.ShowDialog();
            isEditOpen = false;
            LoadProducts();
        }

        private void Product_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Button || e.OriginalSource is TextBlock)
            {
                return;
            }

            if (currentUser == null || currentUser.id_role != 1)
            {
                return;
            }

            if (isEditOpen)
            {
                return;
            }

            Border border = sender as Border;
            ProductDisplay product = border.DataContext as ProductDisplay;
            if (product == null)
            {
                return;
            }

            isEditOpen = true;
            ProductEditWindow w = new ProductEditWindow(product.Id);
            w.Owner = this;
            w.ShowDialog();
            isEditOpen = false;
            LoadProducts();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            int id = (int)btn.Tag;
            var answer = MessageBox.Show("Удалить товар? Отменить это действие будет нельзя.", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (var db = new remonEntities1())
                {
                    bool inOrders = db.order_items.Any(o => o.id_product == id);
                    if (inOrders)
                    {
                        MessageBox.Show("Этот товар есть в заказах, удалить нельзя!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var prod = db.products.Find(id);
                    if (prod != null)
                    {
                        db.products.Remove(prod);
                        db.SaveChanges();
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(prod.photo))
                            {
                                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, prod.photo.Replace(",", "."));
                                if (File.Exists(path)) File.Delete(path);
                            }
                        }
                        catch { }
                    }
                }
                MessageBox.Show("Товар удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow lw = new LoginWindow();
            lw.Show();
            this.Close();
        }
    }
}