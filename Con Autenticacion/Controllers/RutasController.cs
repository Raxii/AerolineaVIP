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
    public class RutasController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Rutas
        public ActionResult Index(string buscar, string estado)
        {
            var listaRutas = new List<Ruta>();
            var todasRutas = new List<Ruta>();
            var aeropuertos = new List<Aeropuerto>();
            foreach (var aeropuertoItem in db.Aeropuerto) { aeropuertos.Add(aeropuertoItem); }
            foreach (var rutaItem in db.Ruta) { todasRutas.Add(rutaItem); }
            int total = 0;
            int activas = 0;
            int inactivas = 0;

            foreach (var ruta in todasRutas)
            {
                total++;
                foreach (var aeropuerto in aeropuertos)
                {
                    if (aeropuerto.AeropuertoId == ruta.AeropuertoOrigenId) { ruta.Aeropuerto = aeropuerto; }
                    if (aeropuerto.AeropuertoId == ruta.AeropuertoDestinoId) { ruta.Aeropuerto1 = aeropuerto; }
                }

                if (ruta.Activa)
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
                    string origen = ruta.Aeropuerto != null ? ruta.Aeropuerto.CodigoIATA : "";
                    string destino = ruta.Aeropuerto1 != null ? ruta.Aeropuerto1.CodigoIATA : "";
                    string texto = ruta.RutaId + " " + origen + " " + destino;
                    if (!Contiene(texto, buscar))
                    {
                        incluir = false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    if (estado == "Activa" && !ruta.Activa)
                    {
                        incluir = false;
                    }
                    if (estado == "Inactiva" && ruta.Activa)
                    {
                        incluir = false;
                    }
                }

                if (incluir)
                {
                    listaRutas.Add(ruta);
                }
            }

            ViewBag.ListaRutas = listaRutas;
            ViewBag.TotalRutas = total;
            ViewBag.RutasActivas = activas;
            ViewBag.RutasInactivas = inactivas;
            ViewBag.Buscar = buscar ?? "";
            ViewBag.EstadoSeleccionado = estado ?? "";
            return View();
        }

        // GET: Rutas/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            ruta.Aeropuerto = db.Aeropuerto.Find(ruta.AeropuertoOrigenId);
            ruta.Aeropuerto1 = db.Aeropuerto.Find(ruta.AeropuertoDestinoId);
            ViewBag.FrecuenciasAsociadas = ContarFrecuencias(ruta.RutaId);
            return View(ruta);
        }

        // GET: Rutas/Create
        public ActionResult Create()
        {
            ViewBag.AeropuertoDestinoId = CargarAeropuertos(null);
            ViewBag.AeropuertoOrigenId = CargarAeropuertos(null);
            return View(new Ruta { Activa = true });
        }

        // POST: Rutas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,Activa")] Ruta ruta)
        {
            if (ruta.AeropuertoOrigenId == ruta.AeropuertoDestinoId)
            {
                ModelState.AddModelError("AeropuertoDestinoId", "El origen y el destino deben ser diferentes.");
            }

            if (ModelState.IsValid)
            {
                db.Ruta.Add(ruta);
                db.SaveChanges();
                TempData["Ok"] = "Ruta registrada correctamente.";
                return RedirectToAction("Index");
            }

            ruta.Aeropuerto = db.Aeropuerto.Find(ruta.AeropuertoOrigenId);
            ruta.Aeropuerto1 = db.Aeropuerto.Find(ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoDestinoId = CargarAeropuertos(ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = CargarAeropuertos(ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            ruta.Aeropuerto = db.Aeropuerto.Find(ruta.AeropuertoOrigenId);
            ruta.Aeropuerto1 = db.Aeropuerto.Find(ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoDestinoId = CargarAeropuertos(ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = CargarAeropuertos(ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // POST: Rutas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,Activa")] Ruta ruta)
        {
            if (ruta.AeropuertoOrigenId == ruta.AeropuertoDestinoId)
            {
                ModelState.AddModelError("AeropuertoDestinoId", "El origen y el destino deben ser diferentes.");
            }

            var rutaActual = db.Ruta.Find(ruta.RutaId);
            if (rutaActual == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                rutaActual.AeropuertoOrigenId = ruta.AeropuertoOrigenId;
                rutaActual.AeropuertoDestinoId = ruta.AeropuertoDestinoId;
                rutaActual.Activa = ruta.Activa;
                db.SaveChanges();
                TempData["Ok"] = "Cambios de ruta guardados correctamente.";
                return RedirectToAction("Index");
            }
            ViewBag.AeropuertoDestinoId = CargarAeropuertos(ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = CargarAeropuertos(ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            ruta.Aeropuerto = db.Aeropuerto.Find(ruta.AeropuertoOrigenId);
            ruta.Aeropuerto1 = db.Aeropuerto.Find(ruta.AeropuertoDestinoId);
            ViewBag.FrecuenciasAsociadas = ContarFrecuencias(ruta.RutaId);
            return View(ruta);
        }

        // POST: Rutas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            if (ContarFrecuencias(id) > 0)
            {
                TempData["Error"] = "No se puede eliminar esta ruta porque tiene frecuencias asociadas. Desactiva la ruta si ya no debe utilizarse.";
                return RedirectToAction("Delete", new { id = id });
            }
            db.Ruta.Remove(ruta);
            db.SaveChanges();
            TempData["Ok"] = "La ruta se elimino correctamente.";
            return RedirectToAction("Index");
        }

        private List<SelectListItem> CargarAeropuertos(int? seleccionado)
        {
            var opciones = new List<SelectListItem>();
            foreach (var aeropuerto in db.Aeropuerto)
            {
                string texto = (aeropuerto.CodigoIATA ?? "") + " - " +
                               (aeropuerto.Localidad ?? "") + " | " +
                               (aeropuerto.Nombre ?? "");
                opciones.Add(new SelectListItem
                {
                    Value = aeropuerto.AeropuertoId.ToString(),
                    Text = texto,
                    Selected = seleccionado.HasValue && seleccionado.Value == aeropuerto.AeropuertoId
                });
            }
            return opciones;
        }

        private int ContarFrecuencias(int rutaId)
        {
            int total = 0;
            foreach (var frecuencia in db.Frecuencia)
            {
                if (frecuencia.RutaId == rutaId) { total++; }
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
