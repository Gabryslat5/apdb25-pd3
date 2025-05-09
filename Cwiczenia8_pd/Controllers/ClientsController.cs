using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientsService _clientsService;

        public ClientsController(IClientsService clientsService)
        {
            _clientsService = clientsService;
        }
        
        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id, CancellationToken cancellationToken)
        {
            var clientsTrips = await _clientsService.GetClientTrips(id, cancellationToken);
            if (clientsTrips == null)
            {
                return NotFound();
            }
            return Ok(clientsTrips);
        }

        [HttpPost]
        public async Task<IActionResult> PostClientTrips(ClientAddDTO clientAddDTO, CancellationToken cancellationToken)
        {
            int newId = await _clientsService.PutClientTrips(clientAddDTO, cancellationToken);
            return CreatedAtAction(nameof(GetClientTrips), new { id = newId }, new { IdClient = newId });
        }

        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> PutClientTrip(int id, int tripId, CancellationToken cancellationToken)
        {
            var result = await _clientsService.PutClientTrip(id, tripId, cancellationToken);

            return result switch
            {
                IClientsService.RegisterClientResult.NotFoundClient => NotFound("Client not found."),
                IClientsService.RegisterClientResult.NotFoundTrip => NotFound("Trip not found."),
                IClientsService.RegisterClientResult.MaxCapacityReached => BadRequest("Maximum number of participants reached."),
                IClientsService.RegisterClientResult.AlreadyRegistered => Conflict("Client is already registered for this trip."),
                IClientsService.RegisterClientResult.Success => Ok("Client successfully registered."),
                _ => StatusCode(500, "Unexpected error.")
            };
        }

        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> DeleteClientTrip(int id, int tripId, CancellationToken cancellationToken)
        {
            var success = await _clientsService.DeleteClientTrip(id, tripId, cancellationToken);
            if (success != 1)
                return NotFound("Clients rejestraction to delete not found.");

            return Ok("Clients rejestraction deleted.");
        }
    }
}