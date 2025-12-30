using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Model
{
    [Table("TProductRefreshPrerequisitesMapping")]
    public class TProductRefreshPrerequisitesMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int OS { get; set; }

        [Required]
        public int CPU { get; set; }

        [Required]
        public int SKU { get; set; }

        [Required]
        public string Prerequisites { get; set; }
    }
}
