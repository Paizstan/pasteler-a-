using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemadePasteleria.Models;

namespace SistemadePasteleria.Controllers
{
    public class ReportesController : Controller
    {
        private readonly PasteldbContext _context;

        public ReportesController(PasteldbContext context)
        {
            _context = context;
        }

      

    }
}
