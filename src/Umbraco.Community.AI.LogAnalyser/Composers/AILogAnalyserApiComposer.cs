using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Community.AI.LogAnalyser.Models;
using Umbraco.Community.AI.LogAnalyser.Services;

namespace Umbraco.Community.AI.LogAnalyser.Composers
{
    public class AILogAnalyserApiComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.Configure<LogContextSettings>(
                builder.Config.GetSection(LogContextSettings.SectionName));

            builder.Services.AddSingleton<IOperationIdHandler, CustomOperationHandler>();
            builder.Services.AddSingleton<ISystemDiagnosticsProvider, SystemDiagnosticsProvider>();
            builder.Services.AddTransient<ILogContextProvider, LogContextProvider>();

            builder.Services.Configure<SwaggerGenOptions>(opt =>
            {
                opt.SwaggerDoc(Constants.ApiName, new OpenApiInfo
                {
                    Title = "AILog Analyser Backoffice API",
                    Version = "1.0",
                });

                opt.OperationFilter<AILogAnalyserOperationSecurityFilter>();
            });
        }

        public class AILogAnalyserOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
        {
            protected override string ApiName => Constants.ApiName;
        }

        /// <summary>
        /// Generates clean operation IDs in the Swagger JSON so that the generated TypeScript client has concise method names.
        /// See: https://docs.umbraco.com/umbraco-cms/tutorials/creating-a-backoffice-api/umbraco-schema-and-operation-ids#operation-ids
        /// </summary>
        public class CustomOperationHandler : OperationIdHandler
        {
            public CustomOperationHandler(IOptions<ApiVersioningOptions> apiVersioningOptions) : base(apiVersioningOptions)
            {
            }

            protected override bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor)
            {
                return controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith("Umbraco.Community.AI.LogAnalyser.Controllers", comparisonType: StringComparison.InvariantCultureIgnoreCase) is true;
            }

            public override string Handle(ApiDescription apiDescription) => $"{apiDescription.ActionDescriptor.RouteValues["action"]}";
        }
    }
}
