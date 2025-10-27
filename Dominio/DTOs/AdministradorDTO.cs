using MinimalApi.Dominio.Enuns;

namespace MinimalApi.Dominio.DTOs
{
    public struct AdministradorDTO
    {
        public AdministradorDTO()
        {
        }

        public string Email { get; set; } = default!;
        public string Senha { get; set; } = default!;
        public Perfil? Perfil { get; set; } = default!;
    }
}