namespace SistemadePasteleria.Utilidades
{
    public class Paginacion
    {
        public int TotalRegistros { get; private set; }
        public int PaginaActual { get; private set; }
        public int RegistrosPagina { get; private set; }
        public int TotalPaginas { get; private set; }
        public int PaginaInicio { get; private set; }
        public int UltimaPagina { get; private set; }
        public string Controlador { get; private set; }
        public string Accion { get; private set; }
        public int Salto { get; private set; }

        public Paginacion() { }

        public Paginacion(int totalRegistros, int pagina, int registrosPagina = 10, string controlador = "Home", string accion = "Index")
        {
            int totalPaginas = (int)Math.Ceiling((decimal)totalRegistros / (decimal)registrosPagina);
            int paginaActual = (pagina < 1) ? 1 : pagina;
            int paginaInicio = paginaActual - 5;
            int ultimaPagina = paginaActual + 4;
            int salto = (paginaActual - 1) * registrosPagina;

            if (paginaInicio <= 0)
            {
                ultimaPagina = ultimaPagina - (paginaInicio - 1);
                paginaInicio = 1;
            }

            if (ultimaPagina > totalPaginas)
            {
                ultimaPagina = totalPaginas;
                if (ultimaPagina > 10)
                {
                    paginaInicio = ultimaPagina - 9;
                }
            }

            TotalRegistros = totalRegistros;
            PaginaActual = paginaActual;
            RegistrosPagina = registrosPagina;
            TotalPaginas = totalPaginas;
            PaginaInicio = paginaInicio;
            UltimaPagina = ultimaPagina;
            Controlador = controlador;
            Accion = accion;
            Salto = salto;
        }
    }
}
