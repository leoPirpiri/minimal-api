namespace MinimalApi.Dominio.ModelViews;

public struct ErrosDeValidacao
{
    public ErrosDeValidacao()
    {
    }

    public List<string> Mensagens { get; set; } = new List<string>();
}