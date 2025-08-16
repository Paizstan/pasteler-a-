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
            page.Size(PageSizes.A4);

            page.Header().Text($"🍰 Reporte de Volumen de Ventas por {_filtro.ToUpper()}")
                .FontSize(20).Bold().FontColor(Colors.Brown.Darken2);

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                // Encabezado con fondo gris
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("Fecha").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("Pedidos").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("Total Vendido").Bold();
                });

                // Filas con estilo (colores alternados)
                bool alt = false;
                foreach (var item in _datos)
                {
                    var bg = alt ? Colors.Grey.Lighten5 : Colors.White;
                    alt = !alt;

                    var fechaTexto = _filtro == "mes"
                        ? item.Fecha.ToString("MM/yyyy")
                        : item.Fecha.ToString("dd/MM/yyyy");

                    table.Cell().Background(bg).Padding(6).Text(fechaTexto);
                    table.Cell().Background(bg).Padding(6).AlignMiddle().AlignCenter().Text(item.TotalPedidos.ToString());
                    table.Cell().Background(bg).Padding(6).AlignRight().Text($"${item.TotalVentas:F2}");
                }
            });

            page.Footer().AlignCenter()
                .Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                .FontSize(9).FontColor(Colors.Grey.Medium);
        });
    }

    public byte[] GeneratePdf()
    {
        using var stream = new MemoryStream();
        this.GeneratePdf(stream);
        return stream.ToArray();
    }
}
