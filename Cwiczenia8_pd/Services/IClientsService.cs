using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientsService
{
    Task<ClientDTO?> GetClientTrips(int idClient, CancellationToken cancellationToken);
    Task <int> PutClientTrips(ClientAddDTO clientAddDTO, CancellationToken cancellationToken);
    
    public enum RegisterClientResult
    {
        Success,
        NotFoundClient,
        NotFoundTrip,
        MaxCapacityReached,
        AlreadyRegistered,
        Error
    }
    
    Task<RegisterClientResult> PutClientTrip(int clientId, int tripId, CancellationToken cancellationToken);

    Task<int> DeleteClientTrip(int clientId, int tripId, CancellationToken cancellationToken);
}