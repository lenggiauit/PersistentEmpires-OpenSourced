using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Data
{
    public class PersonalProperty
    { 
        public int PropertyIndex { get; set; }
        public string PropertyName { get; set; }
        public Banner PropertyBanner { get; set; } 
        public string OwnerId { get; set; }

        public PersonalProperty(int propertyIndex, string propertyName, Banner propertyBanner, string ownerId)
        { 
            this.PropertyIndex = propertyIndex;
            this.PropertyName = propertyName;
            this.OwnerId = ownerId;
            this.PropertyBanner = propertyBanner;
             
        }
    }
}
