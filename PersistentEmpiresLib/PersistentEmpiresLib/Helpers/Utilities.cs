﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

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


        public static bool RayCastForClosestEntityOrTerrain(List<Vec3> vecS1, GameEntity currentGameEntity, float targetDistance)
        { 
            bool found = false;
            for (int i = 0, j = vecS1.Count() - 1; i < j; i++, j--)
            { 
                if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(vecS1[i], vecS1[i + 1], out float hitDistanceI, out GameEntity hitEntityI) )
                {
                    if( hitEntityI != currentGameEntity)
                    {

                        found = true;
                        break;

                    } 
                }
                if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(vecS1[j], vecS1[j - 1], out float hitDistanceJ, out GameEntity hitEntityJ))
                {
                    if (hitEntityJ != currentGameEntity)
                    {
                        found = true;
                        break; 
                    }
                }  
            }

            return found; 
        }

        public static bool HasClosestToDistanceAsVec2(List<Vec3> vecS1, List<Vec3> vecS2, float targetDistance)
        {
            for (int i = 0, j = vecS1.Count - 1; i < j; i++, j--)
            {
                for (int x = 0, y = vecS2.Count - 1; x < y; x++, y--)
                {
                    if (IsWithinTargetDistance(vecS1[i], vecS2[x], targetDistance) ||
                        IsWithinTargetDistance(vecS1[j], vecS2[x], targetDistance) ||
                        IsWithinTargetDistance(vecS1[i], vecS2[y], targetDistance) ||
                        IsWithinTargetDistance(vecS1[j], vecS2[y], targetDistance))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsWithinTargetDistance(Vec3 vec1, Vec3 vec2, float targetDistance)
        {
            return vec1.AsVec2.Distance(vec2.AsVec2) < targetDistance;
        }


        public static List<Vec3> GetCollisionCheckPoints(GameEntity gameEntity, string collisionCheckPointTag)
        {
            List<Vec3> list = new List<Vec3>();
            foreach (var cp in gameEntity.GetChildren().Where(c => c.HasTag(collisionCheckPointTag)))
            {
                list.Add(cp.GetGlobalFrame().origin);
            }
            return list;
        }

        public static float GetDistanceByAngle(Vec3 centerPoint, float hs, float vs, double t_rad)
        {
            float x = centerPoint.X + (hs * (float)Math.Cos(t_rad));
            float y = centerPoint.Y + (vs * (float)Math.Sin(t_rad));
            float z = centerPoint.z;
            Vec3 newVecByAngle = new Vec3(x, y, z);
            return centerPoint.Distance(newVecByAngle);
        }

        public static Vec3 GetCollisonPointByAngle(Vec3 centerPoint, float hs, float vs, double t_rad)
        {
            float x = centerPoint.X + (hs * (float)Math.Cos(t_rad));
            float y = centerPoint.Y + (vs * (float)Math.Sin(t_rad));
            float z = centerPoint.z;
            return new Vec3(x, y, z);
        }

        public static double GetRadian(GameEntity e1, GameEntity e2)
        {
            Vec3 currentEnityVecRotationS = e1.GetGlobalFrame().rotation.s;
            Vec3 enityVecRotationS = e2.GetGlobalFrame().rotation.s;
            var dot = Vec3.DotProduct(currentEnityVecRotationS, enityVecRotationS);
            return Math.Acos(Vec3.DotProduct(currentEnityVecRotationS, enityVecRotationS) / (currentEnityVecRotationS.Length) * enityVecRotationS.Length);
        }

    }
}
