using System;
using System.Collections.Generic;

namespace Con_Autenticacion.ViewModels
{
    public class MisViajesVM
    {
        public List<ReservaClienteVM> Reservas { get; set; } = new List<ReservaClienteVM>();
        public List<ComprobanteClienteVM> Comprobantes { get; set; } = new List<ComprobanteClienteVM>();
        public List<CheckInClienteVM> CheckIns { get; set; } = new List<CheckInClienteVM>();
    }

    public class ReservaClienteVM
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Estado { get; set; }
        public int PasajerosTramos { get; set; }
        public decimal TotalTramos { get; set; }
        public List<string> Descripciones { get; set; } = new List<string>();
    }

    public class ComprobanteClienteVM
    {
        public int ComprobanteId { get; set; }
        public string NumeroComprobante { get; set; }
        public string Tipo { get; set; }
        public string Estado { get; set; }
        public DateTime FechaEmision { get; set; }
        public string CodigoReserva { get; set; }
        public decimal ImporteCompra { get; set; }
        public string Moneda { get; set; }
    }

    public class CheckInClienteVM
    {
        public int AsignacionId { get; set; }
        public string CodigoReserva { get; set; }
        public string Pasajero { get; set; }
        public string Trayecto { get; set; }
        public DateTime FechaVuelo { get; set; }
        public string HoraSalida { get; set; }
        public int Asiento { get; set; }
        public string EstadoAsignacion { get; set; }
        public bool Realizado { get; set; }
        public bool PuedeRealizar { get; set; }
        public string Motivo { get; set; }
    }
}
