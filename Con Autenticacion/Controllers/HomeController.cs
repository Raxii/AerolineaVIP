using System.Collections.Generic;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    public class HomeController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        public ActionResult Index()
        {
            var origenes = new List<SelectListItem>();
            var destinos = new List<SelectListItem>();

            foreach (var aeropuerto in db.Aeropuerto)
            {
                if (aeropuerto.Activo)
                {
                    var item = new SelectListItem
                    {
                        Value = aeropuerto.AeropuertoId.ToString(),
                        Text = aeropuerto.Nombre + " (" + aeropuerto.CodigoIATA + ")"
                    };

                    origenes.Add(item);
                    destinos.Add(item);
                }
            }

            ViewBag.Origenes = origenes;
            ViewBag.Destinos = destinos;

            return View(db.Vuelo);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}
