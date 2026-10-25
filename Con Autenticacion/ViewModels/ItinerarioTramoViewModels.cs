using System;

namespace Con_Autenticacion.ViewModels
{
    public class TramoListaViewModel
    {
        public int TramoItinerarioId { get; set; }
        public int ItinerarioId { get; set; }
        public string ItinerarioReferencia { get; set; }
        public int VueloId { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string HoraSalida { get; set; }
        public string HoraLlegada { get; set; }
        public byte Orden { get; set; }
    }

    public class VueloOpcionViewModel
    {
        public int VueloId { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string HoraSalida { get; set; }
        public string HoraLlegada { get; set; }
        public string Descripcion { get; set; }
    }
}