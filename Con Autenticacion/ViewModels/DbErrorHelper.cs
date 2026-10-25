using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace Con_Autenticacion.ViewModels
{
    public static class DbErrorHelper
    {
        public static string ObtenerMensaje(Exception ex)
        {
            var sql = BuscarSqlException(ex);

            if (sql == null)
            {
                return "No se pudo guardar la información. Intente nuevamente.";
            }

            switch (sql.Number)
            {
                case 50001:
                    return "La fecha del vuelo no corresponde al día de operación de la frecuencia.";
                case 50002:
                    return "No se puede programar un vuelo sobre una frecuencia inactiva.";
                case 50003:
                    return "La aeronave seleccionada no está disponible.";
                case 50004:
                    return "La aeronave ya tiene otro vuelo en un horario incompatible.";
                case 50011:
                    return "La reserva está cancelada o expirada; no puede ocupar asientos.";
                case 50012:
                    return "El vuelo no tiene una aeronave asignada; no se pueden reservar asientos.";
                case 50013:
                    return "El vuelo no está en un estado que permita reservas.";
                case 50014:
                    return "El número de asiento supera la capacidad de la aeronave.";
                case 50021:
                    return "Los tramos no son continuos: el destino de un tramo debe ser el origen del siguiente y la salida debe ser posterior a la llegada.";
                case 50031:
                    return "El check-in solo puede completarse para un asiento confirmado.";
                case 50041:
                    return "El importe aprobado supera el importe de la compra.";
                case 50042:
                    return "No se puede aprobar el pago de una compra anulada.";
                case 50051:
                    return "El rango de fechas no es válido.";
                case 50052:
                    return "El rango no puede superar los 366 días.";
                case 2601:
                case 2627:
                    return "Ya existe un registro con esos datos. Verifique los valores duplicados.";
                case 547:
                    return "No se puede completar la operación porque hay registros relacionados.";
                default:
                    return string.IsNullOrWhiteSpace(sql.Message)
                        ? "No se pudo guardar la información. Intente nuevamente."
                        : sql.Message;
            }
        }

        private static SqlException BuscarSqlException(Exception ex)
        {
            while (ex != null)
            {
                var sql = ex as SqlException;
                if (sql != null)
                {
                    return sql;
                }

                var dbUpdate = ex as DbUpdateException;
                if (dbUpdate != null && dbUpdate.InnerException != null)
                {
                    ex = dbUpdate.InnerException;
                    continue;
                }

                ex = ex.InnerException;
            }

            return null;
        }
    }
}