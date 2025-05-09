using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
    public async Task<List<TripDTO>> GetTrips(CancellationToken cancellationToken)
{
    var tripsDict = new Dictionary<int, TripDTO>();

    string command = @"
        SELECT 
            t.IdTrip, 
            t.Name AS TripName, 
            t.Description, 
            t.DateFrom, 
            t.DateTo, 
            t.MaxPeople, 
            c.Name AS CountryName
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
        ORDER BY t.IdTrip;
    ";

    using (SqlConnection conn = new SqlConnection(_connectionString))
    using (SqlCommand cmd = new SqlCommand(command, conn))
    {
        await conn.OpenAsync(cancellationToken);

        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                int idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip"));

                if (!tripsDict.TryGetValue(idTrip, out var trip))
                {
                    trip = new TripDTO
                    {
                        IdTrip = idTrip,
                        Name = reader.GetString(reader.GetOrdinal("TripName")),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                        DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                        DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                        MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                        Countries = new List<CountryDTO>()
                    };
                    tripsDict.Add(idTrip, trip);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
                {
                    trip.Countries.Add(new CountryDTO
                    {
                        Name = reader.GetString(reader.GetOrdinal("CountryName"))
                    });
                }
            }
        }
    }

    return tripsDict.Values.ToList();
}

}