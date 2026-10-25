
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

using Con_Autenticacion.Models;

namespace Con_Autenticacion
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(
                GlobalFilters.Filters);

            RouteConfig.RegisterRoutes(
                RouteTable.Routes);

            BundleConfig.RegisterBundles(
                BundleTable.Bundles);

            // =====================================
            // CREACION DE ROLES DE AEROLINEA VIP
            // =====================================

            using (var db = new ApplicationDbContext())
            {
                CrearRoles(db);
            }
        }

        // =====================================
        // 1. CREAR ROLES
        // =====================================

        private static void CrearRoles(
            ApplicationDbContext db)
        {
            var store = new RoleStore<IdentityRole>(db)
            {
                DisposeContext = false
            };

            using (var rolemanager =
                new RoleManager<IdentityRole>(store))
            {
                string[] roles =
                {
                    "SuperUsuario",
                    "Administrador",
                    "Cliente",
                    "Counter"
                };

                foreach (string nombreRol in roles)
                {
                    if (!rolemanager.RoleExists(nombreRol))
                    {
                        var resultado = rolemanager.Create(
                            new IdentityRole(nombreRol));

                        VerificarResultado(
                            resultado,
                            "Crear rol " + nombreRol);
                    }
                }
            }
        }

        // =====================================
        // 2. VERIFICAR RESULTADO
        // =====================================

        private static void VerificarResultado(
            IdentityResult resultado,
            string operacion)
        {
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    "Error en " + operacion + ": " +
                    string.Join(", ", resultado.Errors));
            }
        }
    }
}
