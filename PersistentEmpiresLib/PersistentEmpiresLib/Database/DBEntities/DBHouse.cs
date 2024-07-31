using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DBEntities
{
    public class DBHouse
    {
        public int Id { get; set; }
        public string PlayerId { get; set; }
        public string Name { get; set; }
        public string MissionObjectHash { get; set; }
        public bool IsUpgrading { get; set; }
        public int CurrentTier { get; set; }

    }
}
