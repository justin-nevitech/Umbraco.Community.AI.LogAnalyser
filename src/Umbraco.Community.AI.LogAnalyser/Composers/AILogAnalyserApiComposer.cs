using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.AI.LogAnalyser.Models;
using Umbraco.Community.AI.LogAnalyser.Services;
#if UMBRACO_18
using Microsoft.AspNetCore.OpenApi;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
#else
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Api.Common.OpenApi;
#endif

namespace Umbraco.Community.AI.LogAnalyser.Composers
{
    public class AILogAnalyserApiComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.Configure<LogContextSettings>(
                builder.Config.GetSection(LogContextSettings.SectionName));

            builder.Services.Configure<AILogAnalyserSettings>(
                builder.Config.GetSection(AILogAnalyserSettings.SectionName));

            builder.Services.AddSingleton<ISystemDiagnosticsProvider, SystemDiagnosticsProvider>();
            builder.Services.AddTransient<ILogContextProvider, LogContextProvider>();

            ConfigureOpenApi(builder);
        }

#if UMBRACO_18
        // Umbraco 18 replaced Swashbuckle with Microsoft.AspNetCore.OpenApi. Custom backoffice
        // API documents are registered via the AddBackOfficeOpenApiDocument helper, which wires
        // up Umbraco's conventions and OAuth2 backoffice authentication in one call. The document
        // name must match the [MapToApi(Constants.ApiName)] on the controller.
        private static void ConfigureOpenApi(IUmbracoBuilder builder)
        {
            builder.AddBackOfficeOpenApiDocument(
                Constants.ApiName,
                document => document
                    .WithTitle("AI Log Analyser Backoffice API")
                    .WithBackOfficeAuthentication()
                    // Emit clean operation IDs (the action name only) so the generated
                    // TypeScript client has concise method names — the v18 replacement for
                    // the v17 IOperationIdHandler. The transformer is scoped to this document.
                    .ConfigureOpenApiOptions(options =>
                        options.AddOperationTransformer((operation, context, _) =>
                        {
                            operation.OperationId = $"{context.Description.ActionDescriptor.RouteValues["action"]}";
                            return Task.CompletedTask;
                        })));
        }
#else
        // Umbraco 17 (Swashbuckle-based). Registers a dedicated Swagger document for this API
        // group, applies the backoffice security requirement, and emits clean operation IDs so
        // the generated TypeScript client has concise method names.
        private static void ConfigureOpenApi(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IOperationIdHandler, CustomOperationHandler>();

            builder.Services.Configure<SwaggerGenOptions>(opt =>
            {
                opt.SwaggerDoc(Constants.ApiName, new OpenApiInfo
                {
                    Title = "AI Log Analyser Backoffice API",
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
#endif
    }
}
