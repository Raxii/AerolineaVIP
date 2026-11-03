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
    public class AeropuertoController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Aeropuerto
        public ActionResult Index(string buscar, string estado)
        {
            var listaAeropuertos = new List<Aeropuerto>();
            int total = 0;
            int activos = 0;
            int inactivos = 0;

            foreach (var aeropuerto in db.Aeropuerto)
            {
                total++;
                if (aeropuerto.Activo)
                {
                    activos++;
                }
                else
                {
                    inactivos++;
                }

                bool incluir = true;

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string texto = (aeropuerto.CodigoIATA ?? "") + " " +
                                   (aeropuerto.Nombre ?? "") + " " +
                                   (aeropuerto.Localidad ?? "");
                    if (!Contiene(texto, buscar))
                    {
                        incluir = false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    if (estado == "Activo" && !aeropuerto.Activo)
                    {
                        incluir = false;
                    }
                    if (estado == "Inactivo" && aeropuerto.Activo)
                    {
                        incluir = false;
                    }
                }

                if (incluir)
                {
                    listaAeropuertos.Add(aeropuerto);
                }
            }

            ViewBag.ListaAeropuertos = listaAeropuertos;
            ViewBag.TotalAeropuertos = total;
            ViewBag.AeropuertosActivos = activos;
            ViewBag.AeropuertosInactivos = inactivos;
            ViewBag.Buscar = buscar ?? "";
            ViewBag.EstadoSeleccionado = estado ?? "";

            return View();
        }

        // GET: Aeropuerto/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeropuerto aeropuerto = db.Aeropuerto.Find(id);
            if (aeropuerto == null)
            {
                return HttpNotFound();
            }
            return View(aeropuerto);
        }

        // GET: Aeropuerto/Create
        public ActionResult Create()
        {
            return View(new Aeropuerto { Activo = true });
        }

        // POST: Aeropuerto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AeropuertoId,CodigoIATA,Nombre,Localidad,Activo")] Aeropuerto aeropuerto)
        {
            if (ModelState.IsValid)
            {
                db.Aeropuerto.Add(aeropuerto);
                db.SaveChanges();
                TempData["Ok"] = "Aeropuerto registrado correctamente.";
                return RedirectToAction("Index");
            }
            return View(aeropuerto);
        }

        // GET: Aeropuerto/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeropuerto aeropuerto = db.Aeropuerto.Find(id);
            if (aeropuerto == null)
            {
                return HttpNotFound();
            }
            return View(aeropuerto);
        }

        // POST: Aeropuerto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AeropuertoId,CodigoIATA,Nombre,Localidad,Activo")] Aeropuerto aeropuerto)
        {
            if (ModelState.IsValid)
            {
                var actual = db.Aeropuerto.Find(aeropuerto.AeropuertoId);
                if (actual == null) return HttpNotFound();

                bool desactivar = actual.Activo && !aeropuerto.Activo;
                using (var transaccion = db.Database.BeginTransaction())
                {
                    try
                    {
                        actual.CodigoIATA = aeropuerto.CodigoIATA;
                        actual.Nombre = aeropuerto.Nombre;
                        actual.Localidad = aeropuerto.Localidad;
                        actual.Activo = aeropuerto.Activo;

                        if (desactivar)
                        {
                            var rutasRelacionadas = new List<Ruta>();
                            foreach (var ruta in db.Ruta)
                                if (ruta.AeropuertoOrigenId == actual.AeropuertoId || ruta.AeropuertoDestinoId == actual.AeropuertoId)
                                    rutasRelacionadas.Add(ruta);

                            var vuelosRelacionados = new HashSet<int>();
                            foreach (var ruta in rutasRelacionadas)
                            {
                                ruta.Activa = false;
                                foreach (var frecuencia in db.Frecuencia)
                                {
                                    if (frecuencia.RutaId == ruta.RutaId)
                                    {
                                        frecuencia.Activa = false;
                                        foreach (var vuelo in db.Vuelo)
                                        {
                                            if (vuelo.FrecuenciaId == frecuencia.FrecuenciaId)
                                            {
                                                vuelosRelacionados.Add(vuelo.VueloId);
                                                if (vuelo.EstadoVueloCodigo == "PROGRAMADO" || vuelo.EstadoVueloCodigo == "CONFIRMADO")
                                                    vuelo.EstadoVueloCodigo = "CANCELADO";
                                            }
                                        }
                                    }
                                }
                            }

                            // Los tramos no tienen columna de estado en el modelo actual.
                            // Se inactiva el itinerario completo para que esos tramos no puedan ofrecerse.
                            string estadoInactivoItinerario = null;
                            foreach (var estadoItinerario in db.EstadoItinerario)
                            {
                                string texto = ((estadoItinerario.Codigo ?? "") + " " + (estadoItinerario.Descripcion ?? "")).ToUpperInvariant();
                                if (texto.Contains("INACTIV")) { estadoInactivoItinerario = estadoItinerario.Codigo; break; }
                            }
                            if (!string.IsNullOrWhiteSpace(estadoInactivoItinerario))
                            {
                                var itinerariosAfectados = new HashSet<int>();
                                foreach (var tramo in db.TramoItinerario)
                                    if (vuelosRelacionados.Contains(tramo.VueloId)) itinerariosAfectados.Add(tramo.ItinerarioId);
                                foreach (var itinerario in db.Itinerario)
                                    if (itinerariosAfectados.Contains(itinerario.ItinerarioId)) itinerario.EstadoItinerarioCodigo = estadoInactivoItinerario;
                            }
                        }

                        db.SaveChanges();
                        transaccion.Commit();
                    }
                    catch
                    {
                        transaccion.Rollback();
                        ModelState.AddModelError("", "No se pudieron actualizar las dependencias del aeropuerto. No se guardaron cambios.");
                        return View(aeropuerto);
                    }
                }

                TempData["Ok"] = desactivar
                    ? "Aeropuerto desactivado. Tambien se desactivaron sus rutas y frecuencias y se cancelaron sus vuelos programados."
                    : "Cambios del aeropuerto guardados correctamente.";
                return RedirectToAction("Index");
            }
            return View(aeropuerto);
        }

        // GET: Aeropuerto/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeropuerto aeropuerto = db.Aeropuerto.Find(id);
            if (aeropuerto == null)
            {
                return HttpNotFound();
            }
            ViewBag.RutasAsociadas = ContarRutas(aeropuerto.AeropuertoId);
            return View(aeropuerto);
        }

        // POST: Aeropuerto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Aeropuerto aeropuerto = db.Aeropuerto.Find(id);
            if (aeropuerto == null)
            {
                return HttpNotFound();
            }
            if (ContarRutas(id) > 0)
            {
                TempData["Error"] = "No se puede eliminar este aeropuerto porque forma parte de una o mas rutas. Desactivalo si ya no debe utilizarse.";
                return RedirectToAction("Delete", new { id = id });
            }
            db.Aeropuerto.Remove(aeropuerto);
            db.SaveChanges();
            TempData["Ok"] = "El aeropuerto se elimino correctamente.";
            return RedirectToAction("Index");
        }

        private int ContarRutas(int aeropuertoId)
        {
            int total = 0;
            foreach (var ruta in db.Ruta)
            {
                if (ruta.AeropuertoOrigenId == aeropuertoId || ruta.AeropuertoDestinoId == aeropuertoId)
                {
                    total++;
                }
            }
            return total;
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
