/*
    Post-Deployment Script: TestData
    Inserta datos de prueba en membresia.Usuarios.
    Solo se ejecuta si la tabla está vacía para evitar duplicados.
*/

IF OBJECT_ID('membresia.Usuarios', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM membresia.Usuarios)
    BEGIN
        INSERT INTO membresia.Usuarios (Nombre, Email, Rol, EstadoActivo, UltimoLogin, FechaCreacion)
        VALUES
            ('Admin Sistema',    'admin@membresia.local',    'Admin',    1, GETDATE(),           GETDATE()),
            ('Juan Pérez',       'juan.perez@example.com',   'Usuario',  1, GETDATE(),           GETDATE()),
            ('María López',      'maria.lopez@example.com',  'Usuario',  1, DATEADD(DAY,-3, GETDATE()), DATEADD(MONTH,-2, GETDATE())),
            ('Carlos Ruiz',      'carlos.ruiz@example.com',  'Moderador',1, DATEADD(DAY,-1, GETDATE()), DATEADD(MONTH,-5, GETDATE())),
            ('Ana Torres',       'ana.torres@example.com',   'Usuario',  0, NULL,                DATEADD(MONTH,-8, GETDATE())),
            ('Luis Mendoza',     'luis.mendoza@example.com', 'Usuario',  1, DATEADD(DAY,-7, GETDATE()), DATEADD(YEAR,-1,  GETDATE())),
            ('Sofía Herrera',    'sofia.herrera@example.com','Moderador',1, GETDATE(),           DATEADD(MONTH,-3, GETDATE())),
            ('Pedro Castillo',   'pedro.castillo@example.com','Usuario', 0, DATEADD(DAY,-30, GETDATE()), DATEADD(YEAR,-2, GETDATE()));

        PRINT 'TestData: 8 usuarios de prueba insertados correctamente.';
    END
    ELSE
    BEGIN
        PRINT 'TestData: La tabla membresia.Usuarios ya contiene datos. No se insertaron registros.';
    END
END
GO
