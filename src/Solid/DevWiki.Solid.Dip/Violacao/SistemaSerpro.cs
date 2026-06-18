namespace DevWiki.Solid.Dip.Violacao;

public class SistemaSerpro
{
    private const string _endpointFake = "https://gateway.serpro.gov.br/consulta-cpf/v1/consulta";

    public async Task<bool> AnalisarRiscoAsync(string identificador, decimal valor)
    {
        await Task.Delay(20);

        Console.WriteLine($"[serpro] analisando risco: {identificador}, valor r$ {valor:F2}");
        return true;
    }
}
