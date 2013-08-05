using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Configuration;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Web;
using System.Text.RegularExpressions;
using System.ServiceModel.Activation;

namespace DieselXrmSvcWrapper
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class SoapSvc : ISoapSvc
    {
        /// <summary>
        /// This method loads the FetchXML query from the correct file in the query directory and does string replacement
        /// for the parameters as necessary.
        /// </summary>
        /// <param name="query">Name of the query. Corresponding file will be named "query".xml</param>
        /// <param name="inputParameters">Array of parameters for substitution in the query.</param>
        /// <returns></returns>
        private string PrepareFetchQuery(string query, List<ParameterItem> inputParameters)
        {
            var appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;

            //we don't actually do anything with this now, but let's leave it in for the future
            //List<string> fetchFields = new List<string>();
            
            string fetchXML = "";

            //load the query xml file for some xpathing
            var doc = new XPathDocument(appPath + @"\"+ConfigurationManager.AppSettings["retrievequerydir"]+ @"\" + query + ".xml");
            XPathNavigator nav = doc.CreateNavigator();

            //xpath query to find the "fetch" element. using "//" instead of "/" if i want to add extra bits to the query
            //files later
            XPathNodeIterator entityIterator = nav.Select("//fetch");
            
            while (entityIterator.MoveNext())
            {
                //get the fetchxml query
                fetchXML = entityIterator.Current.OuterXml;
                
                //populates fetchFields string - leaving in, but commented out for now
                //XPathNodeIterator attributeIterator = entityIterator.Current.Select("entity/attribute");
                //while (attributeIterator.MoveNext())
                //{
                //    XPathNavigator attributeNav = attributeIterator.Current;
                //    if (attributeNav.GetAttribute("alias", "") != "")
                //    {
                //        fetchFields.Add(attributeNav.GetAttribute("alias", ""));
                //    }
                //    else
                //    {
                //        fetchFields.Add(attributeNav.GetAttribute("name", ""));
                //    }
                //}
            }

            //search for substitution placeholders in form of {$PARAMETER NAME}
            string pattern = @"\{\$.*\}";
            Regex rgx = new Regex(pattern);
            MatchCollection matches = rgx.Matches(fetchXML);

            //if we have substitution placeholders, but not corresponding input parameters, throw an error
            if (matches.Count > 0 && inputParameters.Count != matches.Count)
            {
                throw new InvalidOperationException("Number of supplied parameters does not match expected number.");
            }

            //actually do the substitution replacements
            foreach (Match match in matches)
            {
                string parameterName = match.Value.Substring(2, match.Value.Length - 3); //start at third character, stop before last
                string parameterValue = inputParameters.Find(a => a.Name == parameterName).Value.ToString();
                fetchXML = fetchXML.Replace(match.Value, parameterValue);
            }

            //return the final fetchxml query
            return fetchXML;
        }

        /// <summary>
        /// This method runs the query and returns the results to the client
        /// </summary>
        /// <param name="query">Name of the query. Corresponding file will be named "query".xml</param>
        /// <param name="inputParameters">Array of parameters for substitution in the query.</param>
        /// <returns></returns>
        public List<List<ParameterItem>> Retrieve(string query, List<ParameterItem> inputParameters)
        {
            //need to get the id of the user to impersonate from the HttpContext.User object
            HttpContext context = HttpContext.Current;
            string runasuser = "";

            if (context != null)
            {
                //if we're to this point, this value should be populated. if not, we'll see an error later when we try to use a blank id
                runasuser = context.User.Identity.Name;
            }

            //instantiate the output object
            List<List<ParameterItem>> output = new List<List<ParameterItem>>();

            //get the prepared fetchxml query
            string fetchXml = PrepareFetchQuery(query, inputParameters);

            //get a connection to crm
            Microsoft.Xrm.Client.CrmConnection connection = CrmUtils.GetCrmConnection();
            
            //set callerid for impersonation
            connection.CallerId = new Guid(runasuser);

            //execute the query, loop through results, format output, etc.
            using (OrganizationService service = new OrganizationService(connection))
            {
                //code to show impersonated user details - can leave commented out in prod
                //List<ParameterItem> runasitem = new List<ParameterItem>();
                //runasitem.Add(new ParameterItem { Name = "runasuser", Value = runasuser });
                //runasitem.Add(new ParameterItem { Name = "roles", Value = context.User.IsInRole("sales manager").ToString() });
                //output.Add(runasitem);

                EntityCollection results = service.RetrieveMultiple(new FetchExpression(fetchXml));
                foreach (Entity entity in results.Entities)
                {
                    //loop through the mapping key-value pairs
                    List<ParameterItem> item = new List<ParameterItem>();
                    foreach (var attribute in entity.Attributes)
                    {
                        item.Add(new ParameterItem { Name = attribute.Key, Value = CrmUtils.GetAttributeValue(attribute.Value) });

                        //the _name and _type fields do some user-friendly formatting
                        string attributelabel = (string)CrmUtils.GetAttributeName(attribute.Value);
                        if (attributelabel != "")
                        {
                            item.Add(new ParameterItem { Name = attribute.Key + "_name", Value = attributelabel });
                        }

                        string attributeentitytype = (string)CrmUtils.GetAttributeEntityType(attribute.Value);
                        if (attributeentitytype != "")
                        {
                            item.Add(new ParameterItem { Name = attribute.Key + "_type", Value = attributeentitytype });
                        }

                    }

                    //optionset labels and formatted currency values are available in the formattedvalues collection
                    foreach (var fv in entity.FormattedValues)
                    {
                        item.Add(new ParameterItem { Name = fv.Key + "_formattedvalue", Value = fv.Value });
                    }

                    output.Add(item);
                }
                return output;
            }
        }
    }
}
