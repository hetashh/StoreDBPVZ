using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using OnlineStoreDB.Model;

namespace OnlineStoreDB.ViewModel
{
    public class AdminViewModel : INotifyPropertyChanged
    {
        private List<string> _tableNames;
        public List<string> TableNames
        {
            get { return _tableNames; }
            set
            {
                _tableNames = value;
                OnPropertyChanged(nameof(TableNames)); 
            }
        }

        //  хранение выбранное название таблицы
        private string _selectedTable;
        public string SelectedTable
        {
            get { return _selectedTable; }
            set
            {
                _selectedTable = value;
                OnPropertyChanged(nameof(SelectedTable)); // уведомление об изменении свойства

                LoadTableData(); // загружзка данные выбранной таблицы
                ApplyFilter(); 

            }
        }

        public AdminViewModel()
        {

            LoadTableNames(); 
            SaveChangesCommand = new RelayCommand(SaveChanges); 
            AddNewRowCommand = new RelayCommand(AddNewRow); 
            DeleteSelectedRowsCommand = new RelayCommand(DeleteSelectedRows); 

        }

        // метод для загрузки названий таблиц из базы данных
        private void LoadTableNames()
        {
            // получениее всех свойств типа DbSet<> 
            PropertyInfo[] dbSetProperties = typeof(ApplicationContextDB).GetProperties()
                                            .Where(p => p.PropertyType.IsGenericType &&
                                                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                                            .ToArray();

            //  названия свойств DbSet<> в список строк и их присвоение к свойству TableNames
            TableNames = dbSetProperties.Select(p => p.Name).ToList();
        }

        private ObservableCollection<object> _tableData;
        public ObservableCollection<object> TableData
        {
            get { return _tableData; }
            set
            {
                _tableData = value;
                OnPropertyChanged(nameof(TableData)); 
            }
        }

        // метод для загрузки данных выбранной таблицы
        private void LoadTableData()
        {
            // если SelectedTable пустое, устанавливаем TableData равным null 
            if (string.IsNullOrEmpty(SelectedTable))
            {
                TableData = null;
                return;
            }

            //  новый экземпляр контекста базы данных
            using (var context = new ApplicationContextDB())
            {
                // получение свойства DbSet<> для выбранной таблицы
                var dbSetProperty = typeof(ApplicationContextDB).GetProperty(SelectedTable);


                if (dbSetProperty != null)
                {
                    //  тип сущности для выбранной таблицы
                    var entityType = dbSetProperty.PropertyType.GetGenericArguments().First();

                    //  экземпляр DbSet<> для выбранной таблицы
                    var dbSet = context.GetType().GetMethod("Set").MakeGenericMethod(entityType).Invoke(context, null);

                    // образование данных из DbSet<> в список объектов и  новая ObservableCollection<object>
                    var tableData = ((IEnumerable<object>)dbSet).ToList();
                    TableData = new ObservableCollection<object>(tableData);
                }
                else
                {
                   TableData = null;
                }
            }
        }

        public ICommand SaveChangesCommand { get; set; }

        // метод который вызывается при выполнении команды сохранения изменений
        private void SaveChanges(object obj)
        {
            if (TableData == null)
            {
                return;
            }

            //  новый экземпляр контекста базы данных
            using (var context = new ApplicationContextDB())
            {
                //  тип сущности для выбранной таблицы
                var entityType = typeof(ApplicationContextDB).Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == SelectedTable);

                if (entityType == null)
                {
                    return;
                }

                //  экземпляр DbSet<> для выбранной таблицы
                var dbSet = context.GetType().GetMethod("Set").MakeGenericMethod(entityType).Invoke(context, null);

                foreach (var entity in TableData)
                {
                    context.Entry(entity).State = EntityState.Modified;
                }

                //  изменения в бд
                context.SaveChanges();
            }

            ApplyFilter();
        }

