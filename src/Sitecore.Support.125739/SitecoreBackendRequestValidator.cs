// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Sitecore Corporation" file="SitecoreRequestValidator.cs">
//   Copyright (C) 2016 by Sitecore Corporation
// </copyright>
// <summary>
//    Will suppress form validaton for known URLs:
//    https://msdn.microsoft.com/en-us/library/system.web.configuration.httpruntimesection.requestvalidationtype(v=vs.110).aspx
// </summary>
// -------------------------------------------------------------------------------------------------------------------- 


using System;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Util;
using Sitecore.Diagnostics;
using Sitecore.Abstractions;
using Sitecore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Sitecore.Support.Web.RequestValidators
{
    /// <summary>
    /// Suppresses form validation for known Sitecore URLs ( like shell and SPEAK).
    /// <para><see cref="HttpRuntimeSection.RequestValidationType"/> on MSDN.</para>
    /// </summary>
    /// <summary>
    /// Suppresses form validation for known Sitecore URLs ( like shell and SPEAK).
    /// <para><see cref="HttpRuntimeSection.RequestValidationType"/> on MSDN.</para>
    /// </summary>
    public class SitecoreBackendRequestValidator : RequestValidator
    {
        /// <summary>
        /// Defines a set of well-known, trusted Sitecore URLs.
        /// </summary>
        [NotNull]
        internal static readonly string[] SitecoreTrustedUrls = new string[]
        {
            "/sitecore/shell/", "/sitecore/admin/", "/-/speak/request/"
        };

        #region Constructors

        /// <summary>
        /// Initializes an instance of <see cref="SitecoreBackendRequestValidator"/> type with <see cref="SitecoreTrustedUrls"/> to bypass validation.
        /// </summary>
        public SitecoreBackendRequestValidator()
            : this(ServiceLocator.ServiceProvider.GetRequiredService<BaseSiteManager>(), SitecoreTrustedUrls)
        {

        }

        /// <summary>
        /// Initializes an instance of <see cref="SitecoreBackendRequestValidator"/> type.
        /// </summary>
        /// <param name="siteManager">Instance of BaseSiteManager class.</param>
        /// <param name="urlStartPartsToBypass">>Trusted urls to skip form validation.</param>
        protected SitecoreBackendRequestValidator(BaseSiteManager siteManager, params string[] urlStartPartsToBypass)
            : this(ExtractLoginPages(siteManager).Union(urlStartPartsToBypass).ToArray())
        {

        }

        /// <summary>
        /// Initializes an instance of <see cref="SitecoreBackendRequestValidator"/> type.
        /// </summary>
        /// <param name="urlStartPartsToBypass">Trusted urls to skip form validation.</param>
        protected SitecoreBackendRequestValidator(params string[] urlStartPartsToBypass)
        {
            this.UrlStartPartsToBypassValidation = urlStartPartsToBypass;
        }

        #endregion

        /// <summary>
        /// List of trusted urls that request starts with.        
        /// </summary>
        [NotNull]
        protected string[] UrlStartPartsToBypassValidation { get; private set; }

        /// <summary>
        /// Check if a validation of the incoming form values should be ignored.
        /// </summary>
        /// <param name="rawUrl">The request to be checked.</param>
        /// <returns><value>true</value> if <paramref name="rawUrl"/> is trusted, and validation should not take place;<c>false</c> otherwise.</returns>
        public virtual bool ShouldIgnoreValidation([NotNull] string rawUrl)
        {
            Assert.ArgumentNotNull(rawUrl, "request");

            return this.UrlStartPartsToBypassValidation.Any(urlToByPass => rawUrl.StartsWith(urlToByPass, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Validates a string that contains HTTP request data.
        /// </summary>
        /// <param name="context">The context of the current request.</param>
        /// <param name="value">The HTTP request data to validate.</param>
        /// <param name="requestValidationSource">An enumeration that represents the source of request data that is being validated. The following are possible values for the enumeration:QueryStringForm CookiesFilesRawUrlPathPathInfoHeaders.</param>
        /// <param name="collectionKey">The key in the request collection of the item to validate. This parameter is optional. This parameter is used if the data to validate is obtained from a collection. If the data to validate is not from a collection, <paramref name="collectionKey" /> can be null.</param>
        /// <param name="validationFailureIndex">When this method returns, indicates the zero-based starting point of the problematic or invalid text in the request collection. This parameter is passed uninitialized.</param>
        /// <returns>
        /// <value>true</value> if the string to be validated is valid; <value>false</value> otherwise.
        /// </returns>
        protected override bool IsValidRequestString(HttpContext context, string value, RequestValidationSource requestValidationSource, string collectionKey, out int validationFailureIndex)
        {
            validationFailureIndex = 0;

            var requestUrl = this.ExtractRequestUrl(context);

            if (this.ShouldIgnoreValidation(requestUrl))
            {
                return true;
            }

            return base.IsValidRequestString(context, value, requestValidationSource, collectionKey, out validationFailureIndex);
        }

        /// <summary>
        /// Extracts request url from given context. 
        /// </summary>
        /// <param name="context">Request information.</param>
        /// <returns><see cref="HttpRequest.RawUrl"/> from provided context.</returns>
        public virtual string ExtractRequestUrl([CanBeNull] HttpContext context)
        {
            if ((context == null) || (context.Request == null))
            {
                return string.Empty;
            }

            return context.Request.RawUrl ?? string.Empty;
        }

        /// <summary>
        /// Gets URLs of Login pages to bypass validation
        /// </summary>
        /// <param name="siteManager"></param>
        /// <returns>string array of Login pages Urls to bypass validation</returns>
        private static string[] ExtractLoginPages(BaseSiteManager siteManager)
        {
            var result = siteManager.GetSites().Where(site => site.Properties.ContainsKey("loginPage")).Select(site => site.Properties["loginPage"]);
            return result.ToArray();
        }

    }
}