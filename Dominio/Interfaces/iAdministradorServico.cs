using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.DTOs;

namespace MinimalApi.Dominio.Interfaces;

public interface IAdministradorServico
{
    Administrador? Login(LoginDTO loginDTO);
    List<Administrador> Todos(int? pagina);
    Administrador? BuscarPorId(int id);
    void Incluir(Administrador administrador);
}