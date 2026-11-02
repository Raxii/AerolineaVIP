using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Net;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class TramoItinerariosController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: TramoItinerarios
        public ActionResult Index(int? itinerarioId, string buscar)
        {
            var listaTramos = new List<TramoItinerario>();
            var todosTramos = new List<TramoItinerario>();
            var itinerarios = new List<Itinerario>();
            var vuelos = new List<Vuelo>();
            var frecuencias = new List<Frecuencia>();
            var rutas = new List<Ruta>();
            var aeropuertos = new List<Aeropuerto>();
            var estadosVuelo = new List<EstadoVuelo>();
            var aeronaves = new List<Aeronave>();

            foreach (var item in db.TramoItinerario) { todosTramos.Add(item); }
            foreach (var item in db.Itinerario) { itinerarios.Add(item); }
            foreach (var item in db.Vuelo) { vuelos.Add(item); }
            foreach (var item in db.Frecuencia) { frecuencias.Add(item); }
            foreach (var item in db.Ruta) { rutas.Add(item); }
            foreach (var item in db.Aeropuerto) { aeropuertos.Add(item); }
            foreach (var item in db.EstadoVuelo) { estadosVuelo.Add(item); }
            foreach (var item in db.Aeronave) { aeronaves.Add(item); }

            int total = 0;

            foreach (var tramo in todosTramos)
            {
                total++;
                if (itinerarioId.HasValue && tramo.ItinerarioId != itinerarioId.Value)
                {
                    continue;
                }

                foreach (var item in itinerarios) { if (item.ItinerarioId == tramo.ItinerarioId) { tramo.Itinerario = item; break; } }
                foreach (var item in vuelos) { if (item.VueloId == tramo.VueloId) { tramo.Vuelo = item; break; } }

                if (tramo.Vuelo != null)
                {
                    foreach (var item in estadosVuelo) { if (item.Codigo == tramo.Vuelo.EstadoVueloCodigo) { tramo.Vuelo.EstadoVuelo = item; break; } }
                    foreach (var item in frecuencias) { if (item.FrecuenciaId == tramo.Vuelo.FrecuenciaId) { tramo.Vuelo.Frecuencia = item; break; } }

                    if (tramo.Vuelo.Frecuencia != null)
                    {
                        foreach (var item in rutas) { if (item.RutaId == tramo.Vuelo.Frecuencia.RutaId) { tramo.Vuelo.Frecuencia.Ruta = item; break; } }
                        if (tramo.Vuelo.Frecuencia.Ruta != null)
                        {
                            foreach (var item in aeropuertos)
                            {
                                if (item.AeropuertoId == tramo.Vuelo.Frecuencia.Ruta.AeropuertoOrigenId) { tramo.Vuelo.Frecuencia.Ruta.Aeropuerto = item; }
                                if (item.AeropuertoId == tramo.Vuelo.Frecuencia.Ruta.AeropuertoDestinoId) { tramo.Vuelo.Frecuencia.Ruta.Aeropuerto1 = item; }
                            }
                        }
                    }

                    if (tramo.Vuelo.AeronaveId.HasValue)
                    {
                        foreach (var item in aeronaves) { if (item.AeronaveId == tramo.Vuelo.AeronaveId.Value) { tramo.Vuelo.Aeronave = item; break; } }
                    }
                }

                bool incluir = true;
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string referencia = tramo.Itinerario != null ? tramo.Itinerario.Referencia : "";
                    string texto = tramo.TramoItinerarioId + " " + referencia + " " + tramo.VueloId + " " + tramo.Orden;
                    if (!Contiene(texto, buscar))
                    {
                        incluir = false;
                    }
                }

                if (incluir)
                {
                    listaTramos.Add(tramo);
                }
            }

            ViewBag.ListaTramos = listaTramos;
            ViewBag.TotalTramos = total;
            ViewBag.ItinerarioSeleccionado = itinerarioId;
            ViewBag.Buscar = buscar ?? "";
            ViewBag.Itinerarios = ObtenerItinerarios(itinerarioId);
            return View();
        }

        // GET: TramoItinerarios/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            tramoItinerario.Itinerario = db.Itinerario.Find(tramoItinerario.ItinerarioId);
            tramoItinerario.Vuelo = db.Vuelo.Find(tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Create
        public ActionResult Create()
        {
            ViewBag.ItinerarioId = ObtenerItinerarios(null);
            ViewBag.VueloId = ObtenerVuelos(null);
            return View(new TramoItinerario { Orden = 1 });
        }

        // POST: TramoItinerarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (tramoItinerario.Orden < 1)
            {
                ModelState.AddModelError("Orden", "El orden debe ser mayor que cero.");
            }

            if (ModelState.IsValid)
            {
                db.TramoItinerario.Add(tramoItinerario);
                db.SaveChanges();
                TempData["Ok"] = "Tramo registrado correctamente.";
                return RedirectToAction("Index");
            }

            ViewBag.ItinerarioId = ObtenerItinerarios(tramoItinerario.ItinerarioId);
            ViewBag.VueloId = ObtenerVuelos(tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.ItinerarioId = ObtenerItinerarios(tramoItinerario.ItinerarioId);
            ViewBag.VueloId = ObtenerVuelos(tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // POST: TramoItinerarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (tramoItinerario.Orden < 1)
            {
                ModelState.AddModelError("Orden", "El orden debe ser mayor que cero.");
            }

            if (ModelState.IsValid)
            {
                db.Entry(tramoItinerario).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Ok"] = "Cambios del tramo guardados correctamente.";
                return RedirectToAction("Index");
            }
            ViewBag.ItinerarioId = ObtenerItinerarios(tramoItinerario.ItinerarioId);
            ViewBag.VueloId = ObtenerVuelos(tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            tramoItinerario.Itinerario = db.Itinerario.Find(tramoItinerario.ItinerarioId);
            tramoItinerario.Vuelo = db.Vuelo.Find(tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // POST: TramoItinerarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            db.TramoItinerario.Remove(tramoItinerario);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private List<SelectListItem> ObtenerItinerarios(int? seleccionado)
        {
            var opciones = new List<SelectListItem>();
            var listaItinerarios = new List<Itinerario>();
            foreach (var item in db.Itinerario) { listaItinerarios.Add(item); }
            foreach (var itinerario in listaItinerarios)
            {
                opciones.Add(new SelectListItem
                {
                    Value = itinerario.ItinerarioId.ToString(),
                    Text = (itinerario.Referencia ?? "ITINERARIO") + " - " + (itinerario.Nombre ?? "Sin nombre"),
                    Selected = seleccionado.HasValue && seleccionado.Value == itinerario.ItinerarioId
                });
            }
            return opciones;
        }

        private List<SelectListItem> ObtenerVuelos(int? seleccionado)
        {
            var opciones = new List<SelectListItem>();
            var vuelos = new List<Vuelo>();
            var frecuencias = new List<Frecuencia>();
            var rutas = new List<Ruta>();
            var aeropuertos = new List<Aeropuerto>();
            foreach (var item in db.Vuelo) { vuelos.Add(item); }
            foreach (var item in db.Frecuencia) { frecuencias.Add(item); }
            foreach (var item in db.Ruta) { rutas.Add(item); }
            foreach (var item in db.Aeropuerto) { aeropuertos.Add(item); }

            foreach (var vuelo in vuelos)
            {
                string texto = "Vuelo " + vuelo.VueloId + " - " + vuelo.FechaOperacion.ToString("dd/MM/yyyy");
                Frecuencia frecuencia = null;
                foreach (var item in frecuencias) { if (item.FrecuenciaId == vuelo.FrecuenciaId) { frecuencia = item; break; } }
                if (frecuencia != null)
                {
                    Ruta ruta = null;
                    foreach (var item in rutas) { if (item.RutaId == frecuencia.RutaId) { ruta = item; break; } }
                    if (ruta != null)
                    {
                        Aeropuerto origen = null;
                        Aeropuerto destino = null;
                        foreach (var item in aeropuertos)
                        {
                            if (item.AeropuertoId == ruta.AeropuertoOrigenId) { origen = item; }
                            if (item.AeropuertoId == ruta.AeropuertoDestinoId) { destino = item; }
                        }
                        if (origen != null && destino != null)
                        {
                            texto += " - " + origen.CodigoIATA + " a " + destino.CodigoIATA;
                        }
                    }
                }

                opciones.Add(new SelectListItem
                {
                    Value = vuelo.VueloId.ToString(),
                    Text = texto,
                    Selected = seleccionado.HasValue && seleccionado.Value == vuelo.VueloId
                });
            }
            return opciones;
        }

        private bool Contiene(string texto, string buscar)
        {
            if (string.IsNullOrEmpty(texto) || string.IsNullOrEmpty(buscar))
            {
                return false;
            }
            return texto.IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0;
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
