using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Principal;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using System.ServiceModel;
using System.Web;

namespace DieselXrmSvcWrapper
{
    /// <summary>
    /// class used for username/password authentication against CRM
    /// </summary>
    public class CrmUsernamePasswordValidator : UserNamePasswordValidator
    {
        /// <summary>
        /// Validate method to attempt to connect to CRM with supplied username/password and then execute a whoami request
        /// </summary>
        /// <param name="username">crm username</param>
        /// <param name="password">crm password</param>
        public override void Validate(string username, string password)
        {
            //get the httpcontext so we can store the user guid for impersonation later
            HttpContext context = HttpContext.Current;

            //if username or password are null, obvs we can't continue
            if (null == username || null == password)
            {
                throw new ArgumentNullException();
            }

            //get the crm connection
            Microsoft.Xrm.Client.CrmConnection connection = CrmUtils.GetCrmConnection(username, password);

            //try the whoami request
            //if it fails (user can't be authenticated, is disabled, etc.), the client will get a soap fault message
            using (OrganizationService service = new OrganizationService(connection))
            {
                try
                {
                    WhoAmIRequest req = new WhoAmIRequest();
                    WhoAmIResponse resp = (WhoAmIResponse)service.Execute(req);
                    List<string> roles = CrmUtils.GetUserRoles(resp.UserId, service);
                    context.User = new GenericPrincipal(new GenericIdentity(resp.UserId.ToString()), roles.ToArray());
                }
                catch (System.ServiceModel.Security.MessageSecurityException ex)
                {
                    throw new FaultException(ex.Message); 
                }
                catch (Exception ex)
                {
                    throw new FaultException(ex.Message);
                }
            }
        }
    }
}