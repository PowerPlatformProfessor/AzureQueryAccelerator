using CustomProvider.Model;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CustomProvider
{
    public class RetrieveMultiple : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.Stage == 30)
            {

            }

            EntityCollection ec = new EntityCollection();

            try
            {

                var resp = Common.SendWebRequestAsync("https://queryaccelerator.azurewebsites.net/api/GetOpportunities", HttpMethod.Get);
                var content = resp.Result.Content.ReadAsStringAsync().Result;
                var opportunities = (List<OpportunityHistory>)(new JavaScriptSerializer().Deserialize(content, typeof(List<OpportunityHistory>)));
                tracingService.Trace("Total number of Opportunities: {0}", opportunities.Count());
                ec.Entities.AddRange(opportunities.Select(op => op.ToEntity()));

            }
            catch (Exception e)
            {
                tracingService.Trace("Exception with message: {0}", e.Message);
            }

            
            context.OutputParameters["BusinessEntityCollection"] = ec;
        }
    }
}
