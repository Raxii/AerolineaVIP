using System;
using System.Collections.Generic;

namespace Con_Autenticacion.ViewModels
{
    public class MenuAdminViewModel
    {
        public int Aeropuertos { get; set; }
        public int Rutas { get; set; }
        public int Frecuencias { get; set; }
        public int Aeronaves { get; set; }
        public int Modelos { get; set; }
        public int Vuelos { get; set; }
        public int VuelosProgramados { get; set; }
        public int Itinerarios { get; set; }
        public int Tramos { get; set; }
    }

    public class ReporteViewModel
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int TotalVuelos { get; set; }
        public int VuelosProgramados { get; set; }
        public int VuelosConfirmados { get; set; }
        public int VuelosCompletados { get; set; }
        public int VuelosCancelados { get; set; }
        public int TotalReservas { get; set; }
        public int ReservasPendientes { get; set; }
        public int ReservasConfirmadas { get; set; }
        public int ReservasExpiradas { get; set; }
        public int ReservasCanceladas { get; set; }
        public decimal VentasAprobadas { get; set; }
        public decimal VentasReversadas { get; set; }
        public int TotalPasajeros { get; set; }

        public List<EstadoReporteViewModel> VuelosPorEstado { get; set; }
        public List<EstadoReporteViewModel> ReservasPorEstado { get; set; }
        public List<OcupacionReporteViewModel> Ocupacion { get; set; }
        public List<VentaDiaReporteViewModel> VentasPorDia { get; set; }
        public List<RutaReporteViewModel> RutasTop { get; set; }
    }

    public class EstadoReporteViewModel
    {
        public string Codigo { get; set; }
        public int Total { get; set; }
    }

    public class OcupacionReporteViewModel
    {
        public int VueloId { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string HoraSalida { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string Matricula { get; set; }
        public int Capacidad { get; set; }
        public int Ocupados { get; set; }
        public int Disponibles { get; set; }
        public string EstadoVueloCodigo { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class VentaDiaReporteViewModel
    {
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
    }

    public class RutaReporteViewModel
    {
        public string Origen { get; set; }
        public string Destino { get; set; }
        public int TotalVuelos { get; set; }
    }
}
