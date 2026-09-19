using ISOCodex.Addressing;
using ISOCodex.Addressing.France;
using ISOCodex.Addressing.Ireland;
using ISOCodex.Addressing.Spain;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAddressing()
    .AddSpainAddressing()
    .AddFranceAddressing()
    .AddIrelandAddressing();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
