using System;
using System.Collections.Generic;

namespace Con_Autenticacion.Models
{
    // Clase auxiliar (NO es una entidad EF, no proviene del EDMX y no agrega
    // tablas ni columnas). Se necesita porque ni Vuelo, ni Ruta, ni Frecuencia
    // tienen un campo de precio/tarifa en la base de datos: la tarifa es una
    // regla de negocio fija que se calcula en memoria, no se consulta a SQL.
    public static class TarifaCalculos
    {
        public const string Moneda = "USD";

        // Tarifa por pasajero y tramo (USD). Claves en formato "ORIGEN-DESTINO"
        // usando los mismos códigos IATA que Aeropuerto.CodigoIATA.
        private static readonly Dictionary<string, decimal> TarifasPorTramo =
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "UIO-GYE", 450m },
            { "GYE-UIO", 450m },
            { "UIO-MEC", 450m },
            { "MEC-UIO", 450m },
            { "GYE-GPS", 650m },
            { "GPS-GYE", 650m },
        };

        // Cantidad mínima de pasajeros en una misma reserva para aplicar
        // el descuento académico (una sola vez sobre el total).
        private const int PasajerosMinimosParaDescuento = 3;
        private const decimal PorcentajeDescuentoAcademico = 0.05m;

        // Devuelve la tarifa por pasajero para el tramo origen-destino, o
        // null si el tramo no está contemplado en la tabla de tarifas.
        public static decimal? ObtenerTarifaPorTramo(string origenIata, string destinoIata)
        {
            if (string.IsNullOrWhiteSpace(origenIata) || string.IsNullOrWhiteSpace(destinoIata))
                return null;

            var clave = origenIata.Trim() + "-" + destinoIata.Trim();
            decimal tarifa;
            if (TarifasPorTramo.TryGetValue(clave, out tarifa))
                return tarifa;

            return null;
        }

        // Calcula el total de la reserva (tarifa por pasajero * cantidad de
        // pasajeros), aplicando el 5% de descuento académico una sola vez
        // cuando hay 3 o más pasajeros. Devuelve null si no hay tarifa
        // definida para el tramo.
        public static decimal? CalcularTotalReserva(decimal? tarifaPorPasajero, int cantidadPasajeros,
            out bool descuentoAplicado)
        {
            descuentoAplicado = false;

            if (!tarifaPorPasajero.HasValue || cantidadPasajeros < 1)
                return null;

            var subtotal = tarifaPorPasajero.Value * cantidadPasajeros;

            if (cantidadPasajeros >= PasajerosMinimosParaDescuento)
            {
                descuentoAplicado = true;
                subtotal -= subtotal * PorcentajeDescuentoAcademico;
            }

            return subtotal;
        }
    }
}
