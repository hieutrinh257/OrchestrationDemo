using System;
using System.Collections.Generic;
using System.Text;

namespace HotelService.Models
{
  public class Hotel
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public bool HasRoom { get; set; }
  }
}
