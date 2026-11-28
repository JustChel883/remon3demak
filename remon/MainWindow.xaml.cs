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
                OrdersBtn.Visibility = Visibility.Visible;
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
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки производителей:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                string folder = AppDomain.CurrentDomain.BaseDirectory;
                allProducts.Clear();

                using (var db = new remonEntities1())
                {
                    var productsFromDb = db.products
                        .Include("id_category")
                        .Include("manufactor")
                        .Include("supplement")
                        .Include("measurments")
                        .ToList();

                    foreach (var p in productsFromDb)
                    {
                        double discount = 0;
                        if (p.discount.HasValue)
                        {
                            discount = p.discount.Value;
                        }

                        double discVal = discount * 100;
                        string discStr = "Нет скидки";
                        bool high = false;
                        Visibility oldPriceVisibility = Visibility.Collapsed;

                        if (discount > 0)
                        {
                            discStr = "Скидка:\n" + discVal.ToString("0") + "%";
                            oldPriceVisibility = Visibility.Visible;
                            if (discVal > 15)
                            {
                                high = true;
                            }
                        }

                        string imgPath = Path.Combine(folder, "picture.png");
                        if (!string.IsNullOrWhiteSpace(p.photo))
                        {
                            string fileName = p.photo.Replace(",", ".");
                            string fullPath = Path.Combine(folder, fileName);
                            if (File.Exists(fullPath))
                            {
                                imgPath = fullPath;
                            }
                        }

                        string category = "";
                        if (p.id_category != null)
                        {
                            category = p.id_category.catergory_name;
                        }

                        string manufacturer = "";
                        if (p.manufactor != null)
                        {
                            manufacturer = p.manufactor.name;
                        }

                        string supplier = "";
                        if (p.supplement != null)
                        {
                            supplier = p.supplement.name;
                        }

                        string unit = "";
                        if (p.measurments != null)
                        {
                            unit = p.measurments.name;
                        }

                        string name = "";
                        if (p.article != null)
                        {
                            name = p.article;
                        }

                        string description = "";
                        if (p.description != null)
                        {
                            description = p.description;
                        }

                        bool canDelete = false;
                        if (currentUser != null && currentUser.id_role == 1)
                        {
                            canDelete = true;
                        }

                        decimal finalPrice = p.price - p.price * (decimal)discount;

                        allProducts.Add(new ProductDisplay
                        {
                            Id = p.id_product,
                            Name = name,
                            Category = category,
                            Description = description,
                            Manufacturer = manufacturer,
                            Supplier = supplier,
                            Price = p.price,
                            Unit = unit,
                            Quantity = p.quantity,
                            DiscountValue = discVal,
                            DiscountText = discStr,
                            IsHighDiscount = high,
                            CanDelete = canDelete,
                            Image = LoadImage(imgPath),
                            OldPriceText = p.price.ToString("0.00") + " руб.",
                            FinalPriceText = finalPrice.ToString("0.00") + " руб.",
                            OldPriceVisibility = oldPriceVisibility
                        });
                    }
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товаров:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (SearchTb == null || ManufacturerCb == null || SortCb == null || ProductsList == null)
            {
                return;
            }

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
                    p.Name.ToLower().Contains(search) ||
                    p.Category.ToLower().Contains(search) ||
                    p.Description.ToLower().Contains(search) ||
                    p.Manufacturer.ToLower().Contains(search) ||
                    p.Supplier.ToLower().Contains(search) ||
                    p.Unit.ToLower().Contains(search));
            }

            if (SortCb.SelectedIndex == 1)
            {
                result = result.OrderBy(p => p.Quantity);
            }
            else if (SortCb.SelectedIndex == 2)
            {
                result = result.OrderByDescending(p => p.Quantity);
            }
            else if (SortCb.SelectedIndex == 3)
            {
                result = result.OrderBy(p => p.Price);
            }
            else if (SortCb.SelectedIndex == 4)
            {
                result = result.OrderByDescending(p => p.Price);
            }
            else if (SortCb.SelectedIndex == 5)
            {
                result = result.OrderBy(p => p.DiscountValue);
            }
            else if (SortCb.SelectedIndex == 6)
            {
                result = result.OrderByDescending(p => p.DiscountValue);
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

        private void OrdersBtn_Click(object sender, RoutedEventArgs e)
        {
            OrdersWindow w = new OrdersWindow(currentUser);
            w.Owner = this;
            w.ShowDialog();
        }

        private void AddProductBtn_Click(object sender, RoutedEventArgs e)
        {
            ProductEditWindow w = new ProductEditWindow(0);
            w.Owner = this;
            w.ShowDialog();
            LoadProducts();
        }

        private void Product_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (currentUser == null)
            {
                return;
            }
            if (currentUser.id_role != 1)
            {
                return;
            }

            Border border = sender as Border;
            if (border == null)
            {
                return;
            }

            ProductDisplay product = border.DataContext as ProductDisplay;
            if (product == null)
            {
                return;
            }

            ProductEditWindow w = new ProductEditWindow(product.Id);
            w.Owner = this;
            w.ShowDialog();
            LoadProducts();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            Button btn = sender as Button;
            if (btn == null)
            {
                return;
            }
            int id = (int)btn.Tag;

            MessageBoxResult answer = MessageBox.Show("Удалить товар? Отменить это действие будет нельзя.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
                        MessageBox.Show("Этот товар есть в заказах, удалить его нельзя.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                string file = prod.photo.Replace(",", ".");
                                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                MessageBox.Show("Товар удален.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления товара:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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