using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using remon.models;

namespace remon
{
    public partial class ProductEditWindow : Window
    {
        private int productId;
        private string oldPhoto;
        private string newPhoto;
        private string folder = AppDomain.CurrentDomain.BaseDirectory;
        private remonEntities1 db;

        public ProductEditWindow(int id)
        {
            InitializeComponent();
            productId = id;
            db = new remonEntities1();

            try
            {
                CategoryCb.ItemsSource = db.id_category.ToList();
                ManufacturerCb.ItemsSource = db.manufactor.ToList();
                SupplierCb.ItemsSource = db.supplement.ToList();
                UnitCb.ItemsSource = db.measurments.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PhotoImg.Source = LoadImage(Path.Combine(folder, "picture.png"));

            if (productId == 0)
            {
                Title = "Добавление товара";
                IdLabel.Visibility = Visibility.Collapsed;
                IdTb.Visibility = Visibility.Collapsed;
                QuantityTb.Text = "0";
                DiscountTb.Text = "0";
            }
            else
            {
                Title = "Редактирование товара";
                try
                {
                    var p = db.products
                        .Include("id_category")
                        .Include("manufactor")
                        .Include("supplement")
                        .Include("measurments")
                        .FirstOrDefault(x => x.id_product == productId);

                    if (p != null)
                    {
                        IdTb.Text = p.id_product.ToString();
                        NameTb.Text = p.article;
                        DescriptionTb.Text = p.description;
                        PriceTb.Text = p.price.ToString("0.00");
                        QuantityTb.Text = p.quantity.ToString();
                        DiscountTb.Text = p.discount.HasValue ? (p.discount.Value * 100).ToString("0.##") : "0";

                        CategoryCb.SelectedItem = p.id_category;
                        ManufacturerCb.SelectedItem = p.manufactor;
                        SupplierCb.SelectedItem = p.supplement;
                        UnitCb.SelectedItem = p.measurments;

                        if (!string.IsNullOrWhiteSpace(p.photo))
                        {
                            oldPhoto = p.photo;
                            string path = Path.Combine(folder, p.photo.Replace(",", "."));
                            if (File.Exists(path)) PhotoImg.Source = LoadImage(path);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки товара:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private BitmapImage LoadImage(string path)
        {
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = new Uri(path);
            img.EndInit();
            return img;
        }

        private void ChoosePhotoBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Картинки|*.png;*.jpg;*.jpeg";
            if (dialog.ShowDialog() != true) return;

            try
            {
                BitmapImage img = LoadImage(dialog.FileName);
                if (img.PixelWidth > 300 || img.PixelHeight > 200)
                {
                    MessageBox.Show("Фото слишком большое. Максимум 300×200 пикселей.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(dialog.FileName);
                File.Copy(dialog.FileName, Path.Combine(folder, fileName));
                newPhoto = fileName;
                PhotoImg.Source = img;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть файл.\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTb.Text) || string.IsNullOrWhiteSpace(DescriptionTb.Text))
            {
                MessageBox.Show("Заполните наименование и описание товара.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            id_category cat = CategoryCb.SelectedItem as id_category;
            manufactor man = ManufacturerCb.SelectedItem as manufactor;
            supplement sup = SupplierCb.SelectedItem as supplement;
            measurments unit = UnitCb.SelectedItem as measurments;

            if (cat == null || man == null || sup == null || unit == null)
            {
                MessageBox.Show("Выберите категорию, производителя, поставщика и ед. измерения.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            decimal price;
            if (!decimal.TryParse(PriceTb.Text.Replace(".", ","), out price) || price < 0)
            {
                MessageBox.Show("Цена должна быть числом и не может быть отрицательной.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int quantity;
            if (!int.TryParse(QuantityTb.Text, out quantity) || quantity < 0)
            {
                MessageBox.Show("Количество должно быть целым числом и не может быть отрицательным.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double discount;
            if (!double.TryParse(DiscountTb.Text.Replace(".", ","), out discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Скидка должна быть числом от 0 до 100.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (productId == 0)
                {
                    int newId = 1;
                    if (db.products.Any()) newId = db.products.Max(x => x.id_product) + 1;
                    var p = new products();
                    p.id_product = newId;
                    FillProduct(p, cat, man, sup, unit, price, quantity, discount);
                    db.products.Add(p);
                }
                else
                {
                    var p = db.products.Find(productId);
                    FillProduct(p, cat, man, sup, unit, price, quantity, discount);
                }
                db.SaveChanges();

                if (newPhoto != null && oldPhoto != null)
                {
                    string oldPath = Path.Combine(folder, oldPhoto.Replace(",", "."));
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }

                MessageBox.Show("Данные сохранены.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить товар.\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillProduct(products p, id_category cat, manufactor man, supplement sup, measurments unit, decimal price, int quantity, double discount)
        {
            p.article = NameTb.Text.Trim();
            p.description = DescriptionTb.Text.Trim();
            p.id_category = cat;
            p.manufactor = man;
            p.supplement = sup;
            p.measurments = unit;
            p.price = price;
            p.quantity = quantity;
            p.discount = discount / 100;
            if (newPhoto != null) p.photo = newPhoto;
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }
    }
}