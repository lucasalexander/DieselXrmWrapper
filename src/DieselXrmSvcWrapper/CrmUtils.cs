using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DieselXrmSvcWrapper
{
    /// <summary>
    /// class to hold a some CRM-related methods
    /// </summary>
    public class CrmUtils
    {
        /// <summary>
        /// gets a CRM connection using supplied username and password + AppSettings["crmconnectionstring"]
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Microsoft.Xrm.Client.CrmConnection GetCrmConnection(string username, string password)
        {
            string connectionString = ConfigurationManager.AppSettings["crmconnectionstring"];
            connectionString += " Username=" + username + "; Password=" + password;
            Microsoft.Xrm.Client.CrmConnection connection = CrmConnection.Parse(connectionString);
            return connection;
        }

        /// <summary>
        /// gets a CRM connection using AppSettings["crmconnectionstring"] + AppSettings["impersonatorcredentials"]
        /// </summary>
        /// <returns></returns>
        public static Microsoft.Xrm.Client.CrmConnection GetCrmConnection()
        {
            string connectionString = ConfigurationManager.AppSettings["crmconnectionstring"] + ConfigurationManager.AppSettings["impersonatorcredentials"];
            Microsoft.Xrm.Client.CrmConnection connection = CrmConnection.Parse(connectionString);
            return connection;
        }

        /// <summary>
        /// gets a CRM entity's attribute value
        /// </summary>
        /// <param name="entityValue"></param>
        /// <returns></returns>
        public static object GetAttributeValue(object entityValue)
        {
            object output = "";
            switch (entityValue.ToString())
            {
                case "Microsoft.Xrm.Sdk.EntityReference":
                    output = ((EntityReference)entityValue).Id;
                    break;
                case "Microsoft.Xrm.Sdk.OptionSetValue":
                    output = ((OptionSetValue)entityValue).Value;
                    break;
                case "Microsoft.Xrm.Sdk.Money":
                    output = ((Money)entityValue).Value;
                    break;
                case "Microsoft.Xrm.Sdk.AliasedValue":
                    output = GetAttributeValue(((Microsoft.Xrm.Sdk.AliasedValue)entityValue).Value);
                    break;
                default:
                    output = entityValue.ToString();
                    break;
            }
            return output;
        }

        /// <summary>
        /// gets a CRM entity's attribute name
        /// </summary>
        /// <param name="entityValue"></param>
        /// <returns></returns>
        public static object GetAttributeName(object entityValue)
        {
            object output = "";
            switch (entityValue.ToString())
            {
                case "Microsoft.Xrm.Sdk.EntityReference":
                    output = ((EntityReference)entityValue).Name;
                    break;
                case "Microsoft.Xrm.Sdk.AliasedValue":
                    output = GetAttributeName(((Microsoft.Xrm.Sdk.AliasedValue)entityValue).Value);
                    break;
                default:
                    output = "";
                    break;
            }
            return output;
        }

        /// <summary>
        /// gets a CRM entity's attribute type
        /// </summary>
        /// <param name="entityValue"></param>
        /// <returns></returns>
        public static object GetAttributeEntityType(object entityValue)
        {
            object output = "";
            switch (entityValue.ToString())
            {
                case "Microsoft.Xrm.Sdk.EntityReference":
                    output = ((EntityReference)entityValue).LogicalName;
                    break;
                case "Microsoft.Xrm.Sdk.AliasedValue":
                    output = GetAttributeEntityType(((Microsoft.Xrm.Sdk.AliasedValue)entityValue).Value);
                    break;
                default:
                    output = "";
                    break;
            }
            return output;
        }

        public static Entity GetSystemUser(Guid userid, OrganizationService service)
        {
            Entity systemuser = service.Retrieve("systemuser", userid, new ColumnSet(true));
            return systemuser;
        }

        /// <summary>
        /// retrieves a list of CRM roles assigned to a specific user
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static List<string> GetUserRoles(Guid userid, OrganizationService service)
        {
            List<string> roles = new List<string>();

            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
              <entity name='role'>
                <attribute name='name' />
                <attribute name='businessunitid' />
                <attribute name='roleid' />
                <order attribute='name' descending='false' />
                <link-entity name='systemuserroles' from='roleid' to='roleid' visible='false' intersect='true'>
                  <link-entity name='systemuser' from='systemuserid' to='systemuserid' alias='af'>
                    <filter type='and'>
                      <condition attribute='systemuserid' operator='eq' uitype='systemuser' value='{$USERID}' />
                    </filter>
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>";
            fetchXml = fetchXml.Replace("$USERID", userid.ToString());
            EntityCollection results = service.RetrieveMultiple(new FetchExpression(fetchXml));
            foreach (Entity entity in results.Entities)
            {
                roles.Add((string)entity["name"]);
            }
            return roles;
        }
    }
}