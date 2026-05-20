var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();  

// Configuration: use InMemory DB for demo, switch to SQL Server via configuration.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cfg = builder.Configuration;
    var useInMemory = string.IsNullOrEmpty(cfg.GetConnectionString("DefaultConnection"));
    if (useInMemory)
        options.UseInMemoryDatabase("DemoDb");
    else
        options.UseSqlServer(cfg.GetConnectionString("DefaultConnection"));
});

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Authentication - JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ChangeThisInProduction_UseStrongKey";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "demo";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "demo";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ClockSkew = System.TimeSpan.Zero
    };
});


// Authorization policies (examples)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
});

// Application services wiring
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();

builder.Services.AddProblemDetails();

var app = builder.Build();

// Ensure DB + seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DatabaseSeeder.SeedAsync(services);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
    app.MapScalarApiReference();

app.UseAuthentication();

// Idempotency middleware should run before endpoint handlers so it can intercept and persist responses.
app.UseMiddleware<IdempotencyMiddleware>();

app.UseAuthorization();

// Minimal API endpoints (examples, keep handlers small and delegate to services)
app.MapPost("/api/auth/login", async (LoginDto login, UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.FindByNameAsync(login.UserName);
    if (user == null) return Results.Unauthorized();
    if (!await userManager.CheckPasswordAsync(user, login.Password)) return Results.Unauthorized();

    var roles = await userManager.GetRolesAsync(user);
    var claims = new List<System.Security.Claims.Claim>
    {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName ?? "")
    };
    claims.AddRange(roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)));

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        claims: claims,
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        expires: DateTime.UtcNow.AddHours(3)
    );
    var tokenStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = tokenStr });
})
.AllowAnonymous()
.WithName("Login");

app.MapPost("/api/users", [Authorize(Policy = "RequireAdminRole")] async (Application.DTOs.UserCreateDto dto, IUserService userService) =>
{
    var created = await userService.CreateUserAsync(dto);
    return Results.Created($"/api/users/{created.Id}", created);
});

app.MapGet("/api/users", [Authorize(Policy = "RequireAdminRole")] async (IUserService userService) =>
{
    var list = await userService.GetUsersAsync();
    return Results.Ok(list);
});

app.MapPost("/api/roles", [Authorize(Policy = "RequireAdminRole")] async (string roleName, IRoleService roleService) =>
{
    var created = await roleService.CreateRoleAsync(roleName);
    return Results.Created($"/api/roles/{created.Id}", created);
});

app.MapPost("/api/roles/{roleId}/claims", [Authorize(Policy = "RequireAdminRole")] async (Guid roleId, Application.DTOs.RoleClaimDto claimDto, IRoleService roleService) =>
{
    await roleService.AddClaimToRoleAsync(roleId, claimDto.ClaimType, claimDto.ClaimValue);
    return Results.NoContent();
});

app.MapPost("/api/users/{userId}/roles", [Authorize(Policy = "RequireAdminRole")] async (Guid userId, string roleName, IUserService userService) =>
{
    await userService.AssignRoleAsync(userId, roleName);
    return Results.NoContent();
});

app.MapDelete("/api/users/{userId}/roles/{roleName}", [Authorize(Policy = "RequireAdminRole")] async (Guid userId, string roleName, IUserService userService) =>
{
    await userService.RemoveRoleAsync(userId, roleName);
    return Results.NoContent();
});


app.Run();
