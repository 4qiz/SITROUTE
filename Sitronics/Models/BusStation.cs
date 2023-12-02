﻿using NetTopologySuite.Geometries;
using Sitronics.Repositories;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sitronics.Models;

public partial class BusStation
{
    public int IdBusStation { get; set; }

    public int? PeopleCount { get; set; }

    public string Name { get; set; } = null!;

    public Geometry Location { get; set; } = null!;

    [NotMapped]
    string locationForSerialization;

    [NotMapped]
    public string LocationForSerialization
    {
        get
        {
            return locationForSerialization;
        }
        set
        {
            Location = ConverterGeometry.GetPointByString(value);
        }
    }

    public virtual ICollection<RouteByBusStation> RouteByBusStations { get; set; } = new List<RouteByBusStation>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
