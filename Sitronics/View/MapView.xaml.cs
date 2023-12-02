﻿using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using Sitronics.Models;
using Sitronics.Repositories;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sitronics.View
{
    /// <summary>
    /// Логика взаимодействия для MapView.xaml
    /// </summary>
    public partial class MapView : UserControl
    {
        List<BusStation> BusStations { get; set; }
        List<Bus> Buses { get; set; }
        List<Models.Route> Routes { get; set; }

        public MapView()
        {
            InitializeComponent();

            Manager.MainTimer.Tick += new EventHandler(UpdateTimer_Tick);
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            await LoadData();
        }

        private void AddBusButton_Click(object sender, RoutedEventArgs e)
        {
            var fm = new AddBusWindow();
            fm.ShowDialog();
        }

        private void AddStopButton_Click(object sender, RoutedEventArgs e)
        {
            var fm = new AddStopWindow();
            fm.ShowDialog();
        }

        private async void MapView_Loaded(object sender, RoutedEventArgs e)
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
            mapView.SetPositionByKeywords("Архангельский Колледж Телекоммуникаций");
            await LoadData();

        }

        private async Task LoadData()
        {
            mapView.Markers.Clear();
            List<PointLatLng> points = new List<PointLatLng>();
            RoutingProvider routingProvider =
            mapView.MapProvider as RoutingProvider ?? GMapProviders.OpenStreetMap;
            Random random = new();

            BusStations = await Connection.Client.GetFromJsonAsync<List<BusStation>>("/busStations");
            Buses = await Connection.Client.GetFromJsonAsync<List<Bus>>("/buses");
            Routes = await Connection.Client.GetFromJsonAsync<List<Models.Route>>("/routesByBusStations");
            foreach (var dbroute in Routes)
            {
                var routeColor = new SolidColorBrush(Color.FromArgb(255, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255)));
                points.Clear();
                foreach (var routePoint in dbroute.RouteByBusStations)
                {
                    points.Add(new PointLatLng(routePoint.IdBusStationNavigation.Location.Coordinate.Y, routePoint.IdBusStationNavigation.Location.Coordinate.X));
                }
                AddRouteOnMap(points, routeColor, routingProvider);
            }
            
            foreach (var bus in Buses)
            {
                var point = new PointLatLng(bus.Location.Coordinate.Y, bus.Location.Coordinate.X);
                MapManager.MapManager.CreateBusMarker(point, ref mapView, bus);
            }
        }

        private void AddRouteOnMap(List<PointLatLng> points, SolidColorBrush routeColor, RoutingProvider routingProvider)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                var route = routingProvider.GetRoute(
                                        points[i], //start
                                        points[i + 1], //end
                                        false, //avoid highways 
                                        false, //walking mode
                                        (int)mapView.Zoom);
                Debug.WriteLine(route.Distance.ToString());
                var mapRoute = new GMapRoute(route.Points);
                mapRoute.Shape = new Path() { Stroke = routeColor, StrokeThickness = 4 };
                mapView.Markers.Add(mapRoute);
            }
        }

        private void AddRouteButton_Click(object sender, RoutedEventArgs e)
        {
            var fm = new AddRouteWindow();
            fm.ShowDialog();
        }
    }
}
