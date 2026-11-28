using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using remon.models;

namespace remon
{
    public partial class OrdersWindow : Window
    {
        private users currentUser;
        private List<OrderDisplay> allOrders = new List<OrderDisplay>();

        public OrdersWindow(users user)
        {
            InitializeComponent();
            currentUser = user;

            if (currentUser == null)
            {
                UserFioTb.Text = "Гость";
            }
            else
            {
                UserFioTb.Text = currentUser.Surname + " " + currentUser.Name + " " + currentUser.Patronymic;
                if (currentUser.id_role == 1)
                {
                    AddOrderBtn.Visibility = Visibility.Visible;
                }
            }

            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                allOrders.Clear();

                using (var db = new remonEntities1())
                {
                    var ordersFromDb = db.request
                        .Include("status")
                        .Include("pickup_points")
                        .Include("pickup_points.cities")
                        .Include("order_items")
                        .Include("order_items.products")
                        .ToList();

                    foreach (var r in ordersFromDb)
                    {
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

                        string address = "";
                        if (r.pickup_points != null)
                        {
                            address = r.pickup_points.postal_code.ToString() + ", ";
                            if (r.pickup_points.cities != null)
                            {
                                address = address + r.pickup_points.cities.city_name + ", ";
                            }
                            address = address + "ул. " + r.pickup_points.street + ", д. " + r.pickup_points.house;
                        }

                        string statusName = "";
                        if (r.status != null)
                        {
                            statusName = r.status.status1;
                        }

                        bool canDelete = false;
                        if (currentUser != null && currentUser.id_role == 1)
                        {
                            canDelete = true;
                        }

                        allOrders.Add(new OrderDisplay
                        {
                            Id = r.id_request,
                            Number = r.number.ToString(),
                            Article = articles,
                            Status = statusName,
                            Address = address,
                            RequestDate = r.request_date.ToString("dd.MM.yyyy"),
                            DeliveryDate = r.delivery_date.ToString("dd.MM.yyyy"),
                            CanDelete = canDelete
                        });
                    }
                }

                OrdersList.ItemsSource = null;
                OrdersList.ItemsSource = allOrders;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            OrderEditWindow w = new OrderEditWindow(0, currentUser);
            w.Owner = this;
            w.ShowDialog();
            LoadOrders();
        }

        private void Order_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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

            OrderDisplay order = border.DataContext as OrderDisplay;
            if (order == null)
            {
                return;
            }

            OrderEditWindow w = new OrderEditWindow(order.Id, currentUser);
            w.Owner = this;
            w.ShowDialog();
            LoadOrders();
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

            MessageBoxResult answer = MessageBox.Show("Удалить заказ? Отменить это действие будет нельзя.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (var db = new remonEntities1())
                {
                    var items = db.order_items.Where(o => o.id_request == id).ToList();
                    foreach (var item in items)
                    {
                        db.order_items.Remove(item);
                    }

                    var order = db.request.Find(id);
                    if (order != null)
                    {
                        db.request.Remove(order);
                    }

                    db.SaveChanges();
                }

                MessageBox.Show("Заказ удален.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления заказа:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}