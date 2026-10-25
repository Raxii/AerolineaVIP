
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

            // CONFIGURACION DE IDENTITY
            using (var db = new ApplicationDbContext())
            {
                CrearRoles(db);

#if DEBUG
                // CUENTAS INICIALES DE PRUEBA LOCAL
                CrearAdministrador(db);
                CrearCounter(db);
#endif
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

#if DEBUG
        // =====================================
        // 2. CREAR ADMINISTRADOR
        // =====================================
        private static void CrearAdministrador(
            ApplicationDbContext db)
        {
            CrearUsuarioInicial(
                db,
                "Administrador",
                "administrador@aerolineavip.test",
                "admin123",
                "Administrador");
        }

        // =====================================
        // 3. CREAR COUNTER
        // =====================================
        private static void CrearCounter(
            ApplicationDbContext db)
        {
            CrearUsuarioInicial(
                db,
                "Counter",
                "counter@aerolineavip.test",
                "counter123",
                "Counter");
        }

        // =====================================
        // 4. CREAR USUARIO INICIAL
        // =====================================
        private static void CrearUsuarioInicial(
            ApplicationDbContext db,
            string nombre,
            string correo,
            string clave,
            string rol)
        {
            var store = new UserStore<ApplicationUser>(db)
            {
                DisposeContext = false
            };

            using (var usermanager =
                new UserManager<ApplicationUser>(store))
            {
                // COMPROBAR SI YA EXISTE
                var existente = usermanager.FindByName(nombre);

                if (existente != null)
                {
                    // No modificar usuarios existentes
                    // ni concederles roles automaticamente
                    if (!string.Equals(
                            existente.Email,
                            correo,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        !usermanager.IsInRole(
                            existente.Id, rol))
                    {
                        throw new InvalidOperationException(
                            "La cuenta " + nombre +
                            " ya existe, pero requiere " +
                            "revision de identidad y rol.");
                    }

                    return;
                }

                // VERIFICAR CORREO
                if (usermanager.FindByEmail(correo) != null)
                {
                    throw new InvalidOperationException(
                        "El correo de " + nombre +
                        " ya esta registrado.");
                }

                // COMPROBAR CONTRASEÑA
                if (string.IsNullOrWhiteSpace(clave))
                {
                    throw new InvalidOperationException(
                        "La contraseña esta vacia para " +
                        nombre);
                }

                // =====================================
                // VALIDACION TEMPORAL DE PRUEBA LOCAL
                // =====================================
                usermanager.PasswordValidator =
                    new PasswordValidator
                    {
                        RequiredLength = 8,
                        RequireDigit = true,
                        RequireLowercase = true,
                        RequireUppercase = false,
                        RequireNonLetterOrDigit = false
                    };

                // =====================================
                // CREAR USUARIO EN IDENTITY
                // =====================================
                var usuario = new ApplicationUser
                {
                    UserName = nombre,
                    Email = correo
                };

                var resultado = usermanager.Create(
                    usuario, clave);

                VerificarResultado(
                    resultado,
                    "Crear usuario " + nombre);

                // =====================================
                // ASIGNAR ROL
                // =====================================
                var resultadoRol = usermanager.AddToRole(
                    usuario.Id, rol);

                if (!resultadoRol.Succeeded)
                {
                    // ELIMINAR USUARIO INCOMPLETO
                    var eliminacion = usermanager.Delete(
                        usuario);

                    VerificarResultado(
                        eliminacion,
                        "Revertir usuario " + nombre);

                    VerificarResultado(
                        resultadoRol,
                        "Asignar rol " + rol);
                }
            }
        }
#endif

        // =====================================
        // 5. VERIFICAR RESULTADOS DE IDENTITY
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
