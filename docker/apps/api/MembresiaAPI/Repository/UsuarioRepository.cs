using Microsoft.Data.SqlClient;
using MembresiaAPI.Models;
using System.Data;

namespace MembresiaAPI.Repository
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly string _connectionString;

        public UsuarioRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<PaginacionResponse<Usuario>> ObtenerUsuariosPaginadoAsync(int pageNumber, int pageSize)
        {
            var response = new PaginacionResponse<Usuario>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("membresia.sp_ObtenerUsuariosPaginado", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@PageNumber", pageNumber);
                command.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await command.ExecuteReaderAsync();

                // Primer resultset: usuarios
                while (await reader.ReadAsync())
                {
                    response.Data.Add(new Usuario
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Rol = reader.GetString(reader.GetOrdinal("Rol")),
                        EstadoActivo = reader.GetBoolean(reader.GetOrdinal("EstadoActivo")),
                        UltimoLogin = reader.IsDBNull(reader.GetOrdinal("UltimoLogin"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("UltimoLogin")),
                        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion"))
                    });
                }

                // Mover al siguiente resultset: total registros
                await reader.NextResultAsync();
                if (await reader.ReadAsync())
                {
                    response.TotalRegistros = reader.GetInt32(reader.GetOrdinal("TotalRegistros"));
                }

                response.PageNumber = pageNumber;
                response.PageSize = pageSize;

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener usuarios: {ex.Message}", ex);
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand("SELECT 1", connection);
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}