        public ICommand DeleteSelectedRowsCommand { get; set; }

        public ICommand AddNewRowCommand { get; private set; }

        // метод который вызывается при выполнении команды добавления новой строки
        private void AddNewRow(object obj)
        {
            if (string.IsNullOrEmpty(SelectedTable))
            {
                return;
            }

            //  новый экземпляр контекста базы данных
            using (var context = new ApplicationContextDB())
            {
                var entityType = typeof(ApplicationContextDB).Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == SelectedTable);

                if (entityType == null)
                {
                    return;
                }

                //  новый экземпляр сущности для выбранной таблицы
                var newEntity = Activator.CreateInstance(entityType);

                //  новая сущность в контекст базы данных
                context.Add(newEntity);

                context.SaveChanges();

                LoadTableData();

                ApplyFilter();
            }
        }

        //  свойство, которое будет хранить выбранные строки для удаления
        public IEnumerable<object> selectedRows { get; set; }

        // метод который вызывается при выполнении команды удаления выбранных строк
        private void DeleteSelectedRows(object obj)
        {
            if (string.IsNullOrEmpty(SelectedTable) || TableData == null || !TableData.Any())
            {
                return;
            }

            if (selectedRows == null || !selectedRows.Any())
            {
                return;
            }

            using (var context = new ApplicationContextDB())
            {
                var entityType = typeof(ApplicationContextDB).Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == SelectedTable);

                if (entityType == null)
                {
                    return;
                }

                //  экземпляр DbSet<> для выбранной таблицы
                var dbSet = context.GetType().GetMethod("Set").MakeGenericMethod(entityType).Invoke(context, null);

                //  каждая выбранная строка
                foreach (var selectedRow in selectedRows)
                {
                    // если сущность не отслеживается контекстом, добавляем ее для отслеживания
                    if (context.Entry(selectedRow).State == EntityState.Detached)
                    {
                        dbSet.GetType().GetMethod("Attach").Invoke(dbSet, new[] { selectedRow });
                    }

                    // удалнние сущность из контекста
                    dbSet.GetType().GetMethod("Remove").Invoke(dbSet, new[] { selectedRow });
                }

                context.SaveChanges();

                LoadTableData();

                ApplyFilter();
            }
        }

        //  поле для хранения текста фильтра
        private string _filterText;

        public string FilterText
        {
            get { return _filterText; }
            set
            {
                _filterText = value;
                OnPropertyChanged(nameof(FilterText)); 
                ApplyFilter(); 
            }
        }

        //  поле для хранения отфильтрованных данных таблицы
        private ObservableCollection<object> _filteredTableData;

        public ObservableCollection<object> FilteredTableData
        {
            get { return _filteredTableData; }
            set
            {
                _filteredTableData = value;
                OnPropertyChanged(nameof(FilteredTableData)); 
            }
        }

        // метод для применения фильтра к данным таблицы
        private void ApplyFilter()
        {
            // если текст фильтра пустой то присваиваем FilteredTableData все данные из TableData
            if (string.IsNullOrEmpty(FilterText))
            {
                FilteredTableData = TableData;
            }
            else
            {
                //  новый ObservableCollection для хранения отфильтрованных данных
                FilteredTableData = new ObservableCollection<object>();

                if (SelectedTable != null)
                {
                    // для каждого объекта в TableData
                    foreach (var item in TableData)
                    {
                        // получение свойства объекта
                        var properties = item.GetType().GetProperties();

                        // для каждого свойства объекта
                        foreach (var property in properties)
                        {
                            // получение значения свойства в виде строки
                            var value = property.GetValue(item)?.ToString();

                            // если значение не пустое и содержит текст фильтра
                            if (!string.IsNullOrEmpty(value) && value.Contains(FilterText))
                            {
                                // добавление объекта в отфильтрованные данные
                                FilteredTableData.Add(item);
                                break; 
                            }
                        }
                    }
                }
                else return; 
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}