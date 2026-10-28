
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    // Solo SuperUsuario y Administrador
    // pueden acceder al panel administrativo.
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class CrudController : Controller
    {
        private AerolineaVIP_Context db =
            new AerolineaVIP_Context();

        // ========================================
        // PLANTILLA #17
        // PANEL DE ADMINISTRACIÓN
        // ========================================

        // GET: Crud/Index
        public ActionResult Index()
        {
            DateTime hoy = DateTime.Today;

            // ========================================
            // 1. CONTAR VUELOS PROGRAMADOS
            // ========================================

            var vuelosProgramados = 0;
            var vuelosDelDia = new List<Vuelo>();

            // También se utiliza para incidencias.
            var incidenciasRecientes = new List<Vuelo>();

            // Consultar vuelos una sola vez.
            var todosLosVuelos = db.Vuelo.ToList();

            foreach (var vuelo in todosLosVuelos)
            {
                // Vuelos futuros o de hoy que todavía
                // se encuentran programados o confirmados.
                if (vuelo.FechaOperacion.Date >= hoy &&
                    (vuelo.EstadoVueloCodigo == "PROGRAMADO" ||
                     vuelo.EstadoVueloCodigo == "CONFIRMADO"))
                {
                    vuelosProgramados++;

                    // Obtener vuelos del día actual.
                    if (vuelo.FechaOperacion.Date == hoy)
                    {
                        vuelosDelDia.Add(vuelo);
                    }
                }

                // ========================================
                // 2. DETECTAR VUELOS CANCELADOS
                // ========================================

                // El catálogo actual utiliza CANCELADO.
                // No existen los códigos anteriores
                // RE, CA, CC y OP para EstadoVuelo.
                if (vuelo.EstadoVueloCodigo == "CANCELADO")
                {
                    incidenciasRecientes.Add(vuelo);
                }
            }

            // ========================================
            // 3. CONTAR AERONAVES
            // ========================================

            var aeronavesActivas = 0;
            var aeronavesEnMantenimiento = 0;

            var aeronavesRecientes = new List<Aeronave>();

            foreach (var aeronave in db.Aeronave)
            {
                aeronavesRecientes.Add(aeronave);

                if (aeronave.EstadoAeronave != null)
                {
                    string codigoEstado =
                        aeronave.EstadoAeronave.Codigo;

                    // Estado correcto de la base de datos.
                    if (codigoEstado == "DISPONIBLE")
                    {
                        aeronavesActivas++;
                    }
                    else if (codigoEstado == "NO_DISPONIBLE")
                    {
                        // Se conserva el nombre de esta
                        // variable para no romper la vista.
                        // Representa aeronaves no disponibles.
                        aeronavesEnMantenimiento++;
                    }
                }
            }

            // ========================================
            // 4. CONTAR RESERVAS DEL DÍA
            // ========================================

            var reservasDelDia = 0;

            foreach (var reserva in db.Reserva)
            {
                if (reserva.FechaCreacion.Date == hoy)
                {
                    reservasDelDia++;
                }
            }

            // ========================================
            // 5. CONTAR INCIDENCIAS
            // ========================================

            // En esta versión del catálogo, se utilizan
            // los vuelos CANCELADOS como incidencias.
            //
            // Conservamos el nombre de la variable
            // utilizado en el diseño de Israel.
            var incidenciasAbiertas =
                incidenciasRecientes.Count;

            // ========================================
            // 6. OBTENER LAS TRES MÁS RECIENTES
            // ========================================

            // Ordenar por fecha de operación descendente,
            // en lugar de depender del orden de SQL Server.
            var incidenciasLimitadas =
                incidenciasRecientes
                    .OrderByDescending(v => v.FechaOperacion)
                    .ThenByDescending(v => v.VueloId)
                    .Take(3)
                    .ToList();

            // ========================================
            // 7. ENVIAR INFORMACIÓN A LA VISTA
            // ========================================

            // Se conservan los nombres ViewBag
            // para mantener compatibilidad con
            // Views/Crud/Index.cshtml.

            ViewBag.VuelosProgramados =
                vuelosProgramados;

            ViewBag.AeronavesActivas =
                aeronavesActivas;

            ViewBag.AeronavesEnMantenimiento =
                aeronavesEnMantenimiento;

            ViewBag.ReservasDelDia =
                reservasDelDia;

            ViewBag.IncidenciasAbiertas =
                incidenciasAbiertas;

            ViewBag.IncidenciasRecientes =
                incidenciasLimitadas;

            // Información adicional disponible
            // para futuras mejoras de la vista.
            ViewBag.VuelosDelDia = vuelosDelDia.Count;

            ViewBag.TotalAeronaves =
                aeronavesRecientes.Count;

            return View();
        }

        // ========================================
        // 8. LIBERAR RECURSOS
        // ========================================

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
