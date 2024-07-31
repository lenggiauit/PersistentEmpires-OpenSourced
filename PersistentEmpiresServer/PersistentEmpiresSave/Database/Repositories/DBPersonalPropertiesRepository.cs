using Dapper;
using Database.DBEntities;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors.SaveSystemBehavior;

namespace PersistentEmpiresSave.Database.Repositories
{
    public class DBPersonalPropertiesRepository
    {
        public static void Initialize()
        {
            SaveSystemBehavior.OnGetPersonalProperty += GetPersonalProperties;
            SaveSystemBehavior.OnCreateOrSavePersonalProperty += CreateOrSavePersonalProperty;
        }
        private static DBPersonalProperties CreateDBPersonalProperty(int PropertyIndex, string propertyName, string propertyBanner)
        {
            return new DBPersonalProperties
            {
                PropertyIndex = PropertyIndex,
                PropertyName = propertyName,
                PropertyBanner = propertyBanner
            };
        }
        public static DBPersonalProperties GetPersonalProperty(int propertyIndex)
        {
            IEnumerable<DBPersonalProperties> pp = DBConnection.Connection.Query<DBPersonalProperties>("SELECT * FROM PersonalProperties WHERE PropertyIndex = @PropertyIndex", new { PropertyIndex = propertyIndex });
            if (pp.Count() == 0) return null;
            return pp.First();
        }

        private static DBPersonalProperties CreateOrSavePersonalProperty(int propertyIndex, string propertyName, string propertyBanner)
        {
            if (GetPersonalProperty(propertyIndex) == null)
            {
                return CreatePersonalProperty(propertyIndex, propertyName, propertyBanner);
            }
            return SavePersonalProperty(propertyIndex, propertyName, propertyBanner); 

        }

        private static DBPersonalProperties SavePersonalProperty(int propertyIndex, string propertyName, string propertyBanner)
        {
            DBPersonalProperties dbPersonalProperties = CreateDBPersonalProperty(propertyIndex, propertyName, propertyBanner);
            string updateSql = "UPDATE PersonalProperties SET PropertyIndex = @PropertyIndex WHERE PropertyIndex = @PropertyIndex";
            DBConnection.Connection.Execute(updateSql, dbPersonalProperties);
            return dbPersonalProperties;
        }

        private static DBPersonalProperties CreatePersonalProperty(int propertyIndex, string propertyName, string propertyBanner)
        {
            DBPersonalProperties dbPersonalProperties = CreateDBPersonalProperty(propertyIndex, propertyName, propertyBanner);
            string insertSql = "INSERT INTO PersonalProperties (PropertyIndex, PropertyName, PropertyBanner) VALUES (@PropertyIndex, @PropertyName, @PropertyBanner)";
            DBConnection.Connection.Execute(insertSql, dbPersonalProperties);
            return dbPersonalProperties;
        }

        private static IEnumerable<DBPersonalProperties> GetPersonalProperties()
        {
            return DBConnection.Connection.Query<DBPersonalProperties>("SELECT * FROM PersonalProperties");
        }
    }
} 