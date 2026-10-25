using Con_Autenticacion.Models;
using Con_Autenticacion.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Con_Autenticacion.Controllers
{
    public class HomeController : Controller
    {
        private readonly AerolineaVIP_Context db = new AerolineaVIP_Context();

        public ActionResult Index()
        {
            var model = CrearModeloBusqueda();
            return View(model);
        }

        [HttpGet]
        public ActionResult Buscar(BusquedaVueloViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Aeropuertos = ObtenerAeropuertos();
                model.BusquedaRealizada = false;
                return View("Index", model);
            }

            if (model.AeropuertoOrigenId == null || model.AeropuertoDestinoId == null)
            {
                ModelState.AddModelError(string.Empty, "Debe seleccionar origen y destino.");
                model.Aeropuertos = ObtenerAeropuertos();
                model.BusquedaRealizada = false;
                return View("Index", model);
            }

            if (model.AeropuertoOrigenId == model.AeropuertoDestinoId)
            {
                ModelState.AddModelError(string.Empty, "El origen y el destino deben ser distintos.");
                model.Aeropuertos = ObtenerAeropuertos();
                model.BusquedaRealizada = false;
                return View("Index", model);
            }

            if (model.Fecha == null || model.Fecha < DateTime.Today)
            {
                ModelState.AddModelError(string.Empty, "La fecha de vuelo debe ser válida.");
                model.Aeropuertos = ObtenerAeropuertos();
                model.BusquedaRealizada = false;
                return View("Index", model);
            }

            if (model.CantidadPasajeros < 1 || model.CantidadPasajeros > 20)
            {
                ModelState.AddModelError(string.Empty, "La cantidad de pasajeros debe estar entre 1 y 20.");
                model.Aeropuertos = ObtenerAeropuertos();
                model.BusquedaRealizada = false;
                return View("Index", model);
            }

            var origen = db.Aeropuerto.Find(model.AeropuertoOrigenId.Value);
            var destino = db.Aeropuerto.Find(model.AeropuertoDestinoId.Value);

            if (origen == null || destino == null)
            {
                ModelState.AddModelError(string.Empty, "Los aeropuertos seleccionados no son válidos.");
                model.Aeropuertos = ObtenerAeropuertos();
                model.BusquedaRealizada = false;
                return View("Index", model);
            }

            var resultados = db.Vuelo
                .Include(v => v.Frecuencia)
                .Include(v => v.Frecuencia.Ruta)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave)
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.EstadoVuelo)
                .Where(v => v.FechaOperacion == model.Fecha.Value)
                .Where(v => v.Frecuencia.Ruta.AeropuertoOrigenId == model.AeropuertoOrigenId.Value)
                .Where(v => v.Frecuencia.Ruta.AeropuertoDestinoId == model.AeropuertoDestinoId.Value)
                .ToList()
                .Select(v => new VueloBusquedaViewModel
                {
                    VueloId = v.VueloId,
                    FechaOperacion = v.FechaOperacion,
                    Origen = v.Frecuencia != null && v.Frecuencia.Ruta != null && v.Frecuencia.Ruta.Aeropuerto != null ? v.Frecuencia.Ruta.Aeropuerto.CodigoIATA : string.Empty,
                    Destino = v.Frecuencia != null && v.Frecuencia.Ruta != null && v.Frecuencia.Ruta.Aeropuerto1 != null ? v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA : string.Empty,
                    HoraSalida = v.Frecuencia != null ? v.Frecuencia.HoraSalida.ToString() : string.Empty,
                    HoraLlegada = v.Frecuencia != null ? v.Frecuencia.HoraLlegada.ToString() : string.Empty,
                    EstadoVueloCodigo = v.EstadoVueloCodigo,
                    Matricula = v.Aeronave != null ? v.Aeronave.Matricula : string.Empty,
                    Capacidad = v.Aeronave != null && v.Aeronave.ModeloAeronave != null ? v.Aeronave.ModeloAeronave.CapacidadAsientos : 0,
                    Ocupados = 0,
                    Disponibles = v.Aeronave != null && v.Aeronave.ModeloAeronave != null ? v.Aeronave.ModeloAeronave.CapacidadAsientos : 0
                })
                .ToList();

            model.Resultados = resultados;
            model.BusquedaRealizada = true;
            model.Aeropuertos = ObtenerAeropuertos();
            return View("Index", model);
        }

        private BusquedaVueloViewModel CrearModeloBusqueda()
        {
            return new BusquedaVueloViewModel
            {
                Fecha = DateTime.Today,
                CantidadPasajeros = 1,
                Aeropuertos = ObtenerAeropuertos(),
                BusquedaRealizada = false
            };
        }

        private IEnumerable<SelectListItem> ObtenerAeropuertos()
        {
            return db.Aeropuerto
                .Where(a => a.Activo)
                .OrderBy(a => a.CodigoIATA)
                .Select(a => new SelectListItem
                {
                    Value = a.AeropuertoId.ToString(),
                    Text = a.CodigoIATA + " - " + a.Nombre
                })
                .ToList();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
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