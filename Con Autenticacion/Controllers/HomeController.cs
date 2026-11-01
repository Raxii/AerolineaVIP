using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    public class HomeController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // Pantalla principal: presenta la aerolínea y da acceso al formulario
        // de búsqueda de vuelos (Vueloes/Buscar). NO utiliza operadores LINQ
        // de consulta (.Where/.Select/.OrderBy/.Include/.FirstOrDefault) ni
        // SQL crudo: recorre los DbSet con foreach/if, igual que el resto del
        // proyecto (CrudController, ReportesController, VueloesController).
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
                        Text = aeropuerto.Nombre + " (" + aeropuerto.CodigoIATA + ") - " + aeropuerto.Localidad
                    };

                    origenes.Add(item);
                    destinos.Add(item);
                }
            }

            // Destinos reales y disponibilidad: se obtienen de Vuelo -> Frecuencia
            // -> Ruta -> Aeropuerto1 (destino), sin inventar datos y sin
            // duplicar la lógica completa de disponibilidad de asientos que
            // ya vive en VueloesController.Buscar(). Solo se excluyen los
            // vuelos CANCELADO/COMPLETADO y los que ya pasaron de fecha,
            // igual criterio que usa Buscar().
            var destinosPopulares = new List<Aeropuerto>();
            var idsDestinosAgregados = new HashSet<int>();
            var vuelosDisponibles = 0;
            var hoy = DateTime.Today;

            foreach (var vuelo in db.Vuelo)
            {
                if (vuelo.EstadoVueloCodigo == "CANCELADO" || vuelo.EstadoVueloCodigo == "COMPLETADO")
                    continue;

                if (vuelo.FechaOperacion.Date < hoy)
                    continue;

                vuelosDisponibles++;

                // Solo se recolectan algunos destinos distintos para la
                // sección "Destinos Populares"; el conteo de arriba sigue
                // recorriendo todos los vuelos para que sea un dato real.
                if (destinosPopulares.Count >= 6)
                    continue;

                if (vuelo.Frecuencia == null || vuelo.Frecuencia.Ruta == null ||
                    vuelo.Frecuencia.Ruta.Aeropuerto1 == null)
                    continue;

                var destino = vuelo.Frecuencia.Ruta.Aeropuerto1;
                if (!destino.Activo || idsDestinosAgregados.Contains(destino.AeropuertoId))
                    continue;

                idsDestinosAgregados.Add(destino.AeropuertoId);
                destinosPopulares.Add(destino);
            }

            ViewBag.Origenes = origenes;
            ViewBag.Destinos = destinos;
            ViewBag.DestinosPopulares = destinosPopulares;
            ViewBag.VuelosDisponibles = vuelosDisponibles;

            return View();
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
