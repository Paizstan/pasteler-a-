using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemadePasteleria.Models;

public class VolumenVentasDocument : IDocument
{
    private List<VolumenVentasModel> _datos;
    private string _filtro;

    public VolumenVentasDocument(List<VolumenVentasModel> datos, string filtro)
    {
        _datos = datos;
        _filtro = filtro;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Header().Text($"Reporte de Volumen de Ventas por {_filtro.ToUpper()}").FontSize(20).Bold();
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Fecha").Bold();
                    header.Cell().Text("Pedidos").Bold();
                    header.Cell().Text("Total Vendido").Bold();
                });

                foreach (var item in _datos)
                {
                    table.Cell().Text(item.Fecha.ToString("dd/MM/yyyy"));
                    table.Cell().Text(item.TotalPedidos.ToString());
                    table.Cell().Text($"${item.TotalVentas:F2}");
                }
            });
        });
    }

    public byte[] GeneratePdf()
    {
        using var stream = new MemoryStream();
        this.GeneratePdf(stream);
        return stream.ToArray();
    }
}
