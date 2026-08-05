using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using TCC_SistemaEmpresa.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// RNF37: autenticação obrigatória. O filtro global exige usuário logado em TODA
// action; o que for público precisa de [AllowAnonymous] explícito (ex.: a tela de login).
builder.Services.AddControllersWithViews(options =>
{
    var politica = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(politica));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AcessoNegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.Name = "LOSolutions.Auth";
        options.Cookie.HttpOnly = true;          // bloqueia leitura do cookie via JavaScript
        options.Cookie.SameSite = SameSiteMode.Lax;
        // SameAsRequest permite rodar no perfil "http" do launchSettings durante o
        // desenvolvimento. Em produção (só HTTPS), trocar para CookieSecurePolicy.Always.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cultura fixa em pt-BR: sem isso o parse de dinheiro ("1.234,56") e a formatação
// de datas dependeriam da configuração regional da máquina que hospeda a aplicação.
var culturaPtBr = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = culturaPtBr,
    SupportedUICultures = culturaPtBr
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// UseAuthentication SEMPRE antes de UseAuthorization: sem isso o cookie não é lido
// e o usuário aparece como anônimo mesmo logado.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();