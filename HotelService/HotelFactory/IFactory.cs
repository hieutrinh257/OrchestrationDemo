namespace HotelService.Manager
{
  public interface IFactory
  {
    bool ProcessRequest(string id);

    bool CompensationRequest(string id);
  }
}