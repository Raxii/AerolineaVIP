
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class ReportesController : Controller
    {
        // PLANTILLA #21
        public ActionResult Index(
            string desde,
            string hasta,
            string ruta,
            int? aeronaveId,
            string estado,
            string agrupar)
        {
            // ==========================================
            // 1. VARIABLES GENERALES
            // ==========================================

            int vuelosTotales = 0;
            int reservasConfirmadas = 0;
            int cancelaciones = 0;
            int ocupadosTotales = 0;
            int capacidadTotal = 0;
            int ocupacionPromedio = 0;

            // ==========================================
            // 2. COLECCIONES PARA LA VISTA
            // ==========================================

            var rutas = new List<string>();
            var estados = new List<string>();
            var aeronaves = new List<Aeronave>();

            var reporteRutas = new List<ReporteRuta>();

            // ==========================================
            // 3. COLECCIONES AUXILIARES
            // ==========================================

            var vuelos = new List<Vuelo>();
            var vuelosFiltrados = new List<Vuelo>();

            var asignacionesPorVuelo =
                new Dictionary<int, List<ReservaPasajeroVuelo>>();

            var estadoReservaPorId =
                new Dictionary<int, string>();

            var reservasContabilizadas = new HashSet<int>();

            var grupos = new Dictionary<string, ReporteRuta>();
            var capacidadPorGrupo = new Dictionary<string, int>();

            // ==========================================
            // 4. VALIDAR AGRUPACION
            // ==========================================

            if (agrupar != "Ruta" &&
                agrupar != "Dia" &&
                agrupar != "Mes" &&
                agrupar != "Aeronave")
            {
                agrupar = "Ruta";
            }

            // ==========================================
            // 5. VALIDAR AERONAVE
            // ==========================================

            if (aeronaveId.HasValue && aeronaveId.Value <= 0)
            {
                aeronaveId = null;
            }

            // ==========================================
            // 6. VALIDAR FECHAS
            // ==========================================

            DateTime fechaDesde = DateTime.MinValue;
            DateTime fechaHasta = DateTime.MinValue;

            bool tieneDesde =
                !String.IsNullOrWhiteSpace(desde);

            bool tieneHasta =
                !String.IsNullOrWhiteSpace(hasta);

            bool fechaDesdeValida = !tieneDesde ||
                DateTime.TryParseExact(
                    desde,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out fechaDesde
                );

            bool fechaHastaValida = !tieneHasta ||
                DateTime.TryParseExact(
                    hasta,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out fechaHasta
                );

            bool fechasValidas =
                fechaDesdeValida && fechaHastaValida;

            if (!fechasValidas)
            {
                ViewBag.ErrorReporte =
                    "Una de las fechas ingresadas no es válida.";
            }

            if (fechasValidas &&
                tieneDesde &&
                tieneHasta &&
                fechaDesde.Date > fechaHasta.Date)
            {
                fechasValidas = false;

                ViewBag.ErrorReporte =
                    "La fecha inicial no puede ser posterior a la fecha final.";
            }

            // ==========================================
            // 7. CONSULTA Y PROCESAMIENTO
            // ==========================================

            using (var db = new AerolineaVIP_Context())
            {
                try
                {
                    // ----------------------------------
                    // CARGAR AERONAVES
                    // ----------------------------------

                    foreach (var aeronave in db.Aeronave)
                    {
                        aeronaves.Add(aeronave);
                    }

                    // ----------------------------------
                    // CARGAR VUELOS
                    // ----------------------------------

                    foreach (var vuelo in db.Vuelo)
                    {
                        vuelos.Add(vuelo);

                        // Obtener rutas disponibles.
                        string rutaDisplay =
                            ObtenerRutaDisplay(vuelo);

                        if (!rutas.Contains(rutaDisplay))
                        {
                            rutas.Add(rutaDisplay);
                        }

                        // Obtener estados disponibles.
                        string estadoDisplay =
                            ObtenerEstadoDisplay(vuelo);

                        if (!estados.Contains(estadoDisplay))
                        {
                            estados.Add(estadoDisplay);
                        }
                    }

                    // ----------------------------------
                    // CARGAR ASIGNACIONES
                    // ----------------------------------

                    foreach (var asignacion in db.ReservaPasajeroVuelo)
                    {
                        if (!asignacionesPorVuelo.ContainsKey(
                            asignacion.VueloId))
                        {
                            asignacionesPorVuelo[asignacion.VueloId] =
                                new List<ReservaPasajeroVuelo>();
                        }

                        asignacionesPorVuelo[asignacion.VueloId]
                            .Add(asignacion);
                    }

                    // ----------------------------------
                    // CARGAR ESTADOS DE RESERVAS
                    // ----------------------------------

                    foreach (var reserva in db.Reserva)
                    {
                        estadoReservaPorId[reserva.ReservaId] =
                            reserva.EstadoReservaCodigo;
                    }

                    // ==================================
                    // 8. APLICAR FILTROS
                    // ==================================

                    foreach (var vuelo in vuelos)
                    {
                        bool cumpleFiltros = true;

                        string rutaDisplay =
                            ObtenerRutaDisplay(vuelo);

                        string estadoDisplay =
                            ObtenerEstadoDisplay(vuelo);

                        // FILTRO POR FECHA

                        if (tieneDesde || tieneHasta)
                        {
                            DateTime fechaVuelo =
                                vuelo.FechaOperacion.Date;

                            if (tieneDesde &&
                                fechaVuelo < fechaDesde.Date)
                            {
                                cumpleFiltros = false;
                            }

                            if (tieneHasta &&
                                fechaVuelo > fechaHasta.Date)
                            {
                                cumpleFiltros = false;
                            }
                        }

                        // FILTRO POR RUTA

                        if (!String.IsNullOrWhiteSpace(ruta))
                        {
                            if (rutaDisplay != ruta)
                            {
                                cumpleFiltros = false;
                            }
                        }

                        // FILTRO POR AERONAVE

                        if (aeronaveId.HasValue)
                        {
                            if (!vuelo.AeronaveId.HasValue ||
                                vuelo.AeronaveId.Value != aeronaveId.Value)
                            {
                                cumpleFiltros = false;
                            }
                        }

                        // FILTRO POR ESTADO

                        if (!String.IsNullOrWhiteSpace(estado))
                        {
                            if (estadoDisplay != estado)
                            {
                                cumpleFiltros = false;
                            }
                        }

                        // VALIDACION DE FECHAS

                        if (!fechasValidas)
                        {
                            cumpleFiltros = false;
                        }

                        if (cumpleFiltros)
                        {
                            vuelosFiltrados.Add(vuelo);
                        }
                    }

                    // ==================================
                    // 9. PROCESAR VUELOS FILTRADOS
                    // ==================================

                    foreach (var vuelo in vuelosFiltrados)
                    {
                        vuelosTotales++;

                        // Determinar agrupación.
                        string grupo =
                            ObtenerGrupo(vuelo, agrupar);

                        // Crear grupo si no existe.
                        if (!grupos.ContainsKey(grupo))
                        {
                            grupos[grupo] = new ReporteRuta
                            {
                                Ruta = grupo,
                                Vuelos = 0,
                                Pasajeros = 0,
                                Ocupacion = 0,
                                Ingresos = 0,
                                Estado = "Consolidado"
                            };

                            capacidadPorGrupo[grupo] = 0;
                        }

                        grupos[grupo].Vuelos++;

                        // CONTAR CANCELACIONES

                        if (vuelo.EstadoVueloCodigo == "CANCELADO")
                        {
                            cancelaciones++;
                        }

                        // CAPACIDAD DE LA AERONAVE

                        int capacidadVuelo = 0;

                        if (vuelo.Aeronave != null &&
                            vuelo.Aeronave.ModeloAeronave != null)
                        {
                            capacidadVuelo =
                                vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;
                        }

                        capacidadTotal += capacidadVuelo;

                        capacidadPorGrupo[grupo] += capacidadVuelo;

                        // CONTAR ASIGNACIONES DE ASIENTOS

                        if (asignacionesPorVuelo.ContainsKey(vuelo.VueloId))
                        {
                            foreach (var asignacion in
                                asignacionesPorVuelo[vuelo.VueloId])
                            {
                                if (asignacion.EstadoAsignacionCodigo == "PENDIENTE" ||
                                    asignacion.EstadoAsignacionCodigo == "CONFIRMADA")
                                {
                                    ocupadosTotales++;

                                    grupos[grupo].Pasajeros++;

                                    // CONTAR RESERVAS CONFIRMADAS
                                    // SIN DUPLICAR UNA MISMA RESERVA.

                                    if (estadoReservaPorId.ContainsKey(
                                        asignacion.ReservaId))
                                    {
                                        string estadoReserva =
                                            estadoReservaPorId[asignacion.ReservaId];

                                        if (estadoReserva == "CONFIRMADA")
                                        {
                                            reservasContabilizadas.Add(
                                                asignacion.ReservaId);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // ==================================
                    // 10. TOTAL DE RESERVAS CONFIRMADAS
                    // ==================================

                    reservasConfirmadas =
                        reservasContabilizadas.Count;

                    // ==================================
                    // 11. CONSTRUIR REPORTE AGRUPADO
                    // ==================================

                    foreach (var grupoKey in grupos.Keys)
                    {
                        var reporte = grupos[grupoKey];

                        int capacidadGrupo =
                            capacidadPorGrupo[grupoKey];

                        // CALCULAR OCUPACION

                        reporte.Ocupacion = capacidadGrupo > 0
                            ? (reporte.Pasajeros * 100) / capacidadGrupo
                            : 0;

                        // INGRESOS SIMULADOS

                        reporte.Ingresos =
                            reporte.Pasajeros * 100m;

                        reporteRutas.Add(reporte);
                    }
                }
                catch (Exception)
                {
                    // EVITAR MOSTRAR RESULTADOS PARCIALES.

                    vuelosTotales = 0;
                    reservasConfirmadas = 0;
                    cancelaciones = 0;
                    ocupadosTotales = 0;
                    capacidadTotal = 0;

                    reporteRutas.Clear();

                    ViewBag.ErrorReporte =
                        "No se pudo completar la consulta de reportes.";
                }
            }

            // ==========================================
            // 12. CALCULAR OCUPACION GENERAL
            // ==========================================

            ocupacionPromedio = capacidadTotal > 0
                ? (int)Math.Round(
                    ocupadosTotales * 100.0 / capacidadTotal
                )
                : 0;

            // ==========================================
            // 13. ENVIAR INFORMACION A LA VISTA
            // ==========================================

            ViewBag.VuelosTotales = vuelosTotales;
            ViewBag.OcupacionPromedio = ocupacionPromedio;
            ViewBag.ReservasConfirmadas = reservasConfirmadas;
            ViewBag.Cancelaciones = cancelaciones;

            ViewBag.Rutas = rutas;
            ViewBag.Aeronaves = aeronaves;
            ViewBag.Estados = estados;
            ViewBag.ReporteRutas = reporteRutas;

            // CONSERVAR FILTROS SELECCIONADOS.

            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            ViewBag.RutaSeleccionada = ruta;
            ViewBag.AeronaveSeleccionada = aeronaveId;
            ViewBag.EstadoSeleccionado = estado;
            ViewBag.AgruparSeleccionado = agrupar;

            return View();
        }

        // Exporta el mismo resumen que se muestra en pantalla, respetando los filtros.
        [HttpGet]
        public ActionResult ExportarCSV(string desde, string hasta, string ruta,
            int? aeronaveId, string estado, string agrupar)
        {
            var resultado = Index(desde, hasta, ruta, aeronaveId, estado, agrupar) as ViewResult;
            if (resultado == null)
                return new HttpStatusCodeResult(500, "No se pudo generar el reporte.");

            var filas = resultado.ViewData["ReporteRutas"] as List<ReporteRuta>;
            if (filas == null) filas = new List<ReporteRuta>();

            var csv = new StringBuilder();
            csv.AppendLine("Grupo;Vuelos;Pasajeros;Ocupacion (%);Ingresos;Estado");
            foreach (var fila in filas)
            {
                string grupo = (fila.Ruta ?? "").Replace(";", ",");
                string estadoFila = (fila.Estado ?? "").Replace(";", ",");
                csv.AppendLine(string.Join(";", new string[] {
                    grupo,
                    fila.Vuelos.ToString(CultureInfo.InvariantCulture),
                    fila.Pasajeros.ToString(CultureInfo.InvariantCulture),
                    fila.Ocupacion.ToString("0.##", CultureInfo.InvariantCulture),
                    fila.Ingresos.ToString("0.00", CultureInfo.InvariantCulture),
                    estadoFila
                }));
            }

            byte[] contenido = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(contenido, "text/csv", "Reporte_AerolineaVIP_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv");
        }

        // ==========================================
        // METODO: OBTENER RUTA
        // ==========================================

        private string ObtenerRutaDisplay(Vuelo vuelo)
        {
            string origen = "N/A";
            string destino = "N/A";

            if (vuelo.Frecuencia != null &&
                vuelo.Frecuencia.Ruta != null)
            {
                if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
                {
                    origen =
                        vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA
                        ?? "N/A";
                }

                if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
                {
                    destino =
                        vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA
                        ?? "N/A";
                }
            }

            return origen + " → " + destino;
        }

        // ==========================================
        // METODO: OBTENER ESTADO
        // ==========================================

        private string ObtenerEstadoDisplay(Vuelo vuelo)
        {
            if (vuelo.EstadoVuelo != null)
            {
                return vuelo.EstadoVuelo.Descripcion
                    ?? "Desconocido";
            }

            return "Desconocido";
        }

        // ==========================================
        // METODO: DETERMINAR AGRUPACION
        // ==========================================

        private string ObtenerGrupo(Vuelo vuelo, string agrupar)
        {
            if (agrupar == "Dia")
            {
                return vuelo.FechaOperacion.ToString("dd/MM/yyyy");
            }

            if (agrupar == "Mes")
            {
                return vuelo.FechaOperacion.ToString(
                    "MMMM yyyy",
                    new CultureInfo("es-EC")
                );
            }

            if (agrupar == "Aeronave")
            {
                if (vuelo.Aeronave != null)
                {
                    return vuelo.Aeronave.Matricula
                        ?? "Sin matrícula";
                }

                return "Sin aeronave";
            }

            return ObtenerRutaDisplay(vuelo);
        }
    }
}
