﻿using Sitronics.Data;
using Sitronics.Models;
using Sitronics.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sitronics.View
{
    /// <summary>
    /// Логика взаимодействия для RouteInfoView.xaml
    /// </summary>
    public partial class RouteInfoView : UserControl
    {
        public RouteInfoView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            List<Route> routes = await Connection.Client.GetFromJsonAsync<List<Route>>("/routes");
            BusScheduleAlgorithm busScheduleAlgorithm  = new BusScheduleAlgorithm();
            routes = routes.OrderByDescending(r => busScheduleAlgorithm.GetRouteProfitModifier(r.IdRoute)).ToList();
            routesDataGrid.ItemsSource = routes.Select(r => new {r.IdRoute, r.Name, profit = busScheduleAlgorithm.GetRouteProfitModifier(r.IdRoute)});
        }
    }
}
