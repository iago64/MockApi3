using System.ComponentModel.DataAnnotations;
using System;

namespace MockApi3.Models
{
    public class DNI
    {
        [Key]
        public int Numero { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Sexo { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public int Tramite { get; set; }
    }
}
