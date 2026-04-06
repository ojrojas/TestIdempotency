# Minimal API (.NET 10) — Users / Roles / RoleClaims + Idempotency

Resumen
-------
Demo de API REST usando Minimal APIs (.NET 10) con Clean Architecture (Domain, Application, Infrastructure, Api). Implementa Users, Roles y RoleClaims con ASP.NET Core Identity + EF Core. Idempotencia para operaciones mutables usando header `Idempotency-Key`.

Cómo ejecutar (demo)
--------------------
1. Asegúrate de tener .NET 10 SDK instalado.
2. Desde la raíz del repo:

   dotnet restore
   dotnet build src/Api
   dotnet run --project src/Api

Por defecto la demo usa InMemory DB. Para usar SQL Server, establecer `ConnectionStrings:DefaultConnection` en `src/Api/appsettings.json`.

Endpoints clave
---------------
- POST /api/auth/login  -> { "userName": "admin", "password": "Admin123!" }
- POST /api/users  -> Crear usuario (protegido - role Admin)
- GET  /api/users  -> Listar usuarios (protegido)
- POST /api/roles  -> Crear rol (protegido)
- POST /api/roles/{roleId}/claims -> Añadir claim a rol
- POST /api/users/{userId}/roles -> Asignar rol a usuario

Idempotencia
------------
- Para métodos POST/PUT/DELETE se soporta `Idempotency-Key` en headers.
- Si se repite la misma request (mismo key) dentro del TTL (por defecto 24h) la respuesta se devuelve exactamente igual y la operación no se reejecuta.
- Ejemplo:

  curl -i -X POST http://localhost:5000/api/users \
    -H "Content-Type: application/json" \
    -H "Idempotency-Key: 123e4567-e89b-12d3-a456-426614174000" \
    -H "Authorization: Bearer <ADMIN_TOKEN>" \
    -d '{"userName":"jdoe","email":"jdoe@example.com","password":"P@ssw0rd"}'

- Repeat the same request with the same Idempotency-Key: the API will return the same HTTP status, headers and body without creating another user.

Notas
-----
- This is a demo scaffold. In production use a persistent DB, strong JWT keys and review concurrency/locking for idempotency entries across multiple instances.

Comentarios en el código explican decisiones importantes (buscar "Idempotency" and "Seed").
