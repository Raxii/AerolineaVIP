using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    public class CrudController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        //PLANTILLA #17
        public ActionResult Index()
        {
            // Contar vuelos programados (de hoy en adelante)
            var vuelosProgramados = 0;
            var vuelosDelDia = new List<Vuelo>();
            foreach (var vuelo in db.Vuelo)
            {
                if (vuelo.FechaOperacion.Date >= DateTime.Now.Date)
                {
                    vuelosProgramados++;
                    if (vuelo.FechaOperacion.Date == DateTime.Now.Date)
                    {
                        vuelosDelDia.Add(vuelo);
                    }
                }
            }

            // Contar aeronaves activas vs en mantenimiento
            var aeronavesActivas = 0;
            var aeronavesEnMantenimiento = 0;
            var aeronavesRecientes = new List<Aeronave>();
            foreach (var aeronave in db.Aeronave)
            {
                if (aeronave.EstadoAeronave != null)
                {
                    if (aeronave.EstadoAeronave.Codigo == "A")  // Activa
                    {
                        aeronavesActivas++;
                    }
                    else if (aeronave.EstadoAeronave.Codigo == "M")  // Mantenimiento
                    {
                        aeronavesEnMantenimiento++;
                    }
                }
                aeronavesRecientes.Add(aeronave);
            }

            // Contar reservas del día actual
            var reservasDelDia = 0;
            var incidenciasRecientes = new List<Vuelo>();
            foreach (var reserva in db.Reserva)
            {
                if (reserva.FechaCreacion.Date == DateTime.Now.Date)
                {
                    reservasDelDia++;
                }
            }

            // Obtener vuelos con estados problemáticos como "incidencias"
            foreach (var vuelo in db.Vuelo)
            {
                if (vuelo.EstadoVuelo != null)
                {
                    if (vuelo.EstadoVuelo.Codigo == "RE" ||  // Retrasado
                        vuelo.EstadoVuelo.Codigo == "CA" ||  // Cancelado
                        vuelo.EstadoVuelo.Codigo == "CC")    // Cambio de aeronave
                    {
                        incidenciasRecientes.Add(vuelo);
                    }
                }
            }

            // Contar incidencias abiertas (no resueltas)
            var incidenciasAbiertas = 0;
            foreach (var incidencia in incidenciasRecientes)
            {
                if (incidencia.EstadoVuelo != null && incidencia.EstadoVuelo.Codigo != "OP")  // Si no está "Operado"
                {
                    incidenciasAbiertas++;
                }
            }

            // Limitar a las últimas 3 incidencias ordenadas por fecha
            var incidenciasLimitadas = new List<Vuelo>();
            var contador = 0;
            for (int i = incidenciasRecientes.Count - 1; i >= 0 && contador < 3; i--)
            {
                incidenciasLimitadas.Add(incidenciasRecientes[i]);
                contador++;
            }

            // Pasar datos al ViewBag
            ViewBag.VuelosProgramados = vuelosProgramados;
            ViewBag.AeronavesActivas = aeronavesActivas;
            ViewBag.AeronavesEnMantenimiento = aeronavesEnMantenimiento;
            ViewBag.ReservasDelDia = reservasDelDia;
            ViewBag.IncidenciasAbiertas = incidenciasAbiertas;
            ViewBag.IncidenciasRecientes = incidenciasLimitadas;

            return View();
        }

    }
}