namespace MinimalApi.Dominio.ModelViews;

public struct Home
{
    public string Mensagem { get => "Bem-vindo à API Minimalista!"; }
    public string Doc { get => "/swagger"; }
}