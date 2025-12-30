using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Model
{
    [Table("TApplicabilityRules")]
    public class TApplicabilityRules
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int OS { get; set; }

        [Required]
        public int SKU { get; set; }

        [Required]
        public int Arch { get; set; }

        [Required]
        public int IsInstallableRuleID { get; set; }

        [Required]
        public int IsInstalledRuleID { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Description { get; set; }
    }
}
