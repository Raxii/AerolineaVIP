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
    public class ItinerariosController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Itinerarios
        public ActionResult Index(string buscar, string estadoCodigo)
        {
            var listaItinerarios = new List<Itinerario>();
            var todosItinerarios = new List<Itinerario>();
            var todosTramos = new List<TramoItinerario>();
            var estados = new List<EstadoItinerario>();
            var tramosPorItinerario = new Dictionary<int, int>();
            int total = 0;
            int disponibles = 0;

            foreach (var estado in db.EstadoItinerario) { estados.Add(estado); }
            foreach (var itinerarioItem in db.Itinerario) { todosItinerarios.Add(itinerarioItem); }
            foreach (var tramoItem in db.TramoItinerario) { todosTramos.Add(tramoItem); }

            foreach (var tramo in todosTramos)
            {
                if (!tramosPorItinerario.ContainsKey(tramo.ItinerarioId))
                {
                    tramosPorItinerario[tramo.ItinerarioId] = 0;
                }
                tramosPorItinerario[tramo.ItinerarioId]++;
            }

            foreach (var itinerario in todosItinerarios)
            {
                total++;
                foreach (var estado in estados)
                {
                    if (estado.Codigo == itinerario.EstadoItinerarioCodigo)
                    {
                        itinerario.EstadoItinerario = estado;
                        break;
                    }
                }

                if (itinerario.EstadoItinerario != null &&
                    Contiene(itinerario.EstadoItinerario.Descripcion, "DISPONIBLE"))
                {
                    disponibles++;
                }

                bool incluir = true;
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string texto = (itinerario.Referencia ?? "") + " " + (itinerario.Nombre ?? "");
                    if (!Contiene(texto, buscar))
                    {
                        incluir = false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(estadoCodigo) &&
                    !string.Equals(itinerario.EstadoItinerarioCodigo, estadoCodigo, StringComparison.OrdinalIgnoreCase))
                {
                    incluir = false;
                }

                if (incluir)
                {
                    listaItinerarios.Add(itinerario);
                }
            }

            ViewBag.ListaItinerarios = listaItinerarios;
            ViewBag.Estados = estados;
            ViewBag.TramosPorItinerario = tramosPorItinerario;
            ViewBag.TotalItinerarios = total;
            ViewBag.ItinerariosDisponibles = disponibles;
            ViewBag.Buscar = buscar ?? "";
            ViewBag.EstadoSeleccionado = estadoCodigo ?? "";
            return View();
        }

        // GET: Itinerarios/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            itinerario.EstadoItinerario = db.EstadoItinerario.Find(itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerarios/Create
        public ActionResult Create()
        {
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion");
            return View();
        }

        // POST: Itinerarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ItinerarioId,Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario itinerario)
        {
            if (ModelState.IsValid)
            {
                db.Itinerario.Add(itinerario);
                db.SaveChanges();
                TempData["Ok"] = "Itinerario registrado correctamente.";
                return RedirectToAction("Index");
            }

            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerarios/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // POST: Itinerarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ItinerarioId,Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario itinerario)
        {
            if (ModelState.IsValid)
            {
                db.Entry(itinerario).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Ok"] = "Cambios de itinerario guardados correctamente.";
                return RedirectToAction("Index");
            }
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerarios/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            itinerario.EstadoItinerario = db.EstadoItinerario.Find(itinerario.EstadoItinerarioCodigo);
            ViewBag.TramosAsociados = ContarTramos(itinerario.ItinerarioId);
            return View(itinerario);
        }

        // POST: Itinerarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            if (ContarTramos(id) > 0)
            {
                TempData["Error"] = "No se puede eliminar este itinerario porque tiene tramos asociados. Elimina primero sus tramos o cambia el estado del itinerario.";
                return RedirectToAction("Delete", new { id = id });
            }
            db.Itinerario.Remove(itinerario);
            db.SaveChanges();
            TempData["Ok"] = "El itinerario se elimino correctamente.";
            return RedirectToAction("Index");
        }

        private int ContarTramos(int itinerarioId)
        {
            int total = 0;
            foreach (var tramo in db.TramoItinerario)
            {
                if (tramo.ItinerarioId == itinerarioId) { total++; }
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
