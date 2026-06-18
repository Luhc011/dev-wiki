namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IExportadorCobranca
{
    Task<string> ExportarCsvAsync();
}
