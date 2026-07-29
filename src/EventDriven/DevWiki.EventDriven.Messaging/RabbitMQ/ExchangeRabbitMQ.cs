using System.Data;

namespace DevWiki.EventDriven.Messaging.RabbitMQ;

public sealed class ExchangeRabbitMQ
{
    private readonly Dictionary<string, FilaRabbitMQ> _bindings = [];

    public string Nome { get; }
    public TipoExchange Tipo { get; }

    public ExchangeRabbitMQ(string nome, TipoExchange tipo)
    {
        Nome = nome;
        Tipo = tipo;
    }

    public void Bind(string bindingKey, FilaRabbitMQ fila) => _bindings[bindingKey] = fila;

    public void Publicar(RabbitMQMensagem mensagem)
    {
        var filas = Tipo switch
        {
            TipoExchange.Fanout => _bindings.Values,
            TipoExchange.Direct => _bindings
                .Where(b => b.Key == mensagem.RoutingKey)
                .Select(b => b.Value),
            TipoExchange.Topic => _bindings
                .Where(b => MatchTopic(b.Key.Split('.'), 0, mensagem.RoutingKey.Split('.'), 0))
                .Select(b => b.Value),
            _ => []
        };
    }

    //algoritmo recursivo de wildcard matching p topic exchange
    //pi = indice no array padrao, ki = indici na routing key

    //ambos esgotados -> match completo
    //so o pdrao esgotado -> sem match meu nnobre
    //so a key esgotada -> match se restam apenas '#' no padrao

    //recursao:
    //#: tenta consumir 0, 1, 2... palavras da key
    //*: consome exatamente uma palavra da key
    //literal: deve casar exatamente com a palavra da key
    private static bool MatchTopic(string[] pattern, int pi, string[] key, int ki)
    {
        if (pi == pattern.Length && ki == key.Length) return true;
        if (pi == pattern.Length) return false;
        if (ki == key.Length)
            return pattern[pi] == "#" && MatchTopic(pattern, pi + 1, key, ki);

        if (pattern[pi] == "#")
        {
            // # pode consumir 0 ou mais palavras: tenta cada possibilidade
            for (var skip = 0; skip <= key.Length - ki; skip++)
                if (MatchTopic(pattern, pi + 1, key, ki + skip))
                    return true;
            return false;
        }

        if (pattern[pi] == "*")
            return MatchTopic(pattern, pi + 1, key, ki + 1);

        return pattern[pi] == key[ki] && MatchTopic(pattern, pi + 1, key, ki + 1);
    }
}