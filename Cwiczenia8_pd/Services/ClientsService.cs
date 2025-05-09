using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientsService : IClientsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";

    public async Task<ClientDTO?> GetClientTrips(int idClient, CancellationToken cancellationToken)
    {
        ClientDTO? client = null;

        string command = @"SELECT 
            c.IdClient, 
            t.IdTrip, 
            t.Name AS TripName, 
            t.Description, 
            t.DateFrom, 
            t.DateTo, 
            t.MaxPeople,
            ct.RegisteredAt,
            ct.PaymentDate
            FROM Client c   
            LEFT JOIN Client_Trip ct ON ct.IdClient = c.IdClient                        
            LEFT JOIN Trip t ON ct.IdTrip = t.IdTrip
            WHERE c.IdClient = @IdClient";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", idClient);
            await conn.OpenAsync(cancellationToken);

            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (client == null)
                    {
                        client = new ClientDTO
                        {
                            IdClient = idClient,
                            Trips = new List<TripDTO>()
                        };
                    }
                    if (!reader.IsDBNull(reader.GetOrdinal("TripName")))
                    {
                        client.Trips.Add(new TripDTO
                        {
                            Name = (string)reader.GetValue(reader.GetOrdinal("TripName")),
                            Description = (string)reader.GetValue(reader.GetOrdinal("Description")),
                            DateFrom = (DateTime)reader.GetValue(reader.GetOrdinal("DateFrom")),
                            DateTo = (DateTime)reader.GetValue(reader.GetOrdinal("DateTo")),
                            MaxPeople = (int)reader.GetValue(reader.GetOrdinal("MaxPeople")),
                            RegisteredAt = new DateTime(reader.GetInt32(reader.GetOrdinal("RegisteredAt"))),
                            PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                                ? (DateTime?)null
                                : new DateTime(reader.GetInt32(reader.GetOrdinal("PaymentDate")))
                        });
                    }
                }
            }
        }
        
        return client;
    }

    public async Task<int> PutClientTrips(ClientAddDTO client, CancellationToken cancellationToken)
    {
        string command = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)   
                   VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
                   SELECT CAST(SCOPE_IDENTITY() AS INT);";

        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
            cmd.Parameters.AddWithValue("@LastName", client.LastName);
            cmd.Parameters.AddWithValue("@Email", client.Email);
            cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", client.Pesel);
            
            await conn.OpenAsync(cancellationToken);
            var newId = (int)await cmd.ExecuteScalarAsync(cancellationToken);
            return newId;
        }
    }

    public async Task<IClientsService.RegisterClientResult> PutClientTrip(int clientId, int tripId, CancellationToken cancellationToken)
{
    using (var conn = new SqlConnection(_connectionString))
    {
        await conn.OpenAsync(cancellationToken);

        using (var tran = conn.BeginTransaction())
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tran;

            cmd.CommandText = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
            cmd.Parameters.AddWithValue("@IdClient", clientId);
            if ((await cmd.ExecuteScalarAsync(cancellationToken)) == null)
            {
                return IClientsService.RegisterClientResult.NotFoundClient;
            }

            cmd.CommandText = "SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdTrip", tripId);
            var maxPeopleObj = await cmd.ExecuteScalarAsync(cancellationToken);
            if (maxPeopleObj == null)
            {
                return IClientsService.RegisterClientResult.NotFoundTrip;
            }
            int maxPeople = (int)maxPeopleObj;

            cmd.CommandText = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdTrip", tripId);
            int currentCount = (int)(await cmd.ExecuteScalarAsync(cancellationToken));

            if (currentCount >= maxPeople)
            {
                return IClientsService.RegisterClientResult.MaxCapacityReached;
            }

            cmd.CommandText = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdClient", clientId);
            cmd.Parameters.AddWithValue("@IdTrip", tripId);
            if ((await cmd.ExecuteScalarAsync(cancellationToken)) != null)
            {
                return IClientsService.RegisterClientResult.AlreadyRegistered;
            }
            
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long secondsSinceEpoch = now.ToUnixTimeSeconds();
            cmd.CommandText = @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) 
                                VALUES (@IdClient, @IdTrip, @RegisteredAt)";
            cmd.Parameters.AddWithValue("@RegisteredAt", secondsSinceEpoch);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            await tran.CommitAsync(cancellationToken);
        }
    }

    return IClientsService.RegisterClientResult.Success;
}
    
    public async Task<int> DeleteClientTrip(int clientId, int tripId, CancellationToken cancellationToken)
    {
        const string checkSql = "SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
        const string deleteSql = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@IdClient", clientId);
                checkCmd.Parameters.AddWithValue("@IdTrip", tripId);

                int count = (int)checkCmd.ExecuteScalar();
                if (count == 0)
                    return 0;
            }

            using (SqlCommand deleteCmd = new SqlCommand(deleteSql, conn))
            {
                deleteCmd.Parameters.AddWithValue("@IdClient", clientId);
                deleteCmd.Parameters.AddWithValue("@IdTrip", tripId);
                deleteCmd.ExecuteNonQuery();
            }

            return 1;
        }
    }

}