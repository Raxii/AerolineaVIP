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
    public class FrecuenciasController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Frecuencias
        public ActionResult Index(string buscar, string dia, string estado)
        {
            var listaFrecuencias = new List<Frecuencia>();
            var todasFrecuencias = new List<Frecuencia>();
            var rutas = new List<Ruta>();
            var aeropuertos = new List<Aeropuerto>();
            foreach (var rutaItem in db.Ruta) { rutas.Add(rutaItem); }
            foreach (var aeropuertoItem in db.Aeropuerto) { aeropuertos.Add(aeropuertoItem); }
            foreach (var frecuenciaItem in db.Frecuencia) { todasFrecuencias.Add(frecuenciaItem); }
            int total = 0;
            int activas = 0;
            int inactivas = 0;

            foreach (var frecuencia in todasFrecuencias)
            {
                total++;
                foreach (var ruta in rutas)
                {
                    if (ruta.RutaId == frecuencia.RutaId)
                    {
                        frecuencia.Ruta = ruta;
                        break;
                    }
                }

                if (frecuencia.Ruta != null)
                {
                    foreach (var aeropuerto in aeropuertos)
                    {
                        if (aeropuerto.AeropuertoId == frecuencia.Ruta.AeropuertoOrigenId) { frecuencia.Ruta.Aeropuerto = aeropuerto; }
                        if (aeropuerto.AeropuertoId == frecuencia.Ruta.AeropuertoDestinoId) { frecuencia.Ruta.Aeropuerto1 = aeropuerto; }
                    }
                }

                if (frecuencia.Activa)
                {
                    activas++;
                }
                else
                {
                    inactivas++;
                }

                bool incluir = true;

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string origen = "";
                    string destino = "";
                    if (frecuencia.Ruta != null)
                    {
                        if (frecuencia.Ruta.Aeropuerto != null)
                        {
                            origen = frecuencia.Ruta.Aeropuerto.CodigoIATA ?? "";
                        }
                        if (frecuencia.Ruta.Aeropuerto1 != null)
                        {
                            destino = frecuencia.Ruta.Aeropuerto1.CodigoIATA ?? "";
                        }
                    }

                    string texto = frecuencia.FrecuenciaId + " " + origen + " " + destino;
                    if (!Contiene(texto, buscar))
                    {
                        incluir = false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(dia) && frecuencia.DiaSemana.ToString() != dia)
                {
                    incluir = false;
                }

                if (estado == "Activa" && !frecuencia.Activa)
                {
                    incluir = false;
                }
                if (estado == "Inactiva" && frecuencia.Activa)
                {
                    incluir = false;
                }

                if (incluir)
                {
                    listaFrecuencias.Add(frecuencia);
                }
            }

            ViewBag.ListaFrecuencias = listaFrecuencias;
            ViewBag.TotalFrecuencias = total;
            ViewBag.FrecuenciasActivas = activas;
            ViewBag.FrecuenciasInactivas = inactivas;
            ViewBag.Buscar = buscar ?? "";
            ViewBag.DiaSeleccionado = dia ?? "";
            ViewBag.EstadoSeleccionado = estado ?? "";
            return View();
        }

        // GET: Frecuencias/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencia.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            frecuencia.Ruta = db.Ruta.Find(frecuencia.RutaId);
            if (frecuencia.Ruta != null)
            {
                frecuencia.Ruta.Aeropuerto = db.Aeropuerto.Find(frecuencia.Ruta.AeropuertoOrigenId);
                frecuencia.Ruta.Aeropuerto1 = db.Aeropuerto.Find(frecuencia.Ruta.AeropuertoDestinoId);
            }
            return View(frecuencia);
        }

        // GET: Frecuencias/Create
        public ActionResult Create()
        {
            ViewBag.RutaId = CargarRutas(null);
            return View(new Frecuencia
            {
                DiaSemana = 1,
                HoraSalida = new TimeSpan(9, 0, 0),
                HoraLlegada = new TimeSpan(10, 0, 0),
                Activa = true
            });
        }

        // POST: Frecuencias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia frecuencia)
        {
            if (frecuencia.HoraLlegada <= frecuencia.HoraSalida)
            {
                ModelState.AddModelError("HoraLlegada", "La hora de llegada debe ser posterior a la salida.");
            }

            if (ModelState.IsValid)
            {
                db.Frecuencia.Add(frecuencia);
                db.SaveChanges();
                TempData["Ok"] = "Frecuencia registrada correctamente.";
                return RedirectToAction("Index");
            }

            ViewBag.RutaId = CargarRutas(frecuencia.RutaId);
            return View(frecuencia);
        }

        // GET: Frecuencias/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencia.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            ViewBag.RutaId = CargarRutas(frecuencia.RutaId);
            return View(frecuencia);
        }

        // POST: Frecuencias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia frecuencia)
        {
            if (frecuencia.HoraLlegada <= frecuencia.HoraSalida)
            {
                ModelState.AddModelError("HoraLlegada", "La hora de llegada debe ser posterior a la salida.");
            }

            if (ModelState.IsValid)
            {
                db.Entry(frecuencia).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Ok"] = "Cambios de frecuencia guardados correctamente.";
                return RedirectToAction("Index");
            }
            ViewBag.RutaId = CargarRutas(frecuencia.RutaId);
            return View(frecuencia);
        }

        // GET: Frecuencias/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencia.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            frecuencia.Ruta = db.Ruta.Find(frecuencia.RutaId);
            if (frecuencia.Ruta != null)
            {
                frecuencia.Ruta.Aeropuerto = db.Aeropuerto.Find(frecuencia.Ruta.AeropuertoOrigenId);
                frecuencia.Ruta.Aeropuerto1 = db.Aeropuerto.Find(frecuencia.Ruta.AeropuertoDestinoId);
            }
            ViewBag.VuelosAsociados = ContarVuelos(frecuencia.FrecuenciaId);
            return View(frecuencia);
        }

        // POST: Frecuencias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Frecuencia frecuencia = db.Frecuencia.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            if (ContarVuelos(id) > 0)
            {
                TempData["Error"] = "No se puede eliminar esta frecuencia porque tiene vuelos registrados. Desactivala si ya no debe programarse.";
                return RedirectToAction("Delete", new { id = id });
            }
            db.Frecuencia.Remove(frecuencia);
            db.SaveChanges();
            TempData["Ok"] = "La frecuencia se elimino correctamente.";
            return RedirectToAction("Index");
        }

        private int ContarVuelos(int frecuenciaId)
        {
            int total = 0;
            foreach (var vuelo in db.Vuelo)
            {
                if (vuelo.FrecuenciaId == frecuenciaId) { total++; }
            }
            return total;
        }

        private List<SelectListItem> CargarRutas(int? seleccionada)
        {
            var opciones = new List<SelectListItem>();
            var rutas = new List<Ruta>();
            var aeropuertos = new List<Aeropuerto>();
            foreach (var rutaItem in db.Ruta) { rutas.Add(rutaItem); }
            foreach (var aeropuertoItem in db.Aeropuerto) { aeropuertos.Add(aeropuertoItem); }
            foreach (var ruta in rutas)
            {
                Aeropuerto origen = null;
                Aeropuerto destino = null;
                foreach (var aeropuerto in aeropuertos)
                {
                    if (aeropuerto.AeropuertoId == ruta.AeropuertoOrigenId) { origen = aeropuerto; }
                    if (aeropuerto.AeropuertoId == ruta.AeropuertoDestinoId) { destino = aeropuerto; }
                }
                string codigoOrigen = origen != null ? origen.CodigoIATA : "ORIGEN";
                string codigoDestino = destino != null ? destino.CodigoIATA : "DESTINO";

                opciones.Add(new SelectListItem
                {
                    Value = ruta.RutaId.ToString(),
                    Text = codigoOrigen + " - " + codigoDestino,
                    Selected = seleccionada.HasValue && seleccionada.Value == ruta.RutaId
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
