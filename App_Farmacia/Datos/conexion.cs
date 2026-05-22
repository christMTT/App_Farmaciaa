    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Text;

    namespace App_Farmacia.Datos
    {
        public static class conexion
        {
        public static string CadenaLogin =>
            ConfigurationManager
                .ConnectionStrings["ConexionVendedor"]
                .ConnectionString;
        public static string Cadena
            {

                get
                {
                    return Sesion.Rol switch
                    {

                        "Vendedor" => ConfigurationManager
                                        .ConnectionStrings["ConexionVendedor"]
                                        .ConnectionString,

                        "Gerente" => ConfigurationManager
                                        .ConnectionStrings["ConexionGerente"]
                                        .ConnectionString,

                        "DBA" => ConfigurationManager
                                        .ConnectionStrings["ConexionDBA"]
                                        .ConnectionString,

                        _ => ConfigurationManager
                                        .ConnectionStrings["ConexionVendedor"]
                                        .ConnectionString
                    };
                }
            }
        }
    }