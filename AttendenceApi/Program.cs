var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS – allow any origin for now; restrict in production if needed.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Register Company as a singleton service so all requests share the same in-memory data.
builder.Services.AddSingleton<AttendenceApi.Company>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Serve index.html and other static assets from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

var group = app.MapGroup("/api").WithOpenApi();

// Add Soldier
group.MapPost("/platoons/{platoon}/soldiers", (
    AttendenceApi.Platoon platoon,
    AttendenceApi.Soldier soldier,
    AttendenceApi.Company company) =>
{
    if (!company.AddSoldier(platoon, soldier))
    {
        return Results.Conflict($"A soldier with id {soldier.Id} already exists.");
    }
    return Results.Created($"/api/platoons/{platoon}/soldiers/{soldier.Id}", soldier);
}).WithName("AddSoldier").WithOpenApi();

// Remove Soldier
group.MapDelete("/platoons/{platoon}/soldiers/{id:int}", (
    AttendenceApi.Platoon platoon,
    int id,
    AttendenceApi.Company company) =>
{
    company.RemoveSoldier(platoon, id);
    return Results.NoContent();
}).WithName("RemoveSoldier").WithOpenApi();

// Set Soldier Status
group.MapPut("/platoons/{platoon}/soldiers/{id:int}/status", (
    AttendenceApi.Platoon platoon,
    int id,
    SetSoldierStatusRequest request,
    AttendenceApi.Company company) =>
{
    company.SetSoldierStatus(platoon, id, request.Status, request.Location, request.Notes);
    return Results.NoContent();
}).WithName("SetSoldierStatus").WithOpenApi();

// Get platoon
group.MapGet("/platoons/{platoon}", (
    AttendenceApi.Platoon platoon,
    AttendenceApi.Company company) =>
{
    return Results.Ok(company.GetPlatoon(platoon));
}).WithName("GetPlatoon").WithOpenApi();

// Get entire company
group.MapGet("/company", (AttendenceApi.Company company) =>
{
    return Results.Ok(company.GetCompany());
}).WithName("GetCompany").WithOpenApi();

// Fallback to index.html for client-side routes
app.MapFallbackToFile("index.html");

app.Run();

// DTOs must come after top-level statements to avoid compilation errors.
public record SetSoldierStatusRequest(AttendenceApi.Status Status, string Location, string? Notes);
