using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Con_Autenticacion.Models;
using Con_Autenticacion.ViewModels;

namespace Con_Autenticacion.Controllers
{
    [Authorize]
    public class MisViajesController : Controller
    {
        private readonly AerolineaVIP_Context db = new AerolineaVIP_Context();

        public ActionResult Index() { return RedirectToAction("Reservas"); }

        public ActionResult Reservas()
        {
            return View("Reservas", CargarDatosCliente());
        }

        public ActionResult Comprobantes()
        {
            return View("Comprobantes", CargarDatosCliente());
        }

        public ActionResult CheckIn()
        {
            return View("CheckIn", CargarDatosCliente());
        }

        // Muestra solamente comprobantes de la cuenta autenticada.
        [HttpGet]
        public ActionResult VerComprobante(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(400);

            ReservaClienteVM reserva;
            ComprobanteClienteVM comprobante = ObtenerComprobantePropio(id.Value, out reserva);
            if (comprobante == null) return HttpNotFound();

            ViewBag.DetalleReserva = reserva;
            return View("VerComprobante", comprobante);
        }

        // Descarga un PDF real sin dependencias NuGet adicionales.
        [HttpGet]
        public ActionResult DescargarComprobante(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(400);

            ReservaClienteVM reserva;
            ComprobanteClienteVM comprobante = ObtenerComprobantePropio(id.Value, out reserva);
            if (comprobante == null) return HttpNotFound();

            if (comprobante.Estado != "EMITIDO")
                return new HttpStatusCodeResult(400, "El comprobante no esta emitido.");

            byte[] pdf = Con_Autenticacion.Helpers.ComprobantePdf.Generar(comprobante, reserva);
            string nombre = "Comprobante-" + comprobante.ComprobanteId.ToString() + ".pdf";
            return File(pdf, "application/pdf", nombre);
        }

        private ComprobanteClienteVM ObtenerComprobantePropio(int comprobanteId,
            out ReservaClienteVM reservaEncontrada)
        {
            reservaEncontrada = null;
            // CargarDatosCliente obtiene exclusivamente reservas del usuario autenticado
            // y solo los comprobantes asociados a compras de esas reservas.
            MisViajesVM datos = CargarDatosCliente();
            ComprobanteClienteVM comprobante = null;

            foreach (ComprobanteClienteVM item in datos.Comprobantes)
            {
                if (item.ComprobanteId == comprobanteId)
                {
                    comprobante = item;
                    break;
                }
            }
            if (comprobante == null) return null;

            foreach (ReservaClienteVM reserva in datos.Reservas)
            {
                if (reserva.CodigoReserva == comprobante.CodigoReserva)
                {
                    reservaEncontrada = reserva;
                    break;
                }
            }
            return comprobante;
        }

        // Se comprueba de nuevo la propiedad de la reserva ANTES de escribir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarCheckIn(int asignacionId)
        {
            string usuarioId = User.Identity.GetUserId();
            if (string.IsNullOrWhiteSpace(usuarioId)) return new HttpUnauthorizedResult();

            ReservaPasajeroVuelo asignacion = db.Set<ReservaPasajeroVuelo>().Find(asignacionId);
            if (asignacion == null) return HttpNotFound();

            Reserva reserva = db.Set<Reserva>().Find(asignacion.ReservaId);
            if (reserva == null || reserva.UsuarioId != usuarioId)
                return HttpNotFound(); // No se revela información de otras cuentas.

            if (reserva.EstadoReservaCodigo != "CONFIRMADA" ||
                asignacion.EstadoAsignacionCodigo != "CONFIRMADA")
            {
                TempData["ErrorMisViajes"] = "La reserva y el asiento deben estar confirmados.";
                return RedirectToAction("CheckIn");
            }

            Vuelo vuelo = db.Set<Vuelo>().Find(asignacion.VueloId);
            if (vuelo == null || vuelo.AeronaveId == null ||
                (vuelo.EstadoVueloCodigo != "PROGRAMADO" && vuelo.EstadoVueloCodigo != "CONFIRMADO"))
            {
                TempData["ErrorMisViajes"] = "El vuelo no está habilitado para check-in.";
                return RedirectToAction("CheckIn");
            }

            Frecuencia frecuencia = db.Set<Frecuencia>().Find(vuelo.FrecuenciaId);
            if (frecuencia == null)
            {
                TempData["ErrorMisViajes"] = "No se encontró el horario del vuelo.";
                return RedirectToAction("CheckIn");
            }

            // Regla conservadora para esta implementación académica:
            // check-in durante el día del vuelo y antes de la hora de salida.
            // DateTime.Now supone que el servidor usa la zona horaria operativa local.
            DateTime ahora = DateTime.Now;
            if (ahora.Date != vuelo.FechaOperacion.Date || ahora.TimeOfDay >= frecuencia.HoraSalida)
            {
                TempData["ErrorMisViajes"] = "El check-in está habilitado solo el día del vuelo, antes de la salida.";
                return RedirectToAction("CheckIn");
            }

            foreach (CheckIn existente in db.Set<CheckIn>())
            {
                if (existente.AsignacionId == asignacionId && existente.EstadoCheckInCodigo == "COMPLETADO")
                {
                    TempData["MensajeMisViajes"] = "El check-in ya está registrado.";
                    return RedirectToAction("CheckIn");
                }
            }

            db.Set<CheckIn>().Add(new CheckIn
            {
                AsignacionId = asignacionId,
                FechaRegistro = ahora,
                EstadoCheckInCodigo = "COMPLETADO"
            });

            try
            {
                db.SaveChanges();
                TempData["MensajeMisViajes"] = "Check-in registrado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMisViajes"] = "No fue posible registrar el check-in. Verifica si ya existe uno vigente.";
            }
            return RedirectToAction("CheckIn");
        }

