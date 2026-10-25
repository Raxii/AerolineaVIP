using System;
using System.Collections.Generic;

namespace Con_Autenticacion.ViewModels
{
    public class AsientoMapaViewModel
    {
        public int VueloId { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string HoraSalida { get; set; }
        public string HoraLlegada { get; set; }
        public string EstadoVueloCodigo { get; set; }
        public string Matricula { get; set; }
        public int Capacidad { get; set; }
        public int Ocupados { get; set; }
        public int Disponibles { get; set; }
        public List<AsientoItemViewModel> Asientos { get; set; }
    }

    public class AsientoItemViewModel
    {
        public byte Numero { get; set; }
        public string Codigo { get; set; }
        public byte Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
        public bool Ocupado { get; set; }
    }
}