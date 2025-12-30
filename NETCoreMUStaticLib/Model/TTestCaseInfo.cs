using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Model
{
    [Table("TTestcaseInfo")]
    public class TTestCaseInfo
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Description { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Type { get; set; }
        
        [Required]
        public bool Active { get; set; }
    }
}
