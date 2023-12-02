﻿using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using Sitronics.Data;
using Sitronics.Models;
using Sitronics.Repositories;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Sitronics.View
{
    /// <summary>
    /// Логика взаимодействия для AddRouteWindow.xaml
    /// </summary>
    public partial class AddRouteWindow : Window
    {
        RoutingProvider routingProvider;
        int countBusStation = 0;

        ObservableCollection<BusStation> BusStations = new ObservableCollection<BusStation>();

        public AddRouteWindow()
        {
            InitializeComponent();
            //CreateNewBusStopComboBox();
            Manager.MainTimer.Tick += new EventHandler(UpdateTimer_Tick);
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            await LoadData();
        }

        private async void CreateNewBusStopComboBox()
        {
            await LoadData();
            ComboBox comboBox = new ComboBox();
            comboBox.ItemsSource = BusStations;
            comboBox.SelectedIndex = 0;
            comboBox.DisplayMemberPath = "Name";
            comboBox.Margin = new Thickness(0, 5, 0, 0);
            comboBox.SelectionChanged += ComboBox_SelectionChanged;
            comboBoxesStackPanel.Children.Add(comboBox);
        }

        private async Task LoadData()
        {
            foreach (var busStation in await Connection.Client.GetFromJsonAsync<List<BusStation>>("/busStations"))
            {
                if (BusStations.Any(r => r.Name == busStation.Name))
                    continue;
                BusStations.Add(busStation);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mapView.Markers.Clear();
            Models.Route dbroute = AddBusPointToMap();
        }

        private void MapView_Loaded(object sender, RoutedEventArgs e)
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            // choose your provider here
            mapView.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            mapView.MinZoom = 10;
            mapView.MaxZoom = 17;
            // whole world zoom
            mapView.Zoom = 14;
            mapView.ShowCenter = false;
            // lets the map use the mousewheel to zoom
            mapView.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
            // lets the user drag the map
            mapView.CanDragMap = true;
            // lets the user drag the map with the left mouse button
            mapView.DragButton = MouseButton.Left;
            routingProvider =
                mapView.MapProvider as RoutingProvider ?? GMapProviders.OpenStreetMap;
            mapView.SetPositionByKeywords("Архангельский Колледж Телекоммуникаций");
        }

        private void AddBusStopButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewBusStopComboBox();
            CheckEnabled();
            mapView.Markers.Clear();
            Models.Route dbroute = AddBusPointToMap();
            countBusStation++;
        }

        private async void SaveRouteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Models.Route dbroute = AddBusPointToMap();
                var response = await Connection.Client.PostAsJsonAsync("/route", dbroute);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Маршрут успешно добавлен");
                }
                else
                {
                    MessageBox.Show("Кажется такой маршрут уже есть");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private Models.Route AddBusPointToMap()
        {
            MapRoute route;
            GMapRoute mapRoute;
            RouteByBusStation rbp;
            Models.Route dbroute = new();
            BusStation busStation;
            List<PointLatLng> points = new List<PointLatLng>();

            dbroute.Name = routeNameTextBox.Text;
            var comboBoxes = comboBoxesStackPanel.Children;
            var serialNumberBusStation = 1;
            foreach (ComboBox comboBox in comboBoxes)
            {
                busStation = (BusStation)comboBox.SelectedItem;
                rbp = new() { IdBusStation = busStation.IdBusStation, SerialNumberBusStation = serialNumberBusStation };
                dbroute.RouteByBusStations.Add(rbp);
                points.Add(new PointLatLng(busStation.Location.Coordinate.Y, busStation.Location.Coordinate.X));
                serialNumberBusStation++;
            }
            for (int i = 0; i < points.Count - 1; i++)
            {
                route = routingProvider.GetRoute(
                points[i], //start
                points[i + 1], //end
                false, //avoid highways 
                false, //walking mode\
                (int)mapView.Zoom);
                mapRoute = new GMapRoute(route.Points);
                mapView.Markers.Add(mapRoute);
            }
            dbroute.IsBacked = isBackedCheckBox.IsChecked ?? false;

            return dbroute;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
        private void pnlControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SendMessage(helper.Handle, 161, 2, 0);
        }

        private void pnlControlBar_MouseEnter(object sender, MouseEventArgs e)
        {
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal) WindowState = WindowState.Maximized;
            else WindowState = WindowState.Normal;
        }

        private void RouteNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckEnabled();
        }

        private void CheckEnabled()
        {
            saveRouteButton.IsEnabled = routeNameTextBox.Text.Length > 0 && countBusStation > 1;
        }
    }
}
