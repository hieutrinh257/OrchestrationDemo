using HotelService.Models;

using System.Collections.Generic;

namespace HotelService.DataAccess
{
  public interface IDataAccess<T>
  {
    void Add(T hotel);
    IEnumerable<T> Get();
    T GetByName(string name);
    T GetById(string id);
    bool RemoveById(string id);
  }
}