        private MisViajesVM CargarDatosCliente()
        {
            string usuarioId = User.Identity.GetUserId();
            var resultado = new MisViajesVM();
            if (string.IsNullOrWhiteSpace(usuarioId)) return resultado;

            // Diccionarios en memoria, sin LINQ ni SQL manual.
            var propias = new Dictionary<int, Reserva>();
            foreach (Reserva r in db.Set<Reserva>())
            {
                if (r.UsuarioId != usuarioId) continue;
                propias[r.ReservaId] = r;
                resultado.Reservas.Add(new ReservaClienteVM
                {
                    ReservaId = r.ReservaId,
                    CodigoReserva = r.CodigoReserva,
                    FechaCreacion = r.FechaCreacion,
                    Estado = r.EstadoReservaCodigo
                });
            }

            var comprasPropias = new Dictionary<int, Compra>();
            foreach (Compra c in db.Set<Compra>())
                if (propias.ContainsKey(c.ReservaId)) comprasPropias[c.CompraId] = c;

            foreach (Comprobante cb in db.Set<Comprobante>())
            {
                Compra compra;
                if (!comprasPropias.TryGetValue(cb.CompraId, out compra)) continue;
                Reserva reserva = propias[compra.ReservaId];
                resultado.Comprobantes.Add(new ComprobanteClienteVM
                {
                    ComprobanteId = cb.ComprobanteId,
                    NumeroComprobante = cb.NumeroComprobante,
                    Tipo = cb.TipoComprobanteCodigo,
                    Estado = cb.EstadoComprobanteCodigo,
                    FechaEmision = cb.FechaEmision,
                    CodigoReserva = reserva.CodigoReserva,
                    ImporteCompra = compra.Importe,
                    Moneda = compra.MonedaCodigo
                });
            }

            var vuelos = new Dictionary<int, Vuelo>();
            foreach (Vuelo v in db.Set<Vuelo>()) vuelos[v.VueloId] = v;
            var frecuencias = new Dictionary<int, Frecuencia>();
            foreach (Frecuencia f in db.Set<Frecuencia>()) frecuencias[f.FrecuenciaId] = f;
            var rutas = new Dictionary<int, Ruta>();
            foreach (Ruta r in db.Set<Ruta>()) rutas[r.RutaId] = r;
            var aeropuertos = new Dictionary<int, Aeropuerto>();
            foreach (Aeropuerto a in db.Set<Aeropuerto>()) aeropuertos[a.AeropuertoId] = a;
            var pasajeros = new Dictionary<int, Pasajero>();
            foreach (Pasajero p in db.Set<Pasajero>()) pasajeros[p.PasajeroId] = p;

            var checkInsRealizados = new HashSet<int>();
            foreach (CheckIn ch in db.Set<CheckIn>())
                if (ch.EstadoCheckInCodigo == "COMPLETADO") checkInsRealizados.Add(ch.AsignacionId);

            var resumenReservas = new Dictionary<int, ReservaClienteVM>();
            foreach (ReservaClienteVM item in resultado.Reservas) resumenReservas[item.ReservaId] = item;

            foreach (ReservaPasajeroVuelo a in db.Set<ReservaPasajeroVuelo>())
            {
                Reserva reserva;
                if (!propias.TryGetValue(a.ReservaId, out reserva)) continue;
                ReservaClienteVM resumen = resumenReservas[a.ReservaId];
                resumen.PasajerosTramos++;
                if (a.EstadoAsignacionCodigo != "CANCELADA" && a.EstadoAsignacionCodigo != "LIBERADA")
                    resumen.TotalTramos += a.ImporteTramo;

                Vuelo vuelo;
                Frecuencia frecuencia = null;
                Ruta ruta;
                string trayecto = "Ruta no disponible";
                string hora = "--:--";
                DateTime fechaVuelo = DateTime.MinValue;
                string estadoVuelo = "";

                if (vuelos.TryGetValue(a.VueloId, out vuelo))
                {
                    fechaVuelo = vuelo.FechaOperacion;
                    estadoVuelo = vuelo.EstadoVueloCodigo;
                    if (frecuencias.TryGetValue(vuelo.FrecuenciaId, out frecuencia))
                    {
                        hora = frecuencia.HoraSalida.ToString(@"hh\:mm");
                        if (rutas.TryGetValue(frecuencia.RutaId, out ruta))
                        {
                            Aeropuerto origen;
                            Aeropuerto destino;
                            if (aeropuertos.TryGetValue(ruta.AeropuertoOrigenId, out origen) &&
                                aeropuertos.TryGetValue(ruta.AeropuertoDestinoId, out destino))
                                trayecto = origen.Localidad + " (" + origen.CodigoIATA.Trim() + ") → " +
                                           destino.Localidad + " (" + destino.CodigoIATA.Trim() + ")";
                        }
                    }
                }

                Pasajero pasajero;
                string nombrePasajero = pasajeros.TryGetValue(a.PasajeroId, out pasajero)
                    ? (pasajero.Nombres + " " + pasajero.Apellidos).Trim() : "Pasajero";

                resumen.Descripciones.Add(trayecto + " | " +
                    (fechaVuelo == DateTime.MinValue ? "Sin fecha" : fechaVuelo.ToString("dd/MM/yyyy")) +
                    " " + hora + " | " + nombrePasajero + " | Asiento " + a.NumeroAsiento);

                bool realizado = checkInsRealizados.Contains(a.AsignacionId);
                bool confirmado = reserva.EstadoReservaCodigo == "CONFIRMADA" &&
                                  a.EstadoAsignacionCodigo == "CONFIRMADA";
                bool operativo = vuelo != null && vuelo.AeronaveId.HasValue &&
                    (estadoVuelo == "PROGRAMADO" || estadoVuelo == "CONFIRMADO");
                bool enHorario = vuelo != null && frecuencia != null &&
                    DateTime.Now.Date == fechaVuelo.Date && DateTime.Now.TimeOfDay < frecuencia.HoraSalida;

                resultado.CheckIns.Add(new CheckInClienteVM
                {
                    AsignacionId = a.AsignacionId,
                    CodigoReserva = reserva.CodigoReserva,
                    Pasajero = nombrePasajero,
                    Trayecto = trayecto,
                    FechaVuelo = fechaVuelo,
                    HoraSalida = hora,
                    Asiento = a.NumeroAsiento,
                    EstadoAsignacion = a.EstadoAsignacionCodigo,
                    Realizado = realizado,
                    PuedeRealizar = !realizado && confirmado && operativo && enHorario,
                    Motivo = realizado ? "Completado" : !confirmado ? "Reserva/asiento pendiente de confirmación" :
                             !operativo ? "Vuelo no habilitado" : !enHorario ? "Disponible el día del vuelo antes de salir" : "Disponible"
                });
            }

            resultado.Reservas.Sort(delegate (ReservaClienteVM x, ReservaClienteVM y) {
                return y.FechaCreacion.CompareTo(x.FechaCreacion);
            });
            resultado.Comprobantes.Sort(delegate (ComprobanteClienteVM x, ComprobanteClienteVM y) {
                return y.FechaEmision.CompareTo(x.FechaEmision);
            });
            resultado.CheckIns.Sort(delegate (CheckInClienteVM x, CheckInClienteVM y) {
                return y.FechaVuelo.CompareTo(x.FechaVuelo);
            });
            return resultado;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
