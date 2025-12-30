using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Model
{
    [Table("TLocalizedProperty")]
    public class TLocalizedProperty
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(10)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string SecDescription { get; set; }

        [MaxLength(1000)]
        public string NonSecDescription { get; set; }

        [MaxLength(500)]
        public string UninstallNotes { get; set; }
    }
}
