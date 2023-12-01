﻿using Microsoft.EntityFrameworkCore;
using Sitronics.Data;
using Sitronics.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sitronics
{
    public class BusScheduleAlgorithm
    {
        public List<Schedule> GenerateBusSchedule(DateTime startDate, DateTime endDate, int idRoute, List<RouteByBusStation> routeByBusStation, List<Bus> buses, string weatherInfo, string roadConditions)
        {
            List<Schedule> schedules = new List<Schedule>();
            DateTime busStartTime;
            int workMinutes = endDate.Hour * 60 + endDate.Minute - startDate.Hour * 60 + startDate.Minute;
            int routeTime = GetIntervalInMinutesBetweenBusStations(idRoute, routeByBusStation.First().IdBusStation, routeByBusStation.Last().IdBusStation);//frequencyInMinutes * (routeByBusStation.Count - 1) * 2;
            int busCount = buses.Count;
            int delay = routeTime / (busCount / 2);

            int rushTimeDelay = 3;
            int chillTime = 0;

            int round = workMinutes / routeTime;
            int halfRouteTime = routeTime / 2;
            int halfBusCount = busCount / 2;
            for (int i = 0; i < busCount; i++)
            {
                busStartTime = startDate;
                for (int j = 1; j <= round; j++)
                {
                    if (i >= halfBusCount)
                        busStartTime = busStartTime.AddMinutes(halfRouteTime);
                    busStartTime = busStartTime.AddMinutes(IsRushTime(busStartTime) ? rushTimeDelay * i : delay * i);
                    schedules.AddRange(MakeBusSchedule(buses[i], routeByBusStation, busStartTime, weatherInfo));
                    busStartTime = startDate.AddMinutes(routeTime * j + chillTime);
                }
            }

            return schedules;
        }


        public List<Schedule> MakeBusSchedule(Bus bus, List<RouteByBusStation> busStops, DateTime busStartTime, string weatherCondition)
        {
            List<Schedule> schedules = new List<Schedule>();

            RouteByBusStation startBusStop, endBusStop;
            double busTimeBetweenStations;
            for (int i = 0; i < busStops.Count; i++)
            {
                if (i != 0)
                    startBusStop = busStops[i - 1];
                else
                    startBusStop = busStops[i];
                endBusStop = busStops[i];
                busTimeBetweenStations = Math.Abs(GetIntervalInMinutesBetweenBusStations(
                    (int)bus.IdRoute, startBusStop.IdBusStation, endBusStop.IdBusStation)) / GetWeatherFactor(weatherCondition);
                busStartTime = busStartTime.AddMinutes(busTimeBetweenStations);
                var schedule = new Schedule
                {
                    IdBus = bus.IdBus,
                    IdBusStation = endBusStop.IdBusStation,
                    Time = busStartTime
                };
                schedules.Add(schedule);

            }
            return schedules;
        }

        private bool IsRushTime(DateTime currentDateTime)
        {
            var start = new TimeSpan(8, 0, 0);
            var end = new TimeSpan(10, 0, 0);
            var start2 = new TimeSpan(17, 0, 0);
            var end2 = new TimeSpan(21, 0, 0);
            TimeSpan now = currentDateTime.TimeOfDay;
            return (now >= start) && (now < end) || (now >= start2) && (now < end2);
        }

        public int CalculateBusCount(int minutesToSolveRoute, int delay)
        {
            return minutesToSolveRoute * 2 / delay;
        }

        private double GetWeatherFactor(string weatherInfo)
        {
            var badWeatherFactors = new Dictionary<string, double>
            {
                { "icy condition", 0.6 },
                { "fog", 0.7 },
                { "snowfall", 0.8 }
            };
            if (badWeatherFactors.ContainsKey(weatherInfo))
                return badWeatherFactors[weatherInfo];
            else
                return 1;
        }

        public int GetAmountPeopleOnBusStations(int idRoute)
        {
            using (var context = new SitrouteDataContext())
            {
                var route = context.Routes
                    .Where(r => r.IdRoute == idRoute)
                    .Include(r => r.RouteByBusStations)
                    .ThenInclude(r => r.IdBusStationNavigation).FirstOrDefault();
                return (int)route.RouteByBusStations.Sum(r => r.IdBusStationNavigation.PeopleCount);
            }
        }

        public int GetPeopleOnRouteByDay(DateTime date, int idRoute)
        {
            using (var context = new SitrouteDataContext())
            {
                var route = context.Routes
                    .Where(r => r.IdRoute == idRoute)
                    .Include(r => r.Buses)
                    .ThenInclude(b => b.Schedules).FirstOrDefault(r => r.IdRoute == idRoute);

                return (int)route.Buses.Sum(b => b.Schedules.Where(s => s.Time.Date == date.Date).Sum(s => s.PeopleCountBoardingBus));
            }
        }

        public double GetAveragePeopleOnBusStationByRoute(int idRoute, int idBusStation)
        {
            using (var context = new SitrouteDataContext())
            {
                var route = context.Routes
                    .Where(r => r.IdRoute == idRoute)
                    .Include(r => r.Buses)
                    .ThenInclude(b => b.Schedules).FirstOrDefault(r => r.IdRoute == idRoute);
                double peopleOnBoard = (double)route.Buses.Average(b => b.Schedules.Where(s => s.IdBusStation == idBusStation).Average(s => s.PeopleCountBoardingBus));
                return peopleOnBoard;
            }
        }
        private int GetIntervalInMinutesBetweenBusStations(int idRoute, int idStartBusStation, int idEndBusStation)
        {
            using (var context = new SitrouteDataContext())
            {
                var routeByBusStations = context.RouteByBusStations
                    .Include(s => s.IdBusStationNavigation)
                    .ThenInclude(bs => bs.Schedules)
                    .Where(s => s.IdRoute == idRoute)
                    .ToList();

                return Convert.ToInt32(Math.Round((
                    GetArrivalTime(routeByBusStations, idEndBusStation) -
                    GetArrivalTime(routeByBusStations, idStartBusStation)).TotalMinutes));
            }
        }

        private DateTime GetArrivalTime(List<RouteByBusStation> routeByBusStations, int idBusStation)
        {
            return routeByBusStations.Where(s => s.IdBusStation == idBusStation).First().IdBusStationNavigation.Schedules
                                        .OrderBy(s => s.Time)
                                        .First().Time;
        }
    }
}