using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace Umbraco.Community.AI.LogAnalyser.Controllers
{
    [ApiController]
    [BackOfficeRoute("ailoganalyser/api/v{version:apiVersion}")]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
    [MapToApi(Constants.ApiName)]
    public class AILogAnalyserApiControllerBase : ControllerBase
    {
    }
}
