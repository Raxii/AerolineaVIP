using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    public class ReportesController : Controller
    {
        //#PLANTILLA #21
        public ActionResult Index()
        {
            var db = new AerolineaVIP_Context();

            var vuelosTotales = 0;
            var reservasConfirmadas = 0;
            var cancelaciones = 0;
            var ocupadosTotales = 0;
            var capacidadTotal = 0;

            var rutas = new List<string>();
            var rutasDict = new Dictionary<string, int>();
            var rutasOcupacion = new Dictionary<string, int>();
            var rutasOcupados = new Dictionary<string, int>();
            var rutasCapacidad = new Dictionary<string, int>();

            var aeronaves = new List<Aeronave>();
            var estados = new List<string>();
            var reporteRutas = new List<dynamic>();

            try
            {
                // Cargar todas las aeronaves
                foreach (var aeronave in db.Aeronave)
                {
                    aeronaves.Add(aeronave);
                }

                // Contar vuelos y extraer data
                foreach (var vuelo in db.Vuelo)
                {
                    vuelosTotales++;

                    // Extraer ruta
                    var rutaDisplay = "N/A";
                    try
                    {
                        if (vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
                        {
                            var origenCodigo = "N/A";
                            var destinoCodigo = "N/A";

                            try
                            {
                                if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
                                    origenCodigo = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA ?? "N/A";
                            }
                            catch { }

                            try
                            {
                                if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
                                    destinoCodigo = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA ?? "N/A";
                            }
                            catch { }

                            rutaDisplay = origenCodigo + " → " + destinoCodigo;
                        }
                    }
                    catch { }

                    // Agregar a lista de rutas si no existe
                    if (!rutas.Contains(rutaDisplay))
                        rutas.Add(rutaDisplay);

                    // Contar por ruta
                    if (!rutasDict.ContainsKey(rutaDisplay))
                    {
                        rutasDict[rutaDisplay] = 0;
                        rutasOcupados[rutaDisplay] = 0;
                        rutasCapacidad[rutaDisplay] = 0;
                    }
                    rutasDict[rutaDisplay]++;

                    // Extraer estado
                    var estadoVuelo = "Desconocido";
                    try
                    {
                        if (vuelo.EstadoVuelo != null)
                            estadoVuelo = vuelo.EstadoVuelo.Descripcion ?? "Desconocido";
                    }
                    catch { }

                    if (!estados.Contains(estadoVuelo))
                        estados.Add(estadoVuelo);

                    if (estadoVuelo == "Cancelado")
                        cancelaciones++;

                    // Contar capacidad y ocupación
                    var capacidadVuelo = 0;
                    var ocupadosVuelo = 0;

                    try
                    {
                        if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                        {
                            capacidadVuelo = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;
                            capacidadTotal += capacidadVuelo;
                            rutasCapacidad[rutaDisplay] += capacidadVuelo;
                        }
                    }
                    catch { }

                    // Contar ocupados (reservas confirmadas para este vuelo)
                    try
                    {
                        foreach (var reserva in db.ReservaPasajeroVuelo)
                        {
                            if (reserva.VueloId == vuelo.VueloId)
                            {
                                ocupadosVuelo++;
                                ocupadosTotales++;
                                rutasOcupados[rutaDisplay]++;
                            }
                        }
                    }
                    catch { }
                }

                // Contar reservas confirmadas
                foreach (var reserva in db.ReservaPasajeroVuelo)
                {
                    reservasConfirmadas++;
                }

                // Construir reporte por ruta
                foreach (var rutaKey in rutasDict.Keys)
                {
                    var vuelosRuta = rutasDict[rutaKey];
                    var ocupadosRuta = rutasOcupados[rutaKey];
                    var capacidadRuta = rutasCapacidad[rutaKey];
                    var ocupacionRuta = capacidadRuta > 0 ? (ocupadosRuta * 100) / capacidadRuta : 0;

                    // Calcular ingresos estimados (asientos vendidos * precio promedio)
                    var ingresosRuta = ocupadosRuta * 100; // Precio promedio estimado

                    reporteRutas.Add(new
                    {
                        ruta = rutaKey,
                        vuelos = vuelosRuta,
                        pasajeros = ocupadosRuta,
                        ocupacion = ocupacionRuta,
                        ingresos = "$" + ingresosRuta,
                        estado = "Consolidado"
                    });
                }
            }
            catch { }

            // Calcular ocupación promedio
            var ocupacionPromedio = capacidadTotal > 0 ? (ocupadosTotales * 100) / capacidadTotal : 0;

            // Pasar datos a la vista
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