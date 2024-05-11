using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.Helpers
{
    public  class Utilities
    {
        public static bool HasClosestToDistanceAsVec3(List<Vec3> vecS1, List<Vec3> vecS2, float targetDistance)
        {
            bool found = false; 
             
            for (int i = 0, j = vecS1.Count() -1; i < j; i++, j--)
            {

                for (int x = 0, y = vecS2.Count() - 1; x < y; x++, y--)
                {
                    if(
                        vecS1[i].Distance(vecS2[x]) < targetDistance || 
                        vecS1[j].Distance(vecS2[x]) < targetDistance ||

                        vecS1[i].Distance(vecS2[y]) < targetDistance ||
                        vecS1[j].Distance(vecS2[y]) < targetDistance 
                        )
                    {
                        found = true;
                        break;
                    }

                } 
                if(found)
                {  
                    break; 
                }

            }

            return found;
             

        }

        public static bool HasClosestToDistanceAsVec2(List<Vec3> vecS1, List<Vec3> vecS2, float targetDistance)
        {
            bool found = false;

            for (int i = 0, j = vecS1.Count() - 1; i < j; i++, j--)
            {

                for (int x = 0, y = vecS2.Count() - 1; x < y; x++, y--)
                {
                    if (
                        vecS1[i].AsVec2.Distance(vecS2[x].AsVec2) < targetDistance ||
                        vecS1[j].AsVec2.Distance(vecS2[x].AsVec2) < targetDistance ||

                        vecS1[i].AsVec2.Distance(vecS2[y].AsVec2) < targetDistance ||
                        vecS1[j].AsVec2.Distance(vecS2[y].AsVec2) < targetDistance
                        )
                    {
                        found = true;
                        break;
                    }

                }
                if (found)
                {
                    break;
                }

            }

            return found;


        }
    }
}
