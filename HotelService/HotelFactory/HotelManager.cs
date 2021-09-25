using HotelService.DataAccess;
using HotelService.Models;

using System;
using System.Collections.Generic;
using System.Text;

namespace HotelService.Manager
{
  public class HotelFactory : IFactory
  {
    private readonly IDataAccess<Hotel> _dataAccess;

    public HotelFactory(IDataAccess<Hotel> dataAccess)
    {
      _dataAccess = dataAccess;
    }

    public bool CompensationRequest(string id)
    {
      return _dataAccess.RemoveById(id);
    }

    public bool ProcessRequest(string id)
    {
      return _dataAccess.GetById(id).HasRoom;
    }
  }
}
