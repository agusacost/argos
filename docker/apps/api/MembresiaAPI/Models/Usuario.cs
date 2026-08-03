namespace MembresiaAPI.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool EstadoActivo { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class PaginacionRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PaginacionResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalRegistros { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRegistros / (double)PageSize);
    }
}