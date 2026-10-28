
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class ReportesController : Controller
    {
        // PLANTILLA #21
        public ActionResult Index()
        {
            var vuelosTotales = 0;
            var reservasConfirmadas = 0;
            var cancelaciones = 0;
            var ocupadosTotales = 0;
            var capacidadTotal = 0;

            // Se conservan los diccionarios de Israel.
            var rutas = new List<string>();
            var rutasDict = new Dictionary<string, int>();
            var rutasOcupados = new Dictionary<string, int>();
            var rutasCapacidad = new Dictionary<string, int>();

            var aeronaves = new List<Aeronave>();
            var estados = new List<string>();

            // CORRECCIÓN:
            // Usar el modelo ReporteRuta en lugar de dynamic.
            var reporteRutas = new List<ReporteRuta>();

            using (var db = new AerolineaVIP_Context())
            {
                try
                {
                    // Cargar todas las aeronaves.
                    foreach (var aeronave in db.Aeronave)
                    {
                        aeronaves.Add(aeronave);
                    }

                    // Cargar una vez las asignaciones de asientos.
                    // Se evita consultar toda la tabla por cada vuelo.
                    var todasLasAsignaciones =
                        db.ReservaPasajeroVuelo.ToList();

                    // Contar vuelos y extraer datos.
                    foreach (var vuelo in db.Vuelo)
                    {
                        vuelosTotales++;

                        // Extraer ruta.
                        var rutaDisplay = "N/A";

                        try
                        {
                            if (vuelo.Frecuencia != null &&
                                vuelo.Frecuencia.Ruta != null)
                            {
                                var origenCodigo = "N/A";
                                var destinoCodigo = "N/A";

                                try
                                {
                                    if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
                                    {
                                        origenCodigo =
                                            vuelo.Frecuencia.Ruta
                                            .Aeropuerto.CodigoIATA ?? "N/A";
                                    }
                                }
                                catch { }

                                try
                                {
                                    if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
                                    {
                                        destinoCodigo =
                                            vuelo.Frecuencia.Ruta
                                            .Aeropuerto1.CodigoIATA ?? "N/A";
                                    }
                                }
                                catch { }

                                rutaDisplay =
                                    origenCodigo + " → " + destinoCodigo;
                            }
                        }
                        catch { }

                        // Agregar a lista de rutas si no existe.
                        if (!rutas.Contains(rutaDisplay))
                        {
                            rutas.Add(rutaDisplay);
                        }

                        // Contar vuelos por ruta.
                        if (!rutasDict.ContainsKey(rutaDisplay))
                        {
                            rutasDict[rutaDisplay] = 0;
                            rutasOcupados[rutaDisplay] = 0;
                            rutasCapacidad[rutaDisplay] = 0;
                        }

                        rutasDict[rutaDisplay]++;

                        // Extraer estado.
                        var estadoVuelo = "Desconocido";

                        try
                        {
                            if (vuelo.EstadoVuelo != null)
                            {
                                estadoVuelo =
                                    vuelo.EstadoVuelo.Descripcion
                                    ?? "Desconocido";
                            }
                        }
                        catch { }

                        if (!estados.Contains(estadoVuelo))
                        {
                            estados.Add(estadoVuelo);
                        }

                        // CORRECCIÓN:
                        // Comparar el código del estado,
                        // no su descripción visible.
                        if (vuelo.EstadoVueloCodigo == "CANCELADO")
                        {
                            cancelaciones++;
                        }

                        // Contar capacidad y ocupación.
                        var capacidadVuelo = 0;
                        var ocupadosVuelo = 0;

                        try
                        {
                            if (vuelo.Aeronave != null &&
                                vuelo.Aeronave.ModeloAeronave != null)
                            {
                                capacidadVuelo =
                                    vuelo.Aeronave.ModeloAeronave
                                    .CapacidadAsientos;

                                capacidadTotal += capacidadVuelo;

                                rutasCapacidad[rutaDisplay] +=
                                    capacidadVuelo;
                            }
                        }
                        catch { }

                        // Mantener el conteo original por vuelo,
                        // pero excluir asientos liberados o cancelados.
                        foreach (var reserva in todasLasAsignaciones)
                        {
                            if (reserva.VueloId == vuelo.VueloId &&
                                (reserva.EstadoAsignacionCodigo == "PENDIENTE" ||
                                 reserva.EstadoAsignacionCodigo == "CONFIRMADA"))
                            {
                                ocupadosVuelo++;
                                ocupadosTotales++;
                                rutasOcupados[rutaDisplay]++;
                            }
                        }
                    }

                    // CORRECCIÓN:
                    // Contar reservas confirmadas, no todas
                    // las asignaciones de pasajeros.
                    foreach (var reserva in db.Reserva)
                    {
                        if (reserva.EstadoReservaCodigo == "CONFIRMADA")
                        {
                            reservasConfirmadas++;
                        }
                    }

                    // Construir reporte por ruta.
                    // Se conserva el algoritmo de Israel.
                    foreach (var rutaKey in rutasDict.Keys)
                    {
                        var vuelosRuta = rutasDict[rutaKey];

                        var ocupadosRuta =
                            rutasOcupados[rutaKey];

                        var capacidadRuta =
                            rutasCapacidad[rutaKey];

                        var ocupacionRuta = capacidadRuta > 0
                            ? (ocupadosRuta * 100) / capacidadRuta
                            : 0;

                        // Se mantiene la estimación original:
                        // USD 100 por asiento contabilizado.
                        // NO representa ingresos reales.
                        decimal ingresosRuta = ocupadosRuta * 100m;

                        // CORRECCIÓN PRINCIPAL:
                        // Objeto público ReporteRuta en vez
                        // de un objeto anónimo.
                        reporteRutas.Add(new ReporteRuta
                        {
                            Ruta = rutaKey,
                            Vuelos = vuelosRuta,
                            Pasajeros = ocupadosRuta,
                            Ocupacion = ocupacionRuta,
                            Ingresos = ingresosRuta,
                            Estado = "Consolidado"
                        });
                    }
                }
                catch (Exception)
                {
                    ViewBag.ErrorReporte =
                        "No se pudo completar la consulta de reportes.";
                }
            }

            // Calcular ocupación promedio.
            var ocupacionPromedio = capacidadTotal > 0
                ? (ocupadosTotales * 100) / capacidadTotal
                : 0;

            // Pasar datos a la vista.
            // Se conservan los nombres originales del ViewBag.
            ViewBag.VuelosTotales = vuelosTotales;
            ViewBag.OcupacionPromedio = ocupacionPromedio;
            ViewBag.ReservasConfirmadas = reservasConfirmadas;
            ViewBag.Cancelaciones = cancelaciones;

            ViewBag.Rutas = rutas;
            ViewBag.Aeronaves = aeronaves;
            ViewBag.Estados = estados;
            ViewBag.ReporteRutas = reporteRutas;

            return View();
        }
    }
}
