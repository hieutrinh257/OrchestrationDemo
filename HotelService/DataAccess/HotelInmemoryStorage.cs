using HotelService.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotelService.DataAccess
{
  public class HotelInmemoryStorage : IDataAccess<Hotel>
  {
    private readonly IList<Hotel> hotels = new List<Hotel>() { 
      new Hotel { 
        Id = "1",
        Name = "Victoria",
        Address = "10 Pasteur",
        HasRoom = true
      },
      new Hotel {
        Id = "1",
        Name = "Rex",
        Address = "141 Nguyen Hue",
        HasRoom = true
      }
    };

    public void Add(Hotel sagaModel)
    {
      hotels.Add(sagaModel);
    }

    public IEnumerable<Hotel> Get()
    {
      return hotels;
    }

    public Hotel GetByName(string name)
    {
      return hotels.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public Hotel GetById(string id)
    {
      return hotels.FirstOrDefault(x => x.Id == id);
    }

    public bool RemoveById(string id)
    {
      return hotels.Remove(new Hotel { Id = id });
    }
  }
}
