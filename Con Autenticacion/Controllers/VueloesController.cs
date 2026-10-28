using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    public class VueloesController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Vueloes
        // PLANTILLA #5
        public ActionResult Buscar(int? AeropuertoOrigenId, int? AeropuertoDestinoId, DateTime? Fecha, int CantidadPasajeros = 1)
        {
            var vuelos = new List<Vuelo>();
            foreach (var vuelo in db.Vuelo)
            {
                vuelos.Add(vuelo);
            }

            var todasLasReservas = new List<ReservaPasajeroVuelo>();
            try
            {
                foreach (var reserva in db.ReservaPasajeroVuelo)
                {
                    todasLasReservas.Add(reserva);
                }
            }
            catch
            {
                // Si hay error en la tabla de reservas, continuamos sin datos de ocupados
            }

            var resultados = new List<Vuelo>();
            foreach (var vuelo in vuelos)
            {
                var cumpleOrigen = !AeropuertoOrigenId.HasValue;
                if (AeropuertoOrigenId.HasValue && vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
                {
                    cumpleOrigen = vuelo.Frecuencia.Ruta.AeropuertoOrigenId == AeropuertoOrigenId.Value;
                }

                var cumpleDestino = !AeropuertoDestinoId.HasValue;
                if (AeropuertoDestinoId.HasValue && vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
                {
                    cumpleDestino = vuelo.Frecuencia.Ruta.AeropuertoDestinoId == AeropuertoDestinoId.Value;
                }

                var cumpleFecha = !Fecha.HasValue;
                if (Fecha.HasValue)
                {
                    cumpleFecha = vuelo.FechaOperacion.Date == Fecha.Value.Date;
                }

                if (!cumpleOrigen || !cumpleDestino || !cumpleFecha)
                {
                    continue;
                }

                var capacidad = 0;
                if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                {
                    capacidad = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;
                }

                var ocupados = 0;
                foreach (var reserva in todasLasReservas)
                {
                    if (reserva.VueloId == vuelo.VueloId)
                    {
                        ocupados++;
                    }
                }

                var disponibles = capacidad > 0 ? capacidad - ocupados : 0;
                if (disponibles >= CantidadPasajeros)
                {
                    resultados.Add(vuelo);
                }
            }

            var aeropuertos = new List<SelectListItem>();
            foreach (var aeropuerto in db.Aeropuerto)
            {
                if (aeropuerto.Activo)
                {
                    aeropuertos.Add(new SelectListItem
                    {
                        Value = aeropuerto.AeropuertoId.ToString(),
                        Text = aeropuerto.Localidad + " (" + aeropuerto.CodigoIATA + ")"
                    });
                }
            }

            if (aeropuertos.Count == 0)
            {
                aeropuertos.Add(new SelectListItem { Value = "0", Text = "No existen Registros" });
            }

            var ocupadosPorVuelo = new Dictionary<int, int>();
            foreach (var reserva in todasLasReservas)
            {
                if (!ocupadosPorVuelo.ContainsKey(reserva.VueloId))
                {
                    ocupadosPorVuelo[reserva.VueloId] = 0;
                }
                ocupadosPorVuelo[reserva.VueloId]++;
            }

            ViewBag.AeropuertoOrigenId = AeropuertoOrigenId;
            ViewBag.AeropuertoDestinoId = AeropuertoDestinoId;
            ViewBag.Fecha = Fecha;
            ViewBag.CantidadPasajeros = CantidadPasajeros;
            ViewBag.HayResultados = resultados.Count > 0;
            ViewBag.TotalResultados = resultados.Count;
            ViewBag.Aeropuertos = aeropuertos;
            ViewBag.OcupadosPorVuelo = ocupadosPorVuelo;

            return View(resultados);
        }
        // PLANTILLA #6
        public ActionResult Resultados(int? AeropuertoOrigenId, int? AeropuertoDestinoId, DateTime? Fecha, int CantidadPasajeros = 1)
        {
            var vuelos = new List<Vuelo>();
            foreach (var vuelo in db.Vuelo)
            {
                vuelos.Add(vuelo);
            }

            var todasLasReservas = new List<ReservaPasajeroVuelo>();
            try
            {
                foreach (var reserva in db.ReservaPasajeroVuelo)
                {
                    todasLasReservas.Add(reserva);
                }
            }
            catch
            {
                // Si hay error en la tabla de reservas, continuamos sin datos de ocupados
            }

            var resultados = new List<Vuelo>();
            foreach (var vuelo in vuelos)
            {
                var cumpleOrigen = !AeropuertoOrigenId.HasValue;
                if (AeropuertoOrigenId.HasValue && vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
                {
                    cumpleOrigen = vuelo.Frecuencia.Ruta.AeropuertoOrigenId == AeropuertoOrigenId.Value;
                }

                var cumpleDestino = !AeropuertoDestinoId.HasValue;
                if (AeropuertoDestinoId.HasValue && vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
                {
                    cumpleDestino = vuelo.Frecuencia.Ruta.AeropuertoDestinoId == AeropuertoDestinoId.Value;
                }

                var cumpleFecha = !Fecha.HasValue;
                if (Fecha.HasValue)
                {
                    cumpleFecha = vuelo.FechaOperacion.Date == Fecha.Value.Date;
                }

                if (!cumpleOrigen || !cumpleDestino || !cumpleFecha)
                {
                    continue;
                }

                var capacidad = 0;
                if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                {
                    capacidad = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;
                }

                var ocupados = 0;
                foreach (var reserva in todasLasReservas)
                {
                    if (reserva.VueloId == vuelo.VueloId)
                    {
                        ocupados++;
                    }
                }

                var disponibles = capacidad > 0 ? capacidad - ocupados : 0;
                if (disponibles >= CantidadPasajeros)
                {
                    resultados.Add(vuelo);
                }
            }

            var aeropuertos = new List<Aeropuerto>();
            foreach (var aeropuerto in db.Aeropuerto)
            {
                if (aeropuerto.Activo)
                {
                    aeropuertos.Add(aeropuerto);
                }
            }

            var aeropuertoOrigen = "";
            var aeropuertoDestino = "";
            foreach (var aeropuerto in aeropuertos)
            {
                if (AeropuertoOrigenId.HasValue && aeropuerto.AeropuertoId == AeropuertoOrigenId.Value)
                {
                    aeropuertoOrigen = aeropuerto.Nombre + " (" + aeropuerto.CodigoIATA + ")";
                }
                if (AeropuertoDestinoId.HasValue && aeropuerto.AeropuertoId == AeropuertoDestinoId.Value)
                {
                    aeropuertoDestino = aeropuerto.Nombre + " (" + aeropuerto.CodigoIATA + ")";
                }
            }

            var ocupadosPorVuelo = new Dictionary<int, int>();
            foreach (var reserva in todasLasReservas)
            {
                if (!ocupadosPorVuelo.ContainsKey(reserva.VueloId))
                {
                    ocupadosPorVuelo[reserva.VueloId] = 0;
                }
                ocupadosPorVuelo[reserva.VueloId]++;
            }

            ViewBag.AeropuertoOrigen = aeropuertoOrigen;
            ViewBag.AeropuertoDestino = aeropuertoDestino;
            ViewBag.Fecha = Fecha;
            ViewBag.CantidadPasajeros = CantidadPasajeros;
            ViewBag.Vuelos = resultados;
            ViewBag.HayResultados = resultados.Count > 0;
            ViewBag.TotalResultados = resultados.Count;
            ViewBag.OcupadosPorVuelo = ocupadosPorVuelo;

            return View(resultados);
        }
        // PLANTILLA #7
        public ActionResult DisponibilidadAsientos(int? vueloId)
        {
            if (!vueloId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            Vuelo vuelo = null;
            foreach (var v in db.Vuelo)
            {
                if (v.VueloId == vueloId.Value)
                {
                    vuelo = v;
                    break;
                }
            }

            if (vuelo == null)
            {
                return HttpNotFound();
            }

            var capacidad = 0;
            if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
            {
                capacidad = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;
            }

            var todasLasReservas = new List<ReservaPasajeroVuelo>();
            try
            {
                foreach (var reserva in db.ReservaPasajeroVuelo)
                {
                    todasLasReservas.Add(reserva);
                }
            }
            catch
            {
                // Si hay error en la tabla de reservas, continuamos sin datos de ocupados
            }

            var ocupados = 0;
            var asientosOcupados = new List<string>();
            foreach (var reserva in todasLasReservas)
            {
                if (reserva.VueloId == vuelo.VueloId)
                {
                    ocupados++;
                    var codigoAsiento = "";
                    try
                    {
                        if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                        {
                            foreach (var asiento in vuelo.Aeronave.ModeloAeronave.Asiento)
                            {
                                if (asiento.NumeroAsiento == reserva.NumeroAsiento)
                                {
                                    codigoAsiento = asiento.Codigo;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Si hay error en la tabla de asientos, continuamos sin el código del asiento
                    }
                    if (!string.IsNullOrEmpty(codigoAsiento))
                    {
                        asientosOcupados.Add(codigoAsiento);
                    }
                }
            }

            var restantes = capacidad > 0 ? capacidad - ocupados : 0;

            var asientos = new List<Asiento>();
            try
            {
                if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                {
                    foreach (var asiento in vuelo.Aeronave.ModeloAeronave.Asiento)
                    {
                        asientos.Add(asiento);
                    }
                }
            }
            catch
            {
                // Si hay error en la tabla de asientos, continuamos sin datos de asientos
            }

            var aeropuertoOrigen = "";
            var aeropuertoDestino = "";
            if (vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
            {
                if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
                {
                    aeropuertoOrigen = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA;
                }
                if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
                {
                    aeropuertoDestino = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA;
                }
            }

            ViewBag.VueloId = vuelo.VueloId;
            ViewBag.Capacidad = capacidad;
            ViewBag.Ocupados = ocupados;
            ViewBag.Restantes = restantes;
            ViewBag.AeropuertoOrigen = aeropuertoOrigen;
            ViewBag.AeropuertoDestino = aeropuertoDestino;
            ViewBag.FechaOperacion = vuelo.FechaOperacion;
            ViewBag.Matricula = vuelo.Aeronave != null ? vuelo.Aeronave.Matricula : "N/A";
            ViewBag.AsientosOcupados = asientosOcupados;
            ViewBag.Asientos = asientos;

            return View(vuelo);
        }
        // PLANTILLA #8
        public ActionResult Seleccionar(int? vueloId)
        {
            if (!vueloId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            Vuelo vuelo = null;
            foreach (var v in db.Vuelo)
            {
                if (v.VueloId == vueloId.Value)
                {
                    vuelo = v;
                    break;
                }
            }

            if (vuelo == null)
            {
                return HttpNotFound();
            }

            var capacidad = 0;
            if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
            {
                capacidad = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;
            }

            var todasLasReservas = new List<ReservaPasajeroVuelo>();
            try
            {
                foreach (var reserva in db.ReservaPasajeroVuelo)
                {
                    todasLasReservas.Add(reserva);
                }
            }
            catch
            {
                // Si hay error en la tabla de reservas, continuamos sin datos de ocupados
            }

            var ocupados = 0;
            var asientosOcupados = new List<string>();
            foreach (var reserva in todasLasReservas)
            {
                if (reserva.VueloId == vuelo.VueloId)
                {
                    ocupados++;
                    var codigoAsiento = "";
                    try
                    {
                        if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                        {
                            foreach (var asiento in vuelo.Aeronave.ModeloAeronave.Asiento)
                            {
                                if (asiento.NumeroAsiento == reserva.NumeroAsiento)
                                {
                                    codigoAsiento = asiento.Codigo;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Si hay error en la tabla de asientos, continuamos sin el código del asiento
                    }
                    if (!string.IsNullOrEmpty(codigoAsiento))
                    {
                        asientosOcupados.Add(codigoAsiento);
                    }
                }
            }

            var restantes = capacidad > 0 ? capacidad - ocupados : 0;

            var asientos = new List<Asiento>();
            try
            {
                if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                {
                    foreach (var asiento in vuelo.Aeronave.ModeloAeronave.Asiento)
                    {
                        asientos.Add(asiento);
                    }
                }
            }
            catch
            {
                // Si hay error en la tabla de asientos, continuamos sin datos de asientos
            }

            var aeropuertoOrigen = "";
            var aeropuertoDestino = "";
            var horaPartida = "";
            var horaLlegada = "";
            if (vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
            {
                if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
                {
                    aeropuertoOrigen = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA;
                }
                if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
                {
                    aeropuertoDestino = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA;
                }
                horaPartida = vuelo.Frecuencia.HoraSalida.ToString(@"hh\:mm");
                horaLlegada = vuelo.Frecuencia.HoraLlegada.ToString(@"hh\:mm");
            }

            ViewBag.VueloId = vuelo.VueloId;
            ViewBag.Capacidad = capacidad;
            ViewBag.Ocupados = ocupados;
            ViewBag.Restantes = restantes;
            ViewBag.AeropuertoOrigen = aeropuertoOrigen;
            ViewBag.AeropuertoDestino = aeropuertoDestino;
            ViewBag.HoraPartida = horaPartida;
            ViewBag.HoraLlegada = horaLlegada;
            ViewBag.FechaOperacion = vuelo.FechaOperacion;
            ViewBag.Matricula = vuelo.Aeronave != null ? vuelo.Aeronave.Matricula : "N/A";
            ViewBag.AsientosOcupados = asientosOcupados;
            ViewBag.Asientos = asientos;

            return View(vuelo);
        }
        //PLANTILLA #18
        public ActionResult Administrar()
        {
            // Obtener todos los vuelos
            var vuelos = new List<Vuelo>();
            foreach (var vuelo in db.Vuelo)
            {
                vuelos.Add(vuelo);
            }

            // Pasar datos al ViewBag
            ViewBag.TotalVuelos = vuelos.Count;
            ViewBag.Vuelos = vuelos;

            return View();
        }

        //// GET: Vueloes/Details/5
        //public ActionResult Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Vuelo vuelo = db.Vuelo.Find(id);
        //    if (vuelo == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(vuelo);
        //}

        //// GET: Vueloes/Create
        //public ActionResult Create()
        //{
        //    ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula");
        //    ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion");
        //    ViewBag.FrecuenciaId = new SelectList(db.Frecuencia, "FrecuenciaId", "FrecuenciaId");
        //    return View();
        //}

        //// POST: Vueloes/Create
        //// Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        //// más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Vuelo.Add(vuelo);
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }

        //    ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
        //    ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
        //    ViewBag.FrecuenciaId = new SelectList(db.Frecuencia, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
        //    return View(vuelo);
        //}

        //// GET: Vueloes/Edit/5
        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Vuelo vuelo = db.Vuelo.Find(id);
        //    if (vuelo == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
        //    ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
        //    ViewBag.FrecuenciaId = new SelectList(db.Frecuencia, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
        //    return View(vuelo);
        //}

        //// POST: Vueloes/Edit/5
        //// Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        //// más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(vuelo).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
        //    ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
        //    ViewBag.FrecuenciaId = new SelectList(db.Frecuencia, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
        //    return View(vuelo);
        //}

        //// GET: Vueloes/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Vuelo vuelo = db.Vuelo.Find(id);
        //    if (vuelo == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(vuelo);
        //}

        //// POST: Vueloes/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Vuelo vuelo = db.Vuelo.Find(id);
        //    db.Vuelo.Remove(vuelo);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}

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

