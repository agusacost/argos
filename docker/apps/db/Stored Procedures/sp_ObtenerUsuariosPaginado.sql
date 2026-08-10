CREATE PROCEDURE membresia.sp_ObtenerUsuariosPaginado
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1
    IF @PageSize < 1 SET @PageSize = 10

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize

    SELECT
        Id,
        Nombre,
        Email,
        Rol,
        EstadoActivo,
        UltimoLogin,
        FechaCreacion
    FROM membresia.Usuarios
    ORDER BY Id
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY

    SELECT COUNT(*) AS TotalRegistros
    FROM membresia.Usuarios
END
GO
