using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Servicos;
using MinimalApi.Infraestrutura.Db;
namespace Test.Domain.Servicos;

[TestClass]
public class AdministradorServicosTest
{
    private DbContexto CriarContextoDeTest()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var path = Path.GetFullPath(Path.Combine(assemblyPath ?? "", "..", "..", ".."));
        var builder = new ConfigurationBuilder()
            .SetBasePath(path ?? Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
        // var options = new DbContextOptionsBuilder<DbContexto>()
        //     .UseInMemoryDatab(databaseName: "TestDatabase")
        //     .Options;
        var configuration = builder.Build();

        // var connectionString = Configuration.GetConnectionString("mysql");
        // var options = new DbContextOptionsBuilder<DbContexto>()
        //                   .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
        //                   .Options;
        return new DbContexto(configuration);
    }
    [TestMethod]
    public void TestarSalvarAdministrador()
    {
        // Arrange
        var context = CriarContextoDeTest();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE administradores");

        var adm = new Administrador();
        adm.Email = "teste@teste.com";
        adm.Senha = "teste";
        adm.Perfil = "Admin";
        var administradorServico = new AdministradorServico(context);

        // Act
        administradorServico.Incluir(adm);

        // Assert
        Assert.AreEqual(1, administradorServico.Todos(1).Count());
    }
}
