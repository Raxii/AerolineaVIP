using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
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
            var vuelos = new List<Vuelo>();

            // Se cargan los vuelos mediante un recorrido tradicional.
            foreach (var vuelo in db.Vuelo)
            {
                vuelos.Add(vuelo);
            }

            // Orden descendente por fecha y ascendente por identificador.
            vuelos.Sort(delegate (Vuelo a, Vuelo b)
            {
                var comparacionFecha = b.FechaOperacion.CompareTo(a.FechaOperacion);
                if (comparacionFecha != 0)
                    return comparacionFecha;

                return a.VueloId.CompareTo(b.VueloId);
            });

            // Los indicadores se calculan en el controlador y se envían
            // a Razor mediante ViewBag.
            int vuelosProgramados = 0;
            int vuelosConfirmados = 0;

            foreach (var vuelo in vuelos)
            {
                if (vuelo.EstadoVueloCodigo == "PROGRAMADO")
                    vuelosProgramados++;

                if (vuelo.EstadoVueloCodigo == "CONFIRMADO")
                    vuelosConfirmados++;
            }

            ViewBag.TotalVuelos = vuelos.Count;
            ViewBag.VuelosProgramados = vuelosProgramados;
            ViewBag.VuelosConfirmados = vuelosConfirmados;

            return View(vuelos);
        }

        // PLANTILLA #5: Buscar vuelos por origen, destino, fecha y pasajeros.
        // Conserva los nombres de los parámetros y ViewBag del código de Israel.
        //
        // IMPORTANTE: esta acción NO utiliza operadores LINQ de consulta
        // (.Where/.Select/.OrderBy/.Include/.FirstOrDefault) ni SQL crudo
        // (SqlQuery/ExecuteSqlCommand). Recorre los DbSet con foreach y aplica
        // las reglas de negocio con condicionales, igual que CrudController y
        // ReportesController. Usa helpers propios (...SinLinq) para no afectar
        // a Resultados/DisponibilidadAsientos/Seleccionar, que siguen usando
        // ObtenerAsientosOcupados/ObtenerVuelosFiltrados sin cambios.
        public ActionResult Buscar(int? AeropuertoOrigenId, int? AeropuertoDestinoId,
            DateTime? Fecha, int CantidadPasajeros = 1)
        {
            var errores = new List<string>();

            if (CantidadPasajeros < 1)
            {
                errores.Add("La cantidad de pasajeros debe ser como mínimo 1.");
                CantidadPasajeros = 1;
            }

            if (AeropuertoOrigenId.HasValue && AeropuertoDestinoId.HasValue &&
                AeropuertoOrigenId.Value == AeropuertoDestinoId.Value)
            {
                errores.Add("El aeropuerto de origen y el de destino no pueden ser iguales.");
            }

            if (Fecha.HasValue && Fecha.Value == DateTime.MinValue)
            {
                errores.Add("La fecha ingresada no es válida.");
            }

            // Solo aeropuertos activos, recorridos con foreach (sin .Where()).
            var aeropuertosActivos = ObtenerAeropuertosActivosSinLinq();

            Aeropuerto origen = null;
            Aeropuerto destino = null;
            foreach (var aeropuerto in aeropuertosActivos)
            {
                if (AeropuertoOrigenId.HasValue && aeropuerto.AeropuertoId == AeropuertoOrigenId.Value)
                    origen = aeropuerto;
                if (AeropuertoDestinoId.HasValue && aeropuerto.AeropuertoId == AeropuertoDestinoId.Value)
                    destino = aeropuerto;
            }

            if (AeropuertoOrigenId.HasValue && origen == null)
                errores.Add("El aeropuerto de origen no existe o no está activo.");
            if (AeropuertoDestinoId.HasValue && destino == null)
                errores.Add("El aeropuerto de destino no existe o no está activo.");

            var ocupados = ObtenerAsientosOcupadosSinLinq();
            var resultados = errores.Count == 0
                ? ObtenerVuelosFiltradosSinLinq(AeropuertoOrigenId, AeropuertoDestinoId,
                    Fecha, CantidadPasajeros, ocupados)
                : new List<Vuelo>();

            // Tarifa real (tabla fija de TarifaCalculos), no se inventa ningún precio.
            var tarifaPorVuelo = new Dictionary<int, decimal?>();
            var totalPorVuelo = new Dictionary<int, decimal?>();
            var descuentoPorVuelo = new Dictionary<int, bool>();

            foreach (var vuelo in resultados)
            {
                string origenIata = null;
                string destinoIata = null;
                if (vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
                {
                    if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
                        origenIata = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA;
                    if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
                        destinoIata = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA;
                }

                var tarifa = TarifaCalculos.ObtenerTarifaPorTramo(origenIata, destinoIata);
                bool descuentoAplicado;
                var total = TarifaCalculos.CalcularTotalReserva(tarifa, CantidadPasajeros, out descuentoAplicado);

                tarifaPorVuelo[vuelo.VueloId] = tarifa;
                totalPorVuelo[vuelo.VueloId] = total;
                descuentoPorVuelo[vuelo.VueloId] = descuentoAplicado;
            }

            ViewBag.AeropuertoOrigenId = AeropuertoOrigenId;
            ViewBag.AeropuertoDestinoId = AeropuertoDestinoId;
            ViewBag.Fecha = Fecha;
            ViewBag.CantidadPasajeros = CantidadPasajeros;
            ViewBag.HayResultados = resultados.Count > 0;
            ViewBag.TotalResultados = resultados.Count;
            ViewBag.Errores = errores;

            var listaAeropuertos = new List<SelectListItem>();
            foreach (var aeropuerto in aeropuertosActivos)
            {
                listaAeropuertos.Add(new SelectListItem
                {
                    Value = aeropuerto.AeropuertoId.ToString(),
                    Text = aeropuerto.Localidad + " (" + aeropuerto.CodigoIATA + ")"
                });
            }
            ViewBag.Aeropuertos = listaAeropuertos;

            ViewBag.OcupadosPorVuelo = ObtenerConteos(ocupados);
            ViewBag.TarifaPorVuelo = tarifaPorVuelo;
            ViewBag.TotalPorVuelo = totalPorVuelo;
            ViewBag.DescuentoPorVuelo = descuentoPorVuelo;
            ViewBag.Moneda = TarifaCalculos.Moneda;

            return View(resultados);
        }

        // PLANTILLA #6: Resultados con los mismos filtros que Buscar.
        //
        // IMPORTANTE: igual que Buscar(), esta acción NO utiliza operadores LINQ
        // de consulta (.Where/.Select/.OrderBy/.Include/.FirstOrDefault) ni SQL
        // crudo contra la base de datos. ObtenerAsientosOcupados() y
        // ObtenerVuelosFiltrados() (con LINQ) se dejan intactos porque los sigue
        // usando PrepararDatosDeAsientos (DisponibilidadAsientos/Seleccionar); en
        // su lugar se reutiliza ObtenerAsientosOcupadosSinLinq() (misma regla de
        // negocio) y se agrega ObtenerVuelosFiltradosResultadosSinLinq(), propio
        // de esta pantalla, porque Resultados exige reglas que Buscar no aplica:
        // día autorizado por Frecuencia.DiaSemana, Frecuencia.Activa y orden por
        // horario de salida real.
        public ActionResult Resultados(int? AeropuertoOrigenId, int? AeropuertoDestinoId,
            DateTime? Fecha, int CantidadPasajeros = 1)
        {
            var errores = new List<string>();

            if (CantidadPasajeros < 1)
            {
                errores.Add("La cantidad de pasajeros debe ser como mínimo 1.");
                CantidadPasajeros = 1;
            }

            if (AeropuertoOrigenId.HasValue && AeropuertoDestinoId.HasValue &&
                AeropuertoOrigenId.Value == AeropuertoDestinoId.Value)
            {
                errores.Add("El aeropuerto de origen y el de destino no pueden ser iguales.");
            }

            if (Fecha.HasValue && Fecha.Value == DateTime.MinValue)
            {
                errores.Add("La fecha ingresada no es válida.");
            }

            // Solo aeropuertos activos, recorridos con foreach (sin .Where()/.FirstOrDefault()).
            var aeropuertosActivos = ObtenerAeropuertosActivosSinLinq();

            Aeropuerto origen = null;
            Aeropuerto destino = null;
            foreach (var aeropuerto in aeropuertosActivos)
            {
                if (AeropuertoOrigenId.HasValue && aeropuerto.AeropuertoId == AeropuertoOrigenId.Value)
                    origen = aeropuerto;
                if (AeropuertoDestinoId.HasValue && aeropuerto.AeropuertoId == AeropuertoDestinoId.Value)
                    destino = aeropuerto;
            }

            if (AeropuertoOrigenId.HasValue && origen == null)
                errores.Add("El aeropuerto de origen no existe o no está activo.");
            if (AeropuertoDestinoId.HasValue && destino == null)
                errores.Add("El aeropuerto de destino no existe o no está activo.");

            var ocupados = ObtenerAsientosOcupadosSinLinq();
            var resultados = errores.Count == 0
                ? ObtenerVuelosFiltradosResultadosSinLinq(AeropuertoOrigenId, AeropuertoDestinoId,
                    Fecha, CantidadPasajeros, ocupados)
                : new List<Vuelo>();

            // Capacidad/ocupados/disponibles reales por vuelo (ninguna cifra inventada).
            var capacidadPorVuelo = new Dictionary<int, int>();
            var disponiblesPorVuelo = new Dictionary<int, int>();
            foreach (var vuelo in resultados)
            {
                var capacidad = 0;
                if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                    capacidad = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;

                var ocupadosVuelo = ocupados.ContainsKey(vuelo.VueloId) ? ocupados[vuelo.VueloId].Count : 0;

                capacidadPorVuelo[vuelo.VueloId] = capacidad;
                disponiblesPorVuelo[vuelo.VueloId] = capacidad - ocupadosVuelo;
            }

            // Tarifa real (misma tabla fija que usa Buscar); TarifaCalculos.cs no se modifica.
            var tarifaPorVuelo = new Dictionary<int, decimal?>();
            var totalPorVuelo = new Dictionary<int, decimal?>();
            var descuentoPorVuelo = new Dictionary<int, bool>();
            foreach (var vuelo in resultados)
            {
                string origenIata = null;
                string destinoIata = null;
                if (vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
                {
                    if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
                        origenIata = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA;
                    if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
                        destinoIata = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA;
                }

                var tarifa = TarifaCalculos.ObtenerTarifaPorTramo(origenIata, destinoIata);
                bool descuentoAplicado;
                var total = TarifaCalculos.CalcularTotalReserva(tarifa, CantidadPasajeros, out descuentoAplicado);

                tarifaPorVuelo[vuelo.VueloId] = tarifa;
                totalPorVuelo[vuelo.VueloId] = total;
                descuentoPorVuelo[vuelo.VueloId] = descuentoAplicado;
            }

            ViewBag.AeropuertoOrigenId = AeropuertoOrigenId;
            ViewBag.AeropuertoDestinoId = AeropuertoDestinoId;
            ViewBag.AeropuertoOrigen = origen == null ? "" : origen.Nombre + " (" + origen.CodigoIATA + ")";
            ViewBag.AeropuertoDestino = destino == null ? "" : destino.Nombre + " (" + destino.CodigoIATA + ")";
            ViewBag.AeropuertoOrigenIATA = origen == null ? "" : origen.CodigoIATA;
            ViewBag.AeropuertoDestinoIATA = destino == null ? "" : destino.CodigoIATA;
            ViewBag.Fecha = Fecha;
            ViewBag.CantidadPasajeros = CantidadPasajeros;
            ViewBag.Errores = errores;
            ViewBag.Vuelos = resultados;
            ViewBag.HayResultados = resultados.Count > 0;
            ViewBag.TotalResultados = resultados.Count;
            ViewBag.OcupadosPorVuelo = ObtenerConteos(ocupados);
            ViewBag.CapacidadPorVuelo = capacidadPorVuelo;
            ViewBag.DisponiblesPorVuelo = disponiblesPorVuelo;
            ViewBag.TarifaPorVuelo = tarifaPorVuelo;
            ViewBag.TotalPorVuelo = totalPorVuelo;
            ViewBag.DescuentoPorVuelo = descuentoPorVuelo;
            ViewBag.Moneda = TarifaCalculos.Moneda;

            return View(resultados);
        }

        // PLANTILLA #7: Consulta del estado de los asientos de un vuelo.
        // Reescrita para esta tarea: usa helpers EXCLUSIVOS sin LINQ contra
        // SQL Server, valida el estado operativo del vuelo y aplica el
        // límite de 7 asientos propio de esta pantalla. No llama a
        // ObtenerVueloCompleto() ni a PrepararDatosDeAsientos() porque ambos
        // también los usa Seleccionar() (y ObtenerVueloCompleto, además,
        // Details()/Delete()): modificarlos cambiaría el comportamiento de
        // esas acciones, que esta tarea no debe tocar.
        public ActionResult DisponibilidadAsientos(int? vueloId, int? cantidadPasajeros)
        {
            if (!vueloId.HasValue) return RedirectToAction("Buscar");

            var vuelo = ObtenerVueloParaDisponibilidadSinLinq(vueloId.Value);
            if (vuelo == null) return HttpNotFound();

            PrepararDisponibilidadAsientosSinLinq(vuelo, cantidadPasajeros);

            if (TempData["ErrorDisponibilidad"] != null)
                ViewBag.ErrorValidacion = TempData["ErrorDisponibilidad"];

            return View(vuelo);
        }

        // PLANTILLA #8: Pantalla de selección de vuelo y asientos. Reescrita
        // para esta tarea: usa un helper EXCLUSIVO (ObtenerVueloParaSeleccionSinLinq)
        // en vez de ObtenerVueloCompleto(), porque ObtenerVueloCompleto() también
        // lo usan Details()/Delete() y modificarlo cambiaría el comportamiento de
        // esas acciones. PrepararDatosDeAsientos() sí se modifica más abajo en
        // este mismo archivo porque, desde la tarea anterior, ya es un método
        // EXCLUSIVO de esta acción (DisponibilidadAsientos usa sus propios
        // helpers PrepararDisponibilidadAsientosSinLinq/ObtenerVueloParaDisponibilidadSinLinq).
        //
        // Se agregan los parámetros opcionales "cantidadPasajeros" y
        // "codigosAsientos" para conservar, al llegar desde
        // DisponibilidadAsientos -> ConfirmarSeleccionAsientos (que ya validó
        // esos asientos en el servidor), la cantidad de pasajeros solicitada y
        // los asientos elegidos. Si faltan (por ejemplo, un enlace directo con
        // solo vueloId), la pantalla sigue funcionando igual que antes.
        // No se registra ninguna reserva en esta acción.
        public ActionResult Seleccionar(int? vueloId, int? cantidadPasajeros, string codigosAsientos)
        {
            if (!vueloId.HasValue || vueloId.Value <= 0) return RedirectToAction("Buscar");

            var vuelo = ObtenerVueloParaSeleccionSinLinq(vueloId.Value);
            if (vuelo == null) return HttpNotFound();

            // Si la cantidad no llega desde las pantallas anteriores se asume 1,
            // igual que el valor predeterminado de Buscar()/Resultados(). No se
            // altera una cantidad válida recibida (incluido el caso > 7, que la
            // propia vista rechaza con un mensaje).
            if (!cantidadPasajeros.HasValue || cantidadPasajeros.Value < 1)
                cantidadPasajeros = 1;

            PrepararDatosDeAsientos(vuelo, true, cantidadPasajeros, codigosAsientos);

            if (TempData["ErrorSeleccion"] != null)
                ViewBag.ErrorValidacion = TempData["ErrorSeleccion"];

            return View(vuelo);
        }

        // Validación de SERVIDOR del botón "Continuar" de esta pantalla
        // (Seleccionar). No crea un controlador nuevo: si todas las
        // comprobaciones pasan, redirige a PasajeroesController.Create(), que es
        // la única acción existente en el proyecto para registrar un pasajero.
        // LIMITACIÓN DEL MODELO (no se corrige aquí porque PasajeroesController
        // está fuera del alcance de esta tarea): Create() es un controlador CRUD
        // genérico, sin parámetros, que no asocia el pasajero con el vuelo, los
        // asientos ni una Reserva/ReservaPasajeroVuelo. Por eso vueloId,
        // cantidadPasajeros y codigosAsientos se envían por querystring (para que
        // queden visibles/trazables), pero Create() no los consume todavía.
        // No se registra ninguna reserva en este método.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ContinuarConSeleccion(int vueloId, int? cantidadPasajeros, string codigosAsientos)
        {
            const int limitePantalla = 7;

            var vuelo = ObtenerVueloParaSeleccionSinLinq(vueloId);
            if (vuelo == null) return HttpNotFound();

            if (vuelo.EstadoVueloCodigo != "PROGRAMADO" && vuelo.EstadoVueloCodigo != "CONFIRMADO")
            {
                TempData["ErrorSeleccion"] = "El vuelo se encuentra en estado " + vuelo.EstadoVueloCodigo +
                    " y ya no admite continuar con la selección de asientos.";
                return RedirectToAction("Seleccionar", new { vueloId, cantidadPasajeros, codigosAsientos });
            }

            var modeloContinuar = vuelo.Aeronave != null ? vuelo.Aeronave.ModeloAeronave : null;
            if (modeloContinuar == null)
            {
                TempData["ErrorSeleccion"] = "El vuelo no tiene una aeronave con modelo asignado.";
                return RedirectToAction("Seleccionar", new { vueloId, cantidadPasajeros, codigosAsientos });
            }

            var cantidadSolicitadaContinuar = cantidadPasajeros.HasValue && cantidadPasajeros.Value > 0
                ? cantidadPasajeros.Value : 0;

            if (cantidadSolicitadaContinuar <= 0 || cantidadSolicitadaContinuar > limitePantalla)
            {
                TempData["ErrorSeleccion"] = "La cantidad de pasajeros solicitada no es válida para esta " +
                    "pantalla (máximo " + limitePantalla + " asientos por operación).";
                return RedirectToAction("Seleccionar", new { vueloId, cantidadPasajeros, codigosAsientos });
            }

            var codigosContinuar = new List<string>();
            if (!string.IsNullOrWhiteSpace(codigosAsientos))
            {
                foreach (var crudo in codigosAsientos.Split(','))
                {
                    var codigo = crudo.Trim();
                    if (codigo.Length > 0) codigosContinuar.Add(codigo);
                }
            }

            if (codigosContinuar.Count != cantidadSolicitadaContinuar)
            {
                TempData["ErrorSeleccion"] = "Debes seleccionar exactamente " + cantidadSolicitadaContinuar +
                    " asiento(s) para continuar.";
                return RedirectToAction("Seleccionar", new { vueloId, cantidadPasajeros, codigosAsientos });
            }

            // Asientos reales de la aeronave asignada (sin LINQ).
            var asientosAeronaveContinuar = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            foreach (var asiento in db.Asiento)
            {
                if (asiento.ModeloAeronaveId == modeloContinuar.ModeloAeronaveId)
                    asientosAeronaveContinuar[(asiento.Codigo ?? "").Trim()] = asiento.NumeroAsiento;
            }

            // Disponibilidad vigente consultada en este instante (nueva
            // comprobación obligatoria antes de continuar con el registro del
            // pasajero: no se confía en lo que se mostró antes en pantalla).
            var estadosVigentesContinuar = ObtenerEstadosAsignacionVueloSinLinq(vueloId);

            var vistosContinuar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var codigo in codigosContinuar)
            {
                if (!vistosContinuar.Add(codigo))
                {
                    TempData["ErrorSeleccion"] = "El asiento " + codigo + " fue seleccionado más de una vez.";
                    return RedirectToAction("Seleccionar", new { vueloId, cantidadPasajeros, codigosAsientos });
                }

                byte numero;
                if (!asientosAeronaveContinuar.TryGetValue(codigo, out numero))
                {
                    TempData["ErrorSeleccion"] = "El asiento " + codigo +
                        " no pertenece a la aeronave asignada a este vuelo.";
                    return RedirectToAction("Seleccionar", new { vueloId, cantidadPasajeros, codigosAsientos });
                }

                if (estadosVigentesContinuar.ContainsKey(numero))
                {
                    TempData["ErrorSeleccion"] = "El asiento " + codigo + " ya no está disponible (" +
                        estadosVigentesContinuar[numero] + "). Selecciona otro asiento.";
                    return RedirectToAction("Seleccionar", new { vueloId, cantidadPasajeros, codigosAsientos });
                }
            }

            // Todas las validaciones del servidor se cumplieron para este
            // instante. No se registra ninguna reserva aquí: se avanza al
            // registro del pasajero (PasajeroesController.Create()).
            return RedirectToAction("Create", "Pasajeroes", new { vueloId, cantidadPasajeros, codigosAsientos });
        }

        // Validación de SERVIDOR del botón "Continuar" de DisponibilidadAsientos.
        // No crea un controlador nuevo ni modifica Seleccionar(): si todas las
        // comprobaciones pasan, redirige a la acción Seleccionar ya existente,
        // que ya conserva cantidadPasajeros y codigosAsientos (ver Seleccionar()).
        // No se registra ninguna reserva en este método: la disponibilidad real se
        // volverá a comprobar en ContinuarConSeleccion() cuando el usuario avance
        // desde la pantalla de Seleccionar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarSeleccionAsientos(int vueloId, int? cantidadPasajeros, string codigosAsientos)
        {
            const int limitePantalla = 7;

            var vuelo = ObtenerVueloParaDisponibilidadSinLinq(vueloId);
            if (vuelo == null) return HttpNotFound();

            if (vuelo.EstadoVueloCodigo != "PROGRAMADO" && vuelo.EstadoVueloCodigo != "CONFIRMADO")
            {
                TempData["ErrorDisponibilidad"] = "El vuelo se encuentra en estado " + vuelo.EstadoVueloCodigo +
                    " y ya no admite continuar con la selección de asientos.";
                return RedirectToAction("DisponibilidadAsientos", new { vueloId, cantidadPasajeros });
            }

            var modelo = vuelo.Aeronave != null ? vuelo.Aeronave.ModeloAeronave : null;
            if (modelo == null)
            {
                TempData["ErrorDisponibilidad"] = "El vuelo no tiene una aeronave con modelo asignado.";
                return RedirectToAction("DisponibilidadAsientos", new { vueloId, cantidadPasajeros });
            }

            var cantidadSolicitada = cantidadPasajeros.HasValue && cantidadPasajeros.Value > 0
                ? cantidadPasajeros.Value : 0;

            if (cantidadSolicitada <= 0 || cantidadSolicitada > limitePantalla)
            {
                TempData["ErrorDisponibilidad"] = "La cantidad de pasajeros solicitada no es válida para esta " +
                    "pantalla (máximo " + limitePantalla + " asientos por operación).";
                return RedirectToAction("DisponibilidadAsientos", new { vueloId, cantidadPasajeros });
            }

            var codigos = new List<string>();
            if (!string.IsNullOrWhiteSpace(codigosAsientos))
            {
                foreach (var crudo in codigosAsientos.Split(','))
                {
                    var codigo = crudo.Trim();
                    if (codigo.Length > 0) codigos.Add(codigo);
                }
            }

            if (codigos.Count != cantidadSolicitada)
            {
                TempData["ErrorDisponibilidad"] = "Debes seleccionar exactamente " + cantidadSolicitada +
                    " asiento(s) para continuar.";
                return RedirectToAction("DisponibilidadAsientos", new { vueloId, cantidadPasajeros });
            }

            // Asientos reales de la aeronave asignada (sin LINQ).
            var asientosAeronave = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            foreach (var asiento in db.Asiento)
            {
                if (asiento.ModeloAeronaveId == modelo.ModeloAeronaveId)
                    asientosAeronave[(asiento.Codigo ?? "").Trim()] = asiento.NumeroAsiento;
            }

            // Ocupación vigente consultada en este instante (no la que se
            // mostró antes en pantalla): nueva comprobación obligatoria.
            var estadosVigentes = ObtenerEstadosAsignacionVueloSinLinq(vueloId);

            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var codigo in codigos)
            {
                if (!vistos.Add(codigo))
                {
                    TempData["ErrorDisponibilidad"] = "El asiento " + codigo + " fue seleccionado más de una vez.";
                    return RedirectToAction("DisponibilidadAsientos", new { vueloId, cantidadPasajeros });
                }

                byte numero;
                if (!asientosAeronave.TryGetValue(codigo, out numero))
                {
                    TempData["ErrorDisponibilidad"] = "El asiento " + codigo +
                        " no pertenece a la aeronave asignada a este vuelo.";
                    return RedirectToAction("DisponibilidadAsientos", new { vueloId, cantidadPasajeros });
                }

                if (estadosVigentes.ContainsKey(numero))
                {
                    TempData["ErrorDisponibilidad"] = "El asiento " + codigo + " ya no está disponible (" +
                        estadosVigentes[numero] + ").";
                    return RedirectToAction("DisponibilidadAsientos", new { vueloId, cantidadPasajeros });
                }
            }

            // Todas las validaciones del servidor se cumplieron para este
            // instante. No se registra ninguna reserva aquí. Se envían también
            // cantidadPasajeros y codigosAsientos para que Seleccionar() conserve
            // la selección ya validada (antes solo se enviaba vueloId).
            return RedirectToAction("Seleccionar", new { vueloId, cantidadPasajeros, codigosAsientos = string.Join(",", codigos) });
        }

        // PLANTILLA #18: Vista de administración elaborada por Israel.
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Administrar()
        {
            var vuelos = new List<Vuelo>();

            foreach (var vuelo in db.Vuelo)
            {
                vuelos.Add(vuelo);
            }

            vuelos.Sort(delegate (Vuelo a, Vuelo b)
            {
                var comparacionFecha = b.FechaOperacion.CompareTo(a.FechaOperacion);
                if (comparacionFecha != 0)
                    return comparacionFecha;

                return a.VueloId.CompareTo(b.VueloId);
            });

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

        // Consultas auxiliares mediante foreach y estructuras tradicionales.
        // No se utilizan expresiones LINQ ni consultas SQL escritas manualmente.
        private Vuelo ObtenerVueloCompleto(int id)
        {
            foreach (var vuelo in db.Vuelo)
            {
                if (vuelo.VueloId == id)
                    return vuelo;
            }

            return null;
        }

        private Dictionary<int, int> ObtenerConteos(Dictionary<int, HashSet<byte>> ocupados)
        {
            var conteos = new Dictionary<int, int>();

            foreach (var elemento in ocupados)
            {
                conteos[elemento.Key] = elemento.Value.Count;
            }

            return conteos;
        }

        // Método auxiliar conservado para mantener la estructura del proyecto.
        // Filtra los vuelos recorriendo la colección con foreach.
        private List<Vuelo> ObtenerVuelosFiltrados(int? origenId, int? destinoId,
            DateTime? fecha, int pasajeros, Dictionary<int, HashSet<byte>> ocupados)
        {
            var resultados = new List<Vuelo>();

            foreach (var vuelo in db.Vuelo)
            {
                if (vuelo.EstadoVueloCodigo == "CANCELADO" ||
                    vuelo.EstadoVueloCodigo == "COMPLETADO")
                    continue;

                if (vuelo.Frecuencia == null || vuelo.Frecuencia.Ruta == null)
                    continue;

                if (origenId.HasValue &&
                    vuelo.Frecuencia.Ruta.AeropuertoOrigenId != origenId.Value)
                    continue;

                if (destinoId.HasValue &&
                    vuelo.Frecuencia.Ruta.AeropuertoDestinoId != destinoId.Value)
                    continue;

                if (fecha.HasValue)
                {
                    var desde = fecha.Value.Date;
                    var hasta = desde.AddDays(1);

                    if (vuelo.FechaOperacion < desde || vuelo.FechaOperacion >= hasta)
                        continue;
                }

                var capacidad = 0;

                if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                    capacidad = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;

                var cantidadOcupada = 0;

                if (ocupados.ContainsKey(vuelo.VueloId))
                    cantidadOcupada = ocupados[vuelo.VueloId].Count;

                if (capacidad > 0 && capacidad - cantidadOcupada >= pasajeros)
                    resultados.Add(vuelo);
            }

            resultados.Sort(delegate (Vuelo a, Vuelo b)
            {
                var comparacionFecha = a.FechaOperacion.CompareTo(b.FechaOperacion);
                if (comparacionFecha != 0)
                    return comparacionFecha;

                return a.VueloId.CompareTo(b.VueloId);
            });

            return resultados;
        }

        // ==========================================================
        // Helpers EXCLUSIVOS de Buscar(): sin .Where()/.Select()/.OrderBy()/
        // .Include()/.FirstOrDefault() y sin SQL crudo. No los usan
        // Resultados, DisponibilidadAsientos ni Seleccionar, por eso no
        // modifican el comportamiento de esas acciones.
        // ==========================================================

        private List<Aeropuerto> ObtenerAeropuertosActivosSinLinq()
        {
            var aeropuertos = new List<Aeropuerto>();
            foreach (var aeropuerto in db.Aeropuerto)
            {
                if (aeropuerto.Activo)
                    aeropuertos.Add(aeropuerto);
            }

            aeropuertos.Sort(delegate (Aeropuerto a, Aeropuerto b)
            {
                return string.Compare(a.Localidad, b.Localidad, StringComparison.Ordinal);
            });

            return aeropuertos;
        }

        private Dictionary<int, HashSet<byte>> ObtenerAsientosOcupadosSinLinq()
        {
            // Misma regla de negocio que ObtenerAsientosOcupados (PENDIENTE y
            // CONFIRMADA ocupan inventario), pero recorrida con foreach.
            var ocupacion = new Dictionary<int, HashSet<byte>>();
            foreach (var asignacion in db.ReservaPasajeroVuelo)
            {
                if (asignacion.EstadoAsignacionCodigo != "PENDIENTE" &&
                    asignacion.EstadoAsignacionCodigo != "CONFIRMADA")
                    continue;

                if (!ocupacion.ContainsKey(asignacion.VueloId))
                    ocupacion[asignacion.VueloId] = new HashSet<byte>();
                ocupacion[asignacion.VueloId].Add(asignacion.NumeroAsiento);
            }
            return ocupacion;
        }

        private List<Vuelo> ObtenerVuelosFiltradosSinLinq(int? origenId, int? destinoId,
            DateTime? fecha, int pasajeros, Dictionary<int, HashSet<byte>> ocupados)
        {
            var resultados = new List<Vuelo>();

            foreach (var vuelo in db.Vuelo)
            {
                // No mostrar vuelos CANCELADO o COMPLETADO.
                if (vuelo.EstadoVueloCodigo == "CANCELADO" || vuelo.EstadoVueloCodigo == "COMPLETADO")
                    continue;

                if (vuelo.Frecuencia == null || vuelo.Frecuencia.Ruta == null)
                    continue;

                if (origenId.HasValue && vuelo.Frecuencia.Ruta.AeropuertoOrigenId != origenId.Value)
                    continue;

                if (destinoId.HasValue && vuelo.Frecuencia.Ruta.AeropuertoDestinoId != destinoId.Value)
                    continue;

                if (fecha.HasValue)
                {
                    var desde = fecha.Value.Date;
                    var hasta = desde.AddDays(1);
                    if (vuelo.FechaOperacion < desde || vuelo.FechaOperacion >= hasta)
                        continue;
                }

                // Capacidad real desde ModeloAeronave.CapacidadAsientos.
                var capacidad = 0;
                if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                    capacidad = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;

                var cantidadOcupada = 0;
                if (ocupados.ContainsKey(vuelo.VueloId))
                    cantidadOcupada = ocupados[vuelo.VueloId].Count;

                var disponibles = capacidad - cantidadOcupada;
                if (capacidad > 0 && disponibles >= pasajeros)
                    resultados.Add(vuelo);
            }

            // Orden manual (sin OrderBy/ThenBy): por fecha y luego por VueloId.
            resultados.Sort(delegate (Vuelo a, Vuelo b)
            {
                var cmpFecha = a.FechaOperacion.CompareTo(b.FechaOperacion);
                return cmpFecha != 0 ? cmpFecha : a.VueloId.CompareTo(b.VueloId);
            });

            return resultados;
        }

        // ==========================================================
        // Helper EXCLUSIVO de Resultados(): parte de las mismas reglas que
        // ObtenerVuelosFiltradosSinLinq (CANCELADO/COMPLETADO, origen, destino,
        // fecha y capacidad vs. pasajeros), pero agrega las reglas propias de
        // esta pantalla (Frecuencia.Activa, día autorizado por
        // Frecuencia.DiaSemana y orden por horario de salida). No se reutiliza
        // el helper de Buscar para no alterar su comportamiento: Buscar no
        // exige estas reglas adicionales.
        //
        // Frecuencia.DiaSemana usa la misma convención que DateTime.DayOfWeek
        // (0 = Domingo, 1 = Lunes, ... 6 = Sábado), confirmada para este proyecto.
        // ==========================================================
        private List<Vuelo> ObtenerVuelosFiltradosResultadosSinLinq(int? origenId, int? destinoId,
            DateTime? fecha, int pasajeros, Dictionary<int, HashSet<byte>> ocupados)
        {
            var resultados = new List<Vuelo>();

            foreach (var vuelo in db.Vuelo)
            {
                // No mostrar vuelos CANCELADO o COMPLETADO.
                if (vuelo.EstadoVueloCodigo == "CANCELADO" || vuelo.EstadoVueloCodigo == "COMPLETADO")
                    continue;

                if (vuelo.Frecuencia == null || vuelo.Frecuencia.Ruta == null)
                    continue;

                // Una frecuencia inactiva ya no está autorizada a operar.
                if (!vuelo.Frecuencia.Activa)
                    continue;

                if (origenId.HasValue && vuelo.Frecuencia.Ruta.AeropuertoOrigenId != origenId.Value)
                    continue;

                if (destinoId.HasValue && vuelo.Frecuencia.Ruta.AeropuertoDestinoId != destinoId.Value)
                    continue;

                if (fecha.HasValue)
                {
                    var desde = fecha.Value.Date;
                    var hasta = desde.AddDays(1);
                    if (vuelo.FechaOperacion < desde || vuelo.FechaOperacion >= hasta)
                        continue;

                    // El vuelo solo es válido si su fecha de operación coincide con
                    // el día de la semana autorizado por su frecuencia.
                    var diaSemanaSolicitado = (byte)fecha.Value.DayOfWeek;
                    if (vuelo.Frecuencia.DiaSemana != diaSemanaSolicitado)
                        continue;
                }

                // Capacidad real desde ModeloAeronave.CapacidadAsientos.
                var capacidad = 0;
                if (vuelo.Aeronave != null && vuelo.Aeronave.ModeloAeronave != null)
                    capacidad = vuelo.Aeronave.ModeloAeronave.CapacidadAsientos;

                var cantidadOcupada = 0;
                if (ocupados.ContainsKey(vuelo.VueloId))
                    cantidadOcupada = ocupados[vuelo.VueloId].Count;

                var disponibles = capacidad - cantidadOcupada;
                if (capacidad > 0 && disponibles >= pasajeros)
                    resultados.Add(vuelo);
            }

            // Orden manual (sin OrderBy/ThenBy): por fecha de operación y, cuando
            // coinciden, por el horario de salida real de la frecuencia.
            resultados.Sort(delegate (Vuelo a, Vuelo b)
            {
                var cmpFecha = a.FechaOperacion.CompareTo(b.FechaOperacion);
                if (cmpFecha != 0) return cmpFecha;

                var horaA = a.Frecuencia != null ? a.Frecuencia.HoraSalida : TimeSpan.Zero;
                var horaB = b.Frecuencia != null ? b.Frecuencia.HoraSalida : TimeSpan.Zero;
                var cmpHora = horaA.CompareTo(horaB);
                return cmpHora != 0 ? cmpHora : a.VueloId.CompareTo(b.VueloId);
            });

            return resultados;
        }

        // ==========================================================
        // Helpers EXCLUSIVOS de DisponibilidadAsientos() y de su validación de
        // servidor ConfirmarSeleccionAsientos(): sin .Where()/.Select()/
        // .OrderBy()/.Include()/.FirstOrDefault() ni SQL crudo. No se
        // reutiliza ObtenerVueloCompleto() porque también lo usan
        // Details()/Delete(); modificarlo cambiaría el comportamiento de esas
        // acciones, que esta tarea no debe tocar. ObtenerEstadosAsignacionVueloSinLinq()
        // también la reutiliza Seleccionar()/ContinuarConSeleccion() desde la
        // tarea de esa pantalla, porque es una regla de negocio genérica (no
        // depende de DisponibilidadAsientos) y reutilizarla no modifica el
        // cuerpo de esa acción.
        // ==========================================================

        private Vuelo ObtenerVueloParaDisponibilidadSinLinq(int id)
        {
            foreach (var vuelo in db.Vuelo)
            {
                if (vuelo.VueloId == id) return vuelo;
            }
            return null;
        }

        // Estado de asignación vigente (PENDIENTE/CONFIRMADA) por número de
        // asiento, para UN solo vuelo. Misma regla de negocio que
        // ObtenerAsientosOcupadosSinLinq(), pero conservando el código de
        // estado de cada asiento: permite que la vista diferencie "Reservado"
        // (PENDIENTE) de "Ocupado" (CONFIRMADA), y permite volver a comprobar
        // la ocupación vigente al validar el botón Continuar.
        private Dictionary<byte, string> ObtenerEstadosAsignacionVueloSinLinq(int vueloId)
        {
            var estados = new Dictionary<byte, string>();
            foreach (var asignacion in db.ReservaPasajeroVuelo)
            {
                if (asignacion.VueloId != vueloId) continue;
                if (asignacion.EstadoAsignacionCodigo != "PENDIENTE" &&
                    asignacion.EstadoAsignacionCodigo != "CONFIRMADA") continue;

                estados[asignacion.NumeroAsiento] = asignacion.EstadoAsignacionCodigo;
            }
            return estados;
        }

        private void PrepararDisponibilidadAsientosSinLinq(Vuelo vuelo, int? cantidadPasajeros)
        {
            // Límite propio de esta pantalla: nunca se permiten más de 7
            // asientos por operación, aunque la aeronave real tenga más
            // capacidad física registrada en ModeloAeronave.CapacidadAsientos.
            // La capacidad real NO se reduce ni se altera en SQL Server; solo
            // se limita cuántos asientos puede seleccionar el usuario aquí.
            const int limitePantalla = 7;

            var aeronave = vuelo.Aeronave;
            var modelo = aeronave != null ? aeronave.ModeloAeronave : null;
            var capacidadReal = modelo != null ? (int)modelo.CapacidadAsientos : 0;
            var capacidadAplicable = Math.Min(capacidadReal, limitePantalla);

            // Asientos reales registrados para el modelo de esta aeronave.
            var asientos = new List<Asiento>();
            if (modelo != null)
            {
                foreach (var asiento in db.Asiento)
                {
                    if (asiento.ModeloAeronaveId == modelo.ModeloAeronaveId)
                        asientos.Add(asiento);
                }

                asientos.Sort(delegate (Asiento a, Asiento b)
                {
                    var comparacionFila = a.Fila.CompareTo(b.Fila);
                    if (comparacionFila != 0)
                        return comparacionFila;

                    var comparacionLado = string.Compare(
                        (a.Lado ?? "").Trim(),
                        (b.Lado ?? "").Trim(),
                        StringComparison.OrdinalIgnoreCase);

                    if (comparacionLado != 0)
                        return comparacionLado;

                    return a.NumeroAsiento.CompareTo(b.NumeroAsiento);
                });
            }

            var estadosPorNumero = ObtenerEstadosAsignacionVueloSinLinq(vuelo.VueloId);

            var codigosOcupados = new List<string>();
            var estadosPorCodigo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var ocupados = 0;
            foreach (var asiento in asientos)
            {
                string estadoAsignacion;
                if (estadosPorNumero.TryGetValue(asiento.NumeroAsiento, out estadoAsignacion))
                {
                    ocupados++;
                    var codigo = (asiento.Codigo ?? "").Trim();
                    codigosOcupados.Add(codigo);
                    estadosPorCodigo[codigo] = estadoAsignacion;
                }
            }

            var disponibles = Math.Max(0, capacidadReal - ocupados);
            var limiteSeleccion = Math.Max(0, Math.Min(capacidadAplicable, disponibles));

            var cantidadSolicitada = cantidadPasajeros.HasValue && cantidadPasajeros.Value > 0
                ? cantidadPasajeros.Value : 0;

            var estadoOperativoValido = vuelo.EstadoVueloCodigo == "PROGRAMADO" ||
                                         vuelo.EstadoVueloCodigo == "CONFIRMADO";

            string mensajeEstado = null;
            if (!estadoOperativoValido)
            {
                mensajeEstado = "El vuelo se encuentra en estado " + (vuelo.EstadoVueloCodigo ?? "DESCONOCIDO") +
                    " y no admite selección de asientos.";
            }

            ViewBag.VueloId = vuelo.VueloId;
            ViewBag.FechaOperacion = vuelo.FechaOperacion;
            ViewBag.HoraPartida = vuelo.Frecuencia == null ? "" : vuelo.Frecuencia.HoraSalida.ToString(@"hh\:mm");
            ViewBag.HoraLlegada = vuelo.Frecuencia == null ? "" : vuelo.Frecuencia.HoraLlegada.ToString(@"hh\:mm");
            ViewBag.AeropuertoOrigen = vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null &&
                vuelo.Frecuencia.Ruta.Aeropuerto != null
                ? vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA : "";
            ViewBag.AeropuertoDestino = vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null &&
                vuelo.Frecuencia.Ruta.Aeropuerto1 != null
                ? vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA : "";
            ViewBag.Matricula = aeronave == null ? "N/A" : aeronave.Matricula;
            ViewBag.ModeloAeronave = modelo == null ? "N/A" : (modelo.Fabricante + " " + modelo.Nombre).Trim();
            ViewBag.CapacidadTotal = capacidadReal;
            ViewBag.CapacidadAplicable = capacidadAplicable;
            ViewBag.LimitePantalla = limitePantalla;
            ViewBag.CapacidadExcedeLimite = capacidadReal > limitePantalla;
            ViewBag.Ocupados = ocupados;
            ViewBag.Disponibles = disponibles;
            ViewBag.LimiteSeleccion = limiteSeleccion;
            ViewBag.CantidadPasajeros = cantidadSolicitada;
            ViewBag.Asientos = asientos;
            ViewBag.AsientosOcupados = codigosOcupados;
            ViewBag.EstadosAsientos = estadosPorCodigo;
            ViewBag.EstadoOperativoValido = estadoOperativoValido;
            ViewBag.MensajeEstado = mensajeEstado;
            ViewBag.ExcedeLimitePantalla = cantidadSolicitada > limitePantalla;
            ViewBag.ExcedeDisponibilidad = cantidadSolicitada > 0 && cantidadSolicitada > disponibles;

            PrepararDatosVisualesAsientos(asientos, estadosPorNumero, capacidadReal);
        }

        // ==========================================================
        // Helpers EXCLUSIVOS de Seleccionar() y de su validación de servidor
        // ContinuarConSeleccion(): sin .Where()/.Select()/.OrderBy()/
        // .Include()/.FirstOrDefault() ni SQL crudo. No se reutiliza
        // ObtenerVueloCompleto() porque también lo usan Details()/Delete().
        // Aunque la lógica es idéntica a ObtenerVueloParaDisponibilidadSinLinq(),
        // se crea un helper independiente para no tener que tocar ninguna línea
        // dentro de DisponibilidadAsientos() (acción protegida que ya llama
        // directamente a ObtenerVueloParaDisponibilidadSinLinq por su nombre).
        // ==========================================================

        private Vuelo ObtenerVueloParaSeleccionSinLinq(int id)
        {
            foreach (var vuelo in db.Vuelo)
            {
                if (vuelo.VueloId == id) return vuelo;
            }
            return null;
        }

        private void PrepararDatosDeAsientos(Vuelo vuelo, bool incluirHorario, int? cantidadPasajeros,
            string codigosAsientosPreseleccionados)
        {
            // Límite propio de esta pantalla (igual que en DisponibilidadAsientos):
            // máximo 7 asientos por operación. No se altera la capacidad real
            // almacenada en ModeloAeronave.CapacidadAsientos.
            const int limitePantalla = 7;

            var aeronave = vuelo.Aeronave;
            var modelo = aeronave != null ? aeronave.ModeloAeronave : null;
            var capacidadReal = modelo != null ? (int)modelo.CapacidadAsientos : 0;
            var capacidadAplicable = Math.Min(capacidadReal, limitePantalla);

            // Asientos reales registrados para el modelo de esta aeronave (sin
            // LINQ-a-SQL: se recorre el DbSet con foreach y se ordena con List.Sort).
            // El orden es por fila, lado y número para que la vista pueda agrupar
            // la distribución física real sin inventar filas ni columnas.
            var asientos = new List<Asiento>();
            if (modelo != null)
            {
                foreach (var asiento in db.Asiento)
                {
                    if (asiento.ModeloAeronaveId == modelo.ModeloAeronaveId)
                        asientos.Add(asiento);
                }

                asientos.Sort(delegate (Asiento a, Asiento b)
                {
                    var cmpFila = a.Fila.CompareTo(b.Fila);
                    if (cmpFila != 0) return cmpFila;

                    var cmpLado = string.Compare((a.Lado ?? "").Trim(), (b.Lado ?? "").Trim(),
                        StringComparison.OrdinalIgnoreCase);
                    return cmpLado != 0 ? cmpLado : a.NumeroAsiento.CompareTo(b.NumeroAsiento);
                });
            }

            // Ocupación vigente de ESTE vuelo (PENDIENTE/CONFIRMADA), consultada en
            // este instante: se reutiliza el mismo helper que usa
            // DisponibilidadAsientos, para que ambas pantallas diferencien
            // "Reservado" de "Ocupado" con la misma regla de negocio.
            var estadosPorNumero = ObtenerEstadosAsignacionVueloSinLinq(vuelo.VueloId);

            var codigosOcupados = new List<string>();
            var estadosPorCodigo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var codigosValidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ocupados = 0;
            foreach (var asiento in asientos)
            {
                var codigo = (asiento.Codigo ?? "").Trim();
                codigosValidos.Add(codigo);

                string estadoAsignacion;
                if (estadosPorNumero.TryGetValue(asiento.NumeroAsiento, out estadoAsignacion))
                {
                    ocupados++;
                    codigosOcupados.Add(codigo);
                    estadosPorCodigo[codigo] = estadoAsignacion;
                }
            }

            // Los asientos disponibles se calculan sobre los asientos REALES
            // registrados para el modelo (no sobre una distribución supuesta),
            // sin superar nunca la capacidad física del modelo.
            var capacidadSeleccionable = Math.Min(capacidadReal, asientos.Count);
            var disponibles = Math.Max(0, capacidadSeleccionable - ocupados);

            // Cantidad de pasajeros solicitada. Si no llega desde las pantallas
            // anteriores se asume 1 (mismo valor predeterminado que Buscar()).
            var cantidadSolicitada = cantidadPasajeros.HasValue && cantidadPasajeros.Value > 0
                ? cantidadPasajeros.Value : 1;

            // El máximo seleccionable es el menor entre: capacidad aplicable al
            // límite de 7 de esta pantalla, asientos realmente disponibles y
            // pasajeros solicitados. Así nunca se puede marcar más de lo pedido.
            var limiteSeleccion = Math.Max(0, Math.Min(capacidadAplicable, disponibles));
            if (cantidadSolicitada > 0)
                limiteSeleccion = Math.Min(limiteSeleccion, cantidadSolicitada);

            // El vuelo debe tener aeronave/modelo y un estado operativo válido.
            // No se inventan estados: solo PROGRAMADO y CONFIRMADO admiten
            // selección, y CANCELADO/COMPLETADO quedan bloqueados.
            var tieneAeronave = aeronave != null && modelo != null;
            var estadoVueloValido = vuelo.EstadoVueloCodigo == "PROGRAMADO" ||
                                     vuelo.EstadoVueloCodigo == "CONFIRMADO";
            var estadoOperativoValido = tieneAeronave && estadoVueloValido;

            string mensajeEstado = null;
            if (!tieneAeronave)
            {
                mensajeEstado = "El vuelo no tiene una aeronave con modelo asignado; " +
                    "no es posible seleccionar asientos.";
            }
            else if (!estadoVueloValido)
            {
                mensajeEstado = "El vuelo se encuentra en estado " + (vuelo.EstadoVueloCodigo ?? "DESCONOCIDO") +
                    " y no admite selección de asientos.";
            }

            // Asientos recibidos desde DisponibilidadAsientos: se conservan
            // únicamente los que existen en esta aeronave, que en este instante
            // siguen sin estar ocupados (nueva comprobación: no se confía en lo
            // que el usuario vio en la pantalla anterior), sin duplicados y sin
            // superar la cantidad de pasajeros/límite de la pantalla.
            var asientosSeleccionados = new List<string>();
            if (!string.IsNullOrWhiteSpace(codigosAsientosPreseleccionados))
            {
                var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var crudo in codigosAsientosPreseleccionados.Split(','))
                {
                    var codigo = crudo.Trim();
                    if (codigo.Length == 0) continue;
                    if (!codigosValidos.Contains(codigo)) continue;
                    if (estadosPorCodigo.ContainsKey(codigo)) continue;
                    if (!vistos.Add(codigo)) continue;
                    if (asientosSeleccionados.Count >= limiteSeleccion) break;
                    asientosSeleccionados.Add(codigo);
                }
            }

            string origenIata = "";
            string destinoIata = "";
            string origenNombre = "";
            string destinoNombre = "";
            int? origenId = null;
            int? destinoId = null;
            if (vuelo.Frecuencia != null && vuelo.Frecuencia.Ruta != null)
            {
                origenId = vuelo.Frecuencia.Ruta.AeropuertoOrigenId;
                destinoId = vuelo.Frecuencia.Ruta.AeropuertoDestinoId;

                if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
                {
                    origenIata = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA;
                    origenNombre = vuelo.Frecuencia.Ruta.Aeropuerto.Nombre;
                }

                if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
                {
                    destinoIata = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA;
                    destinoNombre = vuelo.Frecuencia.Ruta.Aeropuerto1.Nombre;
                }
            }

            ViewBag.VueloId = vuelo.VueloId;
            ViewBag.Capacidad = capacidadReal;
            ViewBag.CapacidadAplicable = capacidadAplicable;
            ViewBag.LimitePantalla = limitePantalla;
            ViewBag.CapacidadExcedeLimite = capacidadReal > limitePantalla;
            ViewBag.Ocupados = ocupados;
            ViewBag.Restantes = disponibles;
            ViewBag.Disponibles = disponibles;
            ViewBag.LimiteSeleccion = limiteSeleccion;
            ViewBag.CantidadPasajeros = cantidadSolicitada;
            ViewBag.AeropuertoOrigen = origenIata;
            ViewBag.AeropuertoDestino = destinoIata;
            ViewBag.AeropuertoOrigenNombre = origenNombre;
            ViewBag.AeropuertoDestinoNombre = destinoNombre;
            ViewBag.AeropuertoOrigenId = origenId;
            ViewBag.AeropuertoDestinoId = destinoId;
            ViewBag.FechaOperacion = vuelo.FechaOperacion;
            ViewBag.Matricula = aeronave == null ? "N/A" : (aeronave.Matricula ?? "N/A");
            ViewBag.ModeloAeronave = modelo == null
                ? "N/A"
                : (modelo.Fabricante + " " + modelo.Nombre).Trim();
            ViewBag.EstadoVueloDescripcion = vuelo.EstadoVuelo != null ? vuelo.EstadoVuelo.Descripcion : null;
            ViewBag.Asientos = asientos;
            ViewBag.AsientosOcupados = codigosOcupados;
            ViewBag.EstadosAsientos = estadosPorCodigo;
            ViewBag.AsientosSeleccionados = asientosSeleccionados;
            ViewBag.EstadoOperativoValido = estadoOperativoValido;
            ViewBag.MensajeEstado = mensajeEstado;
            ViewBag.ExcedeLimitePantalla = cantidadSolicitada > limitePantalla;
            ViewBag.ExcedeDisponibilidad = cantidadSolicitada > disponibles;

            if (incluirHorario)
            {
                ViewBag.HoraPartida = vuelo.Frecuencia == null ? "" : vuelo.Frecuencia.HoraSalida.ToString(@"hh\:mm");
                ViewBag.HoraLlegada = vuelo.Frecuencia == null ? "" : vuelo.Frecuencia.HoraLlegada.ToString(@"hh\:mm");
            }

            PrepararDatosVisualesAsientos(asientos, estadosPorNumero, capacidadReal);
        }

        // Prepara en el controlador los datos que la vista necesita mostrar.
        // La vista recibe las filas, los asientos agrupados y sus estados.
        private void PrepararDatosVisualesAsientos(
            List<Asiento> asientos,
            Dictionary<byte, string> estadosPorNumero,
            int capacidadReal)
        {
            var filas = new List<int>();
            var asientosPorFila = new Dictionary<int, List<Asiento>>();
            var estadosPorCodigo = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            int reservados = 0;
            int ocupados = 0;

            foreach (var asiento in asientos)
            {
                if (!asientosPorFila.ContainsKey(asiento.Fila))
                {
                    asientosPorFila[asiento.Fila] = new List<Asiento>();
                    filas.Add(asiento.Fila);
                }

                asientosPorFila[asiento.Fila].Add(asiento);

                var codigo = (asiento.Codigo ?? "").Trim();
                string estadoAsignacion;
                string estadoVisual = "DISPONIBLE";

                if (estadosPorNumero.TryGetValue(asiento.NumeroAsiento, out estadoAsignacion))
                {
                    if (estadoAsignacion == "PENDIENTE")
                    {
                        estadoVisual = "RESERVADO";
                        reservados++;
                    }
                    else if (estadoAsignacion == "CONFIRMADA")
                    {
                        estadoVisual = "OCUPADO";
                        ocupados++;
                    }
                }

                estadosPorCodigo[codigo] = estadoVisual;
            }

            int capacidad = capacidadReal > 0 ? capacidadReal : asientos.Count;
            int disponibles = Math.Max(0, capacidad - reservados - ocupados);

            ViewBag.FilasAsientos = filas;
            ViewBag.AsientosPorFila = asientosPorFila;
            ViewBag.EstadosVisualesAsientos = estadosPorCodigo;
            ViewBag.TotalReservados = reservados;
            ViewBag.TotalOcupados = ocupados;
            ViewBag.TotalDisponibles = disponibles;
        }

        private void CargarListas(Vuelo vuelo = null)
        {
            var aeronaves = new List<Aeronave>();
            foreach (var aeronave in db.Aeronave)
            {
                aeronaves.Add(aeronave);
            }

            aeronaves.Sort(delegate (Aeronave a, Aeronave b)
            {
                return string.Compare(a.Matricula, b.Matricula, StringComparison.OrdinalIgnoreCase);
            });

            var estados = new List<EstadoVuelo>();
            foreach (var estado in db.EstadoVuelo)
            {
                estados.Add(estado);
            }

            var frecuencias = new List<Frecuencia>();
            foreach (var frecuencia in db.Frecuencia)
            {
                frecuencias.Add(frecuencia);
            }

            frecuencias.Sort(delegate (Frecuencia a, Frecuencia b)
            {
                return a.FrecuenciaId.CompareTo(b.FrecuenciaId);
            });

            ViewBag.AeronaveId = new SelectList(
                aeronaves,
                "AeronaveId",
                "Matricula",
                vuelo == null ? null : (object)vuelo.AeronaveId);

            ViewBag.EstadoVueloCodigo = new SelectList(
                estados,
                "Codigo",
                "Descripcion",
                vuelo == null ? null : (object)vuelo.EstadoVueloCodigo);

            ViewBag.FrecuenciaId = new SelectList(
                frecuencias,
                "FrecuenciaId",
                "FrecuenciaId",
                vuelo == null ? null : (object)vuelo.FrecuenciaId);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
