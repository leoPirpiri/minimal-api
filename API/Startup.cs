using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.DTOs;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using MinimalApi.Infraestrutura.Db;
using MinimalApi.Dominio.Enuns;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
namespace MinimalApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {

        Configuration = configuration;
        Key = Configuration.GetSection("Jwt").ToString() ?? "";
    }

    public IConfiguration Configuration { get; set; } = default!;
    private string Key;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        services.AddAuthorization();

        services.AddScoped<IAdministradorServico, AdministradorServico>();
        services.AddScoped<IVeiculoServico, VeiculoServico>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "insira o token JWT aqui!"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{ }
        }
            });
        });

        services.AddDbContext<DbContexto>(Options =>
        {
            Options.UseMySql(
                Configuration.GetConnectionString("mysql"),
                ServerVersion.AutoDetect(Configuration.GetConnectionString("mysql"))
            );
        });

    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {

            #region Home Endpoint
            endpoints.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
            #endregion

            #region Administradores
            string GerarTokenJwt(Administrador administrador)
            {
                if (string.IsNullOrEmpty(Key)) return string.Empty;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
                var credencials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>()
                {
                    new Claim("Email", administrador.Email),
                    new Claim("Perfil", administrador.Perfil),
                    new Claim(ClaimTypes.Role, administrador.Perfil)
                };
                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: credencials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            AdministradorModelView modelarAdministrador(Administrador adm)
            {
                return new AdministradorModelView
                {
                    Id = adm.Id,
                    Email = adm.Email,
                    Senha = new string('*', adm.Senha.Length),
                    Perfil = adm.Perfil.ToString()
                };
            }

            endpoints.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
            {
                var administrador = administradorServico.Login(loginDTO);
                if (administrador != null)
                {
                    string token = GerarTokenJwt(administrador);
                    return Results.Ok(new AdministradorLogado
                    {
                        Email = administrador.Email,
                        Perfil = administrador.Perfil,
                        Token = token
                    });
                }
                return Results.Unauthorized();
            }).AllowAnonymous().WithTags("Administração");

            endpoints.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
            {
                var validacao = new ErrosDeValidacao { Mensagens = new List<string>() };
                if (string.IsNullOrEmpty(administradorDTO.Email))
                {
                    validacao.Mensagens.Add("O email é obrigatório.");
                }
                if (string.IsNullOrEmpty(administradorDTO.Senha))
                {
                    validacao.Mensagens.Add("A senha é obrigatória.");
                }
                if (administradorDTO.Perfil == null)
                {
                    validacao.Mensagens.Add("O perfil é obrigatório.");
                }
                if (validacao.Mensagens.Count > 0)
                {
                    return Results.BadRequest(validacao);
                }
                var administrador = new Administrador
                {
                    Email = administradorDTO.Email,
                    Senha = administradorDTO.Senha,
                    Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
                };

                administradorServico.Incluir(administrador);
                return Results.Created($"/administradores/{administrador.Id}", modelarAdministrador(administrador));
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = Perfil.Admin.ToString() })
            .WithTags("Administração");

            endpoints.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
            {
                var adms = new List<AdministradorModelView>();
                foreach (var adm in administradorServico.Todos(pagina))
                {
                    adms.Add(modelarAdministrador(adm));
                }
                return Results.Ok(adms);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = Perfil.Admin.ToString() })
            .WithTags("Administração");

            endpoints.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
            {
                var adm = administradorServico.BuscarPorId(id);
                if (adm == null) return Results.NotFound();
                return Results.Ok(modelarAdministrador(adm));
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = Perfil.Admin.ToString() })
            .WithTags("Administração");
            #endregion

            #region Veiculos
            ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
            {
                var validacao = new ErrosDeValidacao { Mensagens = new List<string>() };
                if (string.IsNullOrEmpty(veiculoDTO.Nome))
                {
                    validacao.Mensagens.Add("O nome do veículo é obrigatório.");
                }
                if (string.IsNullOrEmpty(veiculoDTO.Marca))
                {
                    validacao.Mensagens.Add("A marca do veículo é obrigatória.");
                }
                if (veiculoDTO.Ano <= 1950)
                {
                    validacao.Mensagens.Add("Veículo muito antigo.");
                }
                return validacao;
            }

            endpoints.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
            {
                var validacao = validaDTO(veiculoDTO);
                if (validacao.Mensagens.Count > 0) return Results.BadRequest(validacao);

                var veiculo = new Veiculo
                {
                    Nome = veiculoDTO.Nome,
                    Marca = veiculoDTO.Marca,
                    Ano = veiculoDTO.Ano
                };
                veiculoServico.Incluir(veiculo);
                return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Perfil.Admin},{Perfil.Editor}" })
            .WithTags("Veículos");

            endpoints.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
            {
                return Results.Ok(veiculoServico.Todos(pagina));
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Perfil.Admin},{Perfil.Editor}" })
            .WithTags("Veículos");

            endpoints.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
            {
                var veiculo = veiculoServico.BuscarPorId(id);
                return veiculo is not null ? Results.Ok(veiculo) : Results.NotFound();
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Perfil.Admin},{Perfil.Editor}" })
            .WithTags("Veículos");

            endpoints.MapPut("/veiculos/{id}", ([FromRoute] int id, [FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
            {
                var validacao = validaDTO(veiculoDTO);
                if (validacao.Mensagens.Count > 0)
                {
                    return Results.BadRequest(validacao);
                }
                var veiculo = veiculoServico.BuscarPorId(id);
                if (veiculo is null) return Results.NotFound();

                veiculo.Nome = veiculoDTO.Nome;
                veiculo.Marca = veiculoDTO.Marca;
                veiculo.Ano = veiculoDTO.Ano;

                veiculoServico.Atualizar(veiculo);
                return Results.Ok(veiculo);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = Perfil.Admin.ToString() })
            .WithTags("Veículos");

            endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
            {
                var veiculo = veiculoServico.BuscarPorId(id);
                if (veiculo is null) return Results.NotFound();

                veiculoServico.Apagar(veiculo);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute { Roles = Perfil.Admin.ToString() })
            .WithTags("Veículos");
            #endregion
        });
    }

}
