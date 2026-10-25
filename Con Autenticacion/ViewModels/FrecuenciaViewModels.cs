using System;

namespace Con_Autenticacion.ViewModels
{
    public class FrecuenciaListaViewModel
    {
        public int FrecuenciaId { get; set; }
        public int RutaId { get; set; }
        public string RutaDescripcion { get; set; }
        public byte DiaSemana { get; set; }
        public string HoraSalida { get; set; }
        public string HoraLlegada { get; set; }
        public bool Activa { get; set; }
        public string DiaSemanaTexto { get; set; }
    }
}