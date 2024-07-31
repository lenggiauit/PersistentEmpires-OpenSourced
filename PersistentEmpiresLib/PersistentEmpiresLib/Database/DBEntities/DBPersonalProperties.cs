using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DBEntities
{
    public class DBPersonalProperties
    {
        public int Id { get; set; }
        public int PropertyIndex { get; set; }
        public string PropertyName { get; set; }
        public string PropertyBanner { get; set; }
        public string OwnerId { get; set; } 
    } 
}
