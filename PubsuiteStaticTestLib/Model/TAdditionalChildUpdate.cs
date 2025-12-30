using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Model
{
    [Table("TAdditionalChildUpdate")]
    public class TAdditionalChildUpdate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int ReleaseType { get; set; }

        [Required]
        public int OS { get; set; }

        [Required]
        public int Arch { get; set; }

        [Required]
        public int ChildUpdateID { get; set; }
    }
}
