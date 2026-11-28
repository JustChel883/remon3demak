using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using remon.models;

namespace remon
{
    public partial class OrderEditWindow : Window
    {
        private int orderId;
        private users currentUser;

        public OrderEditWindow(int id, users user)
        {
            InitializeComponent();
            orderId = id;
            currentUser = user;

            try
            {
                using (var db = new remonEntities1())
                {
                    List<StatusDisplay> statuses = new List<StatusDisplay>();
                    foreach (var s in db.status.ToList())
                    {
                        StatusDisplay sd = new StatusDisplay();
                        sd.Id = s.id_status;
                        sd.Name = s.status1;
                        statuses.Add(sd);
                    }
                    StatusCb.ItemsSource = statuses;

                    List<PickupPointDisplay> points = new List<PickupPointDisplay>();
                    foreach (var p in db.pickup_points.Include("cities").ToList())
                    {
                        PickupPointDisplay pd = new PickupPointDisplay();
                        pd.Id = p.id_pickup_point;
                        string city = "";
                        if (p.cities != null)
                        {
                            city = p.cities.city_name;
                        }
                        pd.Address = p.postal_code.ToString() + ", " + city + ", ул. " + p.street + ", д. " + p.house;
                        points.Add(pd);
                    }
                    PickupCb.ItemsSource = points;

                    if (orderId == 0)
                    {
                        Title = "Добавление заказа";
                        IdLabel.Visibility = Visibility.Collapsed;
                        IdTb.Visibility = Visibility.Collapsed;
                        RequestDatePicker.SelectedDate = DateTime.Today;
                        DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(3);
                        if (statuses.Count > 0)
                        {
                            StatusCb.SelectedIndex = 0;
                        }
                        if (points.Count > 0)
                        {
                            PickupCb.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        Title = "Редактирование заказа";

                        var r = db.request
                            .Include("order_items")
                            .Include("order_items.products")
                            .FirstOrDefault(x => x.id_request == orderId);

                        if (r == null)
                        {
                            MessageBox.Show("Заказ не найден в базе данных. Закройте окно и обновите список.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        IdTb.Text = r.id_request.ToString();

                        string articles = "";
                        foreach (var item in r.order_items)
                        {
                            if (item.products != null)
                            {
                                if (articles != "")
                                {
                                    articles = articles + ", ";
                                }
                                articles = articles + item.products.article;
                            }
                        }
                        ArticleTb.Text = articles;

                        RequestDatePicker.SelectedDate = r.request_date;
                        DeliveryDatePicker.SelectedDate = r.delivery_date;

                        foreach (StatusDisplay sd in statuses)
                        {
                            if (sd.Id == r.id_status)
                            {
                                StatusCb.SelectedItem = sd;
                            }
                        }

                        foreach (PickupPointDisplay pd in points)
                        {
                            if (pd.Id == r.id_pickup_point)
                            {
                                PickupCb.SelectedItem = pd;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных заказа:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArticleTb.Text))
            {
                MessageBox.Show("Введите артикул товара. Если товаров несколько, перечислите артикулы через запятую.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StatusDisplay status = StatusCb.SelectedItem as StatusDisplay;
            PickupPointDisplay point = PickupCb.SelectedItem as PickupPointDisplay;
            if (status == null || point == null)
            {
                MessageBox.Show("Выберите статус заказа и адрес пункта выдачи из выпадающих списков.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (RequestDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату заказа в календаре.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (DeliveryDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату выдачи в календаре.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime requestDate = RequestDatePicker.SelectedDate.Value;
            DateTime deliveryDate = DeliveryDatePicker.SelectedDate.Value;
            if (deliveryDate < requestDate)
            {
                MessageBox.Show("Дата выдачи не может быть раньше даты заказа. Исправьте даты.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (orderId == 0 && currentUser == null)
            {
                MessageBox.Show("Добавлять заказы может только администратор.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var db = new remonEntities1())
                {
                    string[] articles = ArticleTb.Text.Split(',');
                    List<products> selectedProducts = new List<products>();

                    foreach (string a in articles)
                    {
                        string article = a.Trim();
                        if (article == "")
                        {
                            continue;
                        }

                        products prod = db.products.FirstOrDefault(x => x.article == article);
                        if (prod == null)
                        {
                            MessageBox.Show("Товар с артикулом '" + article + "' не найден в базе данных. Проверьте правильность ввода артикула.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        selectedProducts.Add(prod);
                    }

                    if (selectedProducts.Count == 0)
                    {
                        MessageBox.Show("Не указан ни один артикул товара. Введите артикулы через запятую.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int requestId;

                    if (orderId == 0)
                    {
                        requestId = 1;
                        if (db.request.Any())
                        {
                            requestId = db.request.Max(x => x.id_request) + 1;
                        }

                        request newOrder = new request();
                        newOrder.id_request = requestId;
                        newOrder.number = requestId;
                        newOrder.code = new Random().Next(100, 1000);
                        newOrder.id_user = currentUser.id_user;
                        newOrder.request_date = requestDate;
                        newOrder.delivery_date = deliveryDate;
                        newOrder.id_status = status.Id;
                        newOrder.id_pickup_point = point.Id;
                        db.request.Add(newOrder);
                    }
                    else
                    {
                        requestId = orderId;
                        request order = db.request.Find(orderId);
                        order.request_date = requestDate;
                        order.delivery_date = deliveryDate;
                        order.id_status = status.Id;
                        order.id_pickup_point = point.Id;
                    }

                    db.SaveChanges();

                    List<order_items> oldItems = db.order_items.Where(o => o.id_request == requestId).ToList();

                    foreach (order_items item in oldItems)
                    {
                        bool found = false;
                        foreach (products prod in selectedProducts)
                        {
                            if (prod.id_product == item.id_product)
                            {
                                found = true;
                            }
                        }
                        if (found == false)
                        {
                            db.order_items.Remove(item);
                        }
                    }

                    int newItemId = 1;
                    if (db.order_items.Any())
                    {
                        newItemId = db.order_items.Max(x => x.id_order_item) + 1;
                    }

                    foreach (products prod in selectedProducts)
                    {
                        bool exists = false;
                        foreach (order_items item in oldItems)
                        {
                            if (item.id_product == prod.id_product)
                            {
                                exists = true;
                            }
                        }
                        if (exists == false)
                        {
                            order_items newItem = new order_items();
                            newItem.id_order_item = newItemId;
                            newItemId = newItemId + 1;
                            newItem.id_request = requestId;
                            newItem.id_product = prod.id_product;
                            newItem.quantity = 1;
                            db.order_items.Add(newItem);
                        }
                    }

                    db.SaveChanges();
                }

                MessageBox.Show("Данные заказа сохранены.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить заказ:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}