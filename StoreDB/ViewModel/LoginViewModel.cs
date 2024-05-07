using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Windows;
using System.Windows.Input;
using OnlineStoreDB;
using OnlineStoreDB.Model;
using OnlineStoreDB.View;


namespace OnlineStoreDB.ViewModel
{
    public class LoginViewModel: INotifyPropertyChanged
    {
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public ICommand AuthCommand { get; set; }

        public LoginViewModel() 
        {
            AuthCommand = new RelayCommand(Auth);
        }



        private void Auth(object obj)
        {
            try
            {
                
                Users user;
                using (var dbContext = ApplicationContextDB.getInstance())
                {
                    user = dbContext.Users.FirstOrDefault(u => u.UserName == this.UserName);
                }

                
                if (user != null && user.UserPassword == this.UserPassword)
                {

                    
                    if (user.UserRole == "admin")
                    {
                        AdminWindow adminWindow = new AdminWindow();
                        adminWindow.Show();
                    }
                    else if (user.UserRole == "employee")
                    {
                        EmployeeWindow employeeWindow = new EmployeeWindow();
                        employeeWindow.Show();
                    }
                }
                else
                {
                    MessageBox.Show("ошибочка");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ошибочка: " + ex.Message);
            }
            Application.Current.MainWindow.Close();
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}


