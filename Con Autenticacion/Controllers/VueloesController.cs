using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    public class VueloesController : Controller
    {
        // Se conserva el contexto Database First que ya utiliza el proyecto.
        private readonly AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: /Vueloes o /Vueloes/Index
        // Corrige el 404: el controlador de Israel no tenía esta acción.
        public ActionResult Index()
        {
            var vuelos = db.Vuelo
                .Include(v => v.Aeronave)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .OrderByDescending(v => v.FechaOperacion)
                .ThenBy(v => v.VueloId)
                .ToList();

            return View(vuelos);
        }

        // PLANTILLA #5: Buscar vuelos por origen, destino, fecha y pasajeros.
        // Conserva los nombres de los parámetros y ViewBag del código de Israel.
        public ActionResult Buscar(int? AeropuertoOrigenId, int? AeropuertoDestinoId,
            DateTime? Fecha, int CantidadPasajeros = 1)
        {
            CantidadPasajeros = Math.Max(1, CantidadPasajeros);
            var ocupados = ObtenerAsientosOcupados();
            var resultados = ObtenerVuelosFiltrados(AeropuertoOrigenId,
                AeropuertoDestinoId, Fecha, CantidadPasajeros, ocupados);

            ViewBag.AeropuertoOrigenId = AeropuertoOrigenId;
            ViewBag.AeropuertoDestinoId = AeropuertoDestinoId;
            ViewBag.Fecha = Fecha;
            ViewBag.CantidadPasajeros = CantidadPasajeros;
            ViewBag.HayResultados = resultados.Count > 0;
            ViewBag.TotalResultados = resultados.Count;
            ViewBag.Aeropuertos = db.Aeropuerto
                .Where(a => a.Activo)
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new SelectListItem
                {
                    Value = a.AeropuertoId.ToString(),
                    Text = a.Localidad + " (" + a.CodigoIATA + ")"
                })
                .ToList();
            ViewBag.OcupadosPorVuelo = ObtenerConteos(ocupados);

            return View(resultados);
        }

        // PLANTILLA #6: Resultados con los mismos filtros que Buscar.
        public ActionResult Resultados(int? AeropuertoOrigenId, int? AeropuertoDestinoId,
            DateTime? Fecha, int CantidadPasajeros = 1)
        {
            CantidadPasajeros = Math.Max(1, CantidadPasajeros);
            var ocupados = ObtenerAsientosOcupados();
            var resultados = ObtenerVuelosFiltrados(AeropuertoOrigenId,
                AeropuertoDestinoId, Fecha, CantidadPasajeros, ocupados);

            var aeropuertos = db.Aeropuerto.Where(a => a.Activo).ToList();
            var origen = aeropuertos.FirstOrDefault(a => a.AeropuertoId == AeropuertoOrigenId);
            var destino = aeropuertos.FirstOrDefault(a => a.AeropuertoId == AeropuertoDestinoId);

            ViewBag.AeropuertoOrigen = origen == null ? "" : origen.Nombre + " (" + origen.CodigoIATA + ")";
            ViewBag.AeropuertoDestino = destino == null ? "" : destino.Nombre + " (" + destino.CodigoIATA + ")";
            ViewBag.Fecha = Fecha;
            ViewBag.CantidadPasajeros = CantidadPasajeros;
            ViewBag.Vuelos = resultados;
            ViewBag.HayResultados = resultados.Count > 0;
            ViewBag.TotalResultados = resultados.Count;
            ViewBag.OcupadosPorVuelo = ObtenerConteos(ocupados);

            return View(resultados);
        }

        // PLANTILLA #7: Consulta del estado de los asientos de un vuelo.
        public ActionResult DisponibilidadAsientos(int? vueloId)
        {
            if (!vueloId.HasValue) return RedirectToAction("Buscar");

            var vuelo = ObtenerVueloCompleto(vueloId.Value);
            if (vuelo == null) return HttpNotFound();

            PrepararDatosDeAsientos(vuelo, false);
            return View(vuelo);
        }

        // PLANTILLA #8: Pantalla de selección. No registra reservas por sí sola.
        public ActionResult Seleccionar(int? vueloId)
        {
            if (!vueloId.HasValue) return RedirectToAction("Buscar");

            var vuelo = ObtenerVueloCompleto(vueloId.Value);
            if (vuelo == null) return HttpNotFound();

            PrepararDatosDeAsientos(vuelo, true);
            return View(vuelo);
        }

        // PLANTILLA #18: Vista de administración elaborada por Israel.
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Administrar()
        {
            var vuelos = db.Vuelo
                .Include(v => v.Aeronave)
                .Include(v => v.Frecuencia)
                .OrderByDescending(v => v.FechaOperacion)
                .ToList();
            ViewBag.TotalVuelos = vuelos.Count;
            ViewBag.Vuelos = vuelos;
            return View();
        }

        // GET: /Vueloes/Details/5
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var vuelo = ObtenerVueloCompleto(id.Value);
            if (vuelo == null) return HttpNotFound();
            return View(vuelo);
        }

        // Los siguientes métodos recuperan el CRUD original, que estaba comentado.
        // Solo los roles administrativos pueden crear, modificar o eliminar vuelos.
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            CargarListas();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Vuelo.Add(vuelo);
                    db.SaveChanges();
                    TempData["Mensaje"] = "Vuelo registrado correctamente.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "No se pudo crear el vuelo. Comprueba fecha, frecuencia, aeronave y posibles conflictos de horario.");
                }
            }

            CargarListas(vuelo);
            return View(vuelo);
        }

        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var vuelo = db.Vuelo.Find(id.Value);
            if (vuelo == null) return HttpNotFound();
            CargarListas(vuelo);
            return View(vuelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo datos)
        {
            if (ModelState.IsValid)
            {
                var vuelo = db.Vuelo.Find(datos.VueloId);
                if (vuelo == null) return HttpNotFound();

                vuelo.FrecuenciaId = datos.FrecuenciaId;
                vuelo.FechaOperacion = datos.FechaOperacion;
                vuelo.AeronaveId = datos.AeronaveId;
                vuelo.EstadoVueloCodigo = datos.EstadoVueloCodigo;

                try
                {
                    db.SaveChanges();
                    TempData["Mensaje"] = "Vuelo actualizado correctamente.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "No se pudo actualizar. Comprueba fecha, frecuencia, aeronave y las reglas de la base de datos.");
                }
            }

            CargarListas(datos);
            return View(datos);
        }

        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var vuelo = ObtenerVueloCompleto(id.Value);
            if (vuelo == null) return HttpNotFound();
            return View(vuelo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            var vuelo = db.Vuelo.Find(id);
            if (vuelo == null) return HttpNotFound();

            try
            {
                db.Vuelo.Remove(vuelo);
                db.SaveChanges();
                TempData["Mensaje"] = "Vuelo eliminado correctamente.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se puede eliminar el vuelo porque podría tener reservas, itinerarios u otros registros asociados.");
                return View("Delete", vuelo);
            }
        }

        // Consultas auxiliares (sin modificar datos ni esquema de SQL Server).
        private Vuelo ObtenerVueloCompleto(int id)
        {
            return db.Vuelo
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.EstadoVuelo)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .FirstOrDefault(v => v.VueloId == id);
        }

        private Dictionary<int, HashSet<byte>> ObtenerAsientosOcupados()
        {
            // La restricción UNIQUE de la BD considera ocupadas las asignaciones
            // PENDIENTE y CONFIRMADA. Seguimos exactamente esa regla.
            // Una reserva vencida necesita ser LIBERADA mediante el proceso de
            // reservas antes de aparecer como disponible: esta consulta no escribe datos.
            var asignaciones = db.ReservaPasajeroVuelo
                .Where(a => a.EstadoAsignacionCodigo == "PENDIENTE" ||
                            a.EstadoAsignacionCodigo == "CONFIRMADA")
                .ToList();

            var ocupacion = new Dictionary<int, HashSet<byte>>();
            foreach (var a in asignaciones)
            {
                if (!ocupacion.ContainsKey(a.VueloId))
                    ocupacion[a.VueloId] = new HashSet<byte>();
                ocupacion[a.VueloId].Add(a.NumeroAsiento);
            }
            return ocupacion;
        }

        private Dictionary<int, int> ObtenerConteos(Dictionary<int, HashSet<byte>> ocupados)
        {
            return ocupados.ToDictionary(x => x.Key, x => x.Value.Count);
        }

        private List<Vuelo> ObtenerVuelosFiltrados(int? origenId, int? destinoId,
            DateTime? fecha, int pasajeros, Dictionary<int, HashSet<byte>> ocupados)
        {
            var consulta = db.Vuelo
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Where(v => v.EstadoVueloCodigo != "CANCELADO" &&
                            v.EstadoVueloCodigo != "COMPLETADO");

            if (origenId.HasValue)
                consulta = consulta.Where(v => v.Frecuencia.Ruta.AeropuertoOrigenId == origenId.Value);
            if (destinoId.HasValue)
                consulta = consulta.Where(v => v.Frecuencia.Ruta.AeropuertoDestinoId == destinoId.Value);
            if (fecha.HasValue)
            {
                var desde = fecha.Value.Date;
                var hasta = desde.AddDays(1);
                consulta = consulta.Where(v => v.FechaOperacion >= desde && v.FechaOperacion < hasta);
            }

            var vuelos = consulta.OrderBy(v => v.FechaOperacion).ThenBy(v => v.VueloId).ToList();
            var resultados = new List<Vuelo>();
            foreach (var vuelo in vuelos)
            {
                var capacidad = vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null
                    ? (int)vuelo.Aeronave.ModeloAeronave.CapacidadAsientos : 0;
                var cantidadOcupada = ocupados.ContainsKey(vuelo.VueloId)
                    ? ocupados[vuelo.VueloId].Count : 0;
                if (capacidad > 0 && capacidad - cantidadOcupada >= pasajeros)
                    resultados.Add(vuelo);
            }
            return resultados;
        }

        private void PrepararDatosDeAsientos(Vuelo vuelo, bool incluirHorario)
        {
            var capacidad = vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null
                ? (int)vuelo.Aeronave.ModeloAeronave.CapacidadAsientos : 0;
            var ocupados = ObtenerAsientosOcupados();
            var numeros = ocupados.ContainsKey(vuelo.VueloId)
                ? ocupados[vuelo.VueloId] : new HashSet<byte>();
            var asientos = vuelo.Aeronave == null ? new List<Asiento>() : db.Asiento
                .Where(a => a.ModeloAeronaveId == vuelo.Aeronave.ModeloAeronaveId)
                .OrderBy(a => a.NumeroAsiento)
                .ToList();

            ViewBag.VueloId = vuelo.VueloId;
            ViewBag.Capacidad = capacidad;
            ViewBag.Ocupados = numeros.Count;
            ViewBag.Restantes = Math.Max(0, capacidad - numeros.Count);
            ViewBag.AeropuertoOrigen = vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null &&
                vuelo.Frecuencia.Ruta.Aeropuerto != null
                ? vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA : "";
            ViewBag.AeropuertoDestino = vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null &&
                vuelo.Frecuencia.Ruta.Aeropuerto1 != null
                ? vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA : "";
            ViewBag.FechaOperacion = vuelo.FechaOperacion;
            ViewBag.Matricula = vuelo.Aeronave == null ? "N/A" : vuelo.Aeronave.Matricula;
            ViewBag.Asientos = asientos;
            ViewBag.AsientosOcupados = asientos.Where(a => numeros.Contains(a.NumeroAsiento))
                .Select(a => a.Codigo).ToList();

            if (incluirHorario)
            {
                ViewBag.HoraPartida = vuelo.Frecuencia == null ? "" : vuelo.Frecuencia.HoraSalida.ToString(@"hh\:mm");
                ViewBag.HoraLlegada = vuelo.Frecuencia == null ? "" : vuelo.Frecuencia.HoraLlegada.ToString(@"hh\:mm");
            }
        }

        private void CargarListas(Vuelo vuelo = null)
        {
            ViewBag.AeronaveId = new SelectList(db.Aeronave.OrderBy(a => a.Matricula).ToList(),
                "AeronaveId", "Matricula", vuelo == null ? null : (object)vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo.ToList(),
                "Codigo", "Descripcion", vuelo == null ? null : (object)vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencia.OrderBy(f => f.FrecuenciaId).ToList(),
                "FrecuenciaId", "FrecuenciaId", vuelo == null ? null : (object)vuelo.FrecuenciaId);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
