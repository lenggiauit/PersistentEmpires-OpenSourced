using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
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

        private static float GetDistanceByAngle(Vec3 centerPoint, float hs, float vs, double t_rad)
        {
            float x = centerPoint.X + (hs * (float)Math.Cos(t_rad));
            float y = centerPoint.Y + (vs * (float)Math.Sin(t_rad));
            float z = centerPoint.z;
            Vec3 newVecByAngle = new Vec3(x, y, z);
            return centerPoint.Distance(newVecByAngle);
        }

        private static Vec3 GetCollisonPointByAngle(Vec3 centerPoint, float hs, float vs, double t_rad)
        {
            float x = centerPoint.X + (hs * (float)Math.Cos(t_rad));
            float y = centerPoint.Y + (vs * (float)Math.Sin(t_rad));
            float z = centerPoint.z;
            return new Vec3(x, y, z);
        }

        private static double GetRadian(GameEntity e1, GameEntity e2)
        {
            Vec3 currentEnityVecRotationS = e1.GetGlobalFrame().rotation.s;
            Vec3 enityVecRotationS = e2.GetGlobalFrame().rotation.s;
            var dot = Vec3.DotProduct(currentEnityVecRotationS, enityVecRotationS);
            return Math.Acos(Vec3.DotProduct(currentEnityVecRotationS, enityVecRotationS) / (currentEnityVecRotationS.Length) * enityVecRotationS.Length);
        }

    }
}
