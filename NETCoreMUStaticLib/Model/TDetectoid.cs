using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Model
{
    [Table("TDetectoid")]
    public class TDetectoid
    {
        [Key]
        [MaxLength(50)]
        public string GUID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
    }
}
