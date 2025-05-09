namespace Tutorial8.Models.DTOs;

public class ClientDTO
{
    public int IdClient { get; set; }
    
    public List<TripDTO> Trips { get; set; }
}