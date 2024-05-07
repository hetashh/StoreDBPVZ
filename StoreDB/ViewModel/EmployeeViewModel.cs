using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OnlineStoreDB;
using OnlineStoreDB.Model;
using OnlineStoreDB.View;

namespace OnlineStoreDB.ViewModel
{
    public class EmployeeViewModel : INotifyPropertyChanged
    {
        // Коллекция заказов
        private ObservableCollection<Orders> _orders;
        public ObservableCollection<Orders> Orders
        {
            get { return _orders; }
            set
            {
                _orders = value;
                OnPropertyChanged();
            }
        }

        // текст запроса для поиска
        private string _searchQuery;
        public string SearchQuery
        {
            get { return _searchQuery; }
            set
            {
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
                FilterOrders();
            }
        }

        // отфильтрованные заказы
        private ObservableCollection<Orders> _filteredOrders;
        public ObservableCollection<Orders> FilteredOrders
        {
            get { return _filteredOrders; }
            set
            {
                _filteredOrders = value;
                OnPropertyChanged(nameof(FilteredOrders));
            }
        }

        // конструктор класса EmployeeViewModel
        public EmployeeViewModel()
        {
            // загрузка заказов
            LoadOrders();
            // фильтрация заказов
            FilterOrders();
            //   пометка заказа как выполненный
            MarkAsDoneCommand = new RelayCommand(MarkAsDone);
            // загрузка данных для фильтрации по офису
            OrderOffices = new ObservableCollection<int>(GetColumnData());
        }

        // метод загрузки заказов
        private void LoadOrders()
        {
            using (var context = new ApplicationContextDB())
            {
                // пролучение всех заказов из базы данных
                Orders = new ObservableCollection<Orders>(context.Orders.ToList());
            }
        }

        // метод фильтрации заказов по запросу поиска
        private void FilterOrders()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredOrders = Orders;
            }
            else
            {
                // фильтрация заказов по запросу
                FilteredOrders = new ObservableCollection<Orders>(
                    Orders.Where(order =>
                        order.Order_Status.ToLower().Contains(SearchQuery.ToLower()) ||
                        order.Employee.ToLower().Contains(SearchQuery.ToLower()) ||
                        order.OrderID.ToString().Contains(SearchQuery.ToLower())
                    )
                );
            }
        }

        // команда пометки заказа как выполненный
        public ICommand MarkAsDoneCommand { get; set; }

        // метод пометки заказа как выполненный
        private void MarkAsDone(object obj)
        {
            SelectedOrder.Order_Status = "Доставлен";
            using (var context = new ApplicationContextDB())
            {
                context.Orders.Attach(SelectedOrder);
                context.Entry(SelectedOrder).State = EntityState.Modified;
                context.SaveChanges();
            }
        }

        // метод проверки возможности выполнения пометки заказа как выполненный
        private bool CanMarkAsDone(object obj)
        {
            return SelectedOrder != null;
        }

        // выбранный заказ
        private Orders _selectedOrder;
        public Orders SelectedOrder
        {
            get { return _selectedOrder; }
            set
            {
                _selectedOrder = value;
                OnPropertyChanged(nameof(SelectedOrder));
            }
        }

        // получение данных для фильтрации по офису
        private List<int> GetColumnData()
        {
            using (var dbContext = new ApplicationContextDB())
            {
                return dbContext.Orders.Select(row => row.OrderOfficeId).Distinct().ToList();
            }
        }

        // коллекция данных офисов
        private ObservableCollection<int> _orderOffices;
        public ObservableCollection<int> OrderOffices
        {
            get { return _orderOffices; }
            set
            {
                _orderOffices = value;
                OnPropertyChanged(nameof(OrderOffices));
            }
        }

        // выбранный офис
        private int _selectedOffice;
        public int SelectedOffice
        {
            get { return _selectedOffice; }
            set
            {
                _selectedOffice = value;
                OnPropertyChanged(nameof(SelectedOffice));
                // фильтрация заказов по выбранному офису
                FilterOrdersByOffice(_selectedOffice);
                FilterOrders();
            }
        }

        //  заказы по выбранному офису
        private ObservableCollection<Orders> _filteredOrdersByOffice;
        public ObservableCollection<Orders> FilteredOrdersByOffice
        {
            get { return _filteredOrdersByOffice; }
            set
            {
                _filteredOrdersByOffice = value;
                OnPropertyChanged(nameof(FilteredOrdersByOffice));
            }
        }

        // метод фильтрации заказов по выбранному офису
        private void FilterOrdersByOffice(int selectedOfficeId)
        {
            if (selectedOfficeId == 0)
            {
                FilteredOrdersByOffice = Orders;
            }
            else
            {
                FilteredOrdersByOffice = new ObservableCollection<Orders>(
                    Orders.Where(order => order.OrderOfficeId == selectedOfficeId));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
