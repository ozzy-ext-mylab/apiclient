﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedCucumber.Wac
{
    partial class WebApiMethodDescription
    {
        private static void InitRelPAthAndHttpMethod(MethodInfo method,
            WebApiMethodDescription d,
            out Wac.ContentType defaultSubmitContentType)
        {
            var srvEp = method.GetCustomAttribute<ServiceEndpointAttribute>();
            if (srvEp != null)
            {
                d.RelPath = srvEp.Path;
                d.HttpMethod = srvEp.Method;

                defaultSubmitContentType = ContentType.UrlEncodedForm;
            }
            else
            {
                var rcEp = method.GetCustomAttribute<ResourceActionAttribute>();
                if (rcEp != null)
                {
                    d.HttpMethod = rcEp.Method;
                    defaultSubmitContentType = ContentType.Json;
                }
                else
                {
                    throw new WebApiContractException("Method is not endpoint");
                }
            }
        }

        private static void InitContentType(MethodInfo method,
            WebApiMethodDescription d,
            Wac.ContentType defaultSubmitContentType)
        {
            if (d.HttpMethod == HttpMethod.Get || d.HttpMethod == HttpMethod.Delete)
            {
                d.ContentType = ContentType.Undefined;
            }
            else
            {
                var contentTypeAttr = method.GetCustomAttribute<ContentTypeAttribute>();
                d.ContentType = contentTypeAttr?.ContentType ?? defaultSubmitContentType;
            }
        }

        private static void InitParameters(MethodInfo method, WebApiMethodDescription d)
        {
            var parameters = method.GetParameters()
                .Select(WebApiParameterDescription.Create)
                .ToArray();

            foreach (var p in parameters.Where(prm => prm.Type == WebApiParameterType.Undefined))
            {
                if (d.HttpMethod == HttpMethod.Get)
                {
                    p.Type = WebApiParameterType.Get;
                }
                else
                {
                    switch (d.ContentType)
                    {
                        case ContentType.FromData:
                        case ContentType.UrlEncodedForm:
                            p.Type = WebApiParameterType.FormItem;
                            break;
                        case ContentType.Text:
                        case ContentType.Xml:
                        case ContentType.Html:
                        case ContentType.Json:
                        case ContentType.Javascript:
                            p.Type = WebApiParameterType.Payload;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            d.Parameters = new WebApiParameterDescriptions(parameters);
        }

        static void CheckParametersConflict(WebApiParameterDescriptions parameters)
        {
            var payloadParamsCount = parameters.Values.Count(p => p.Type == WebApiParameterType.Payload);
            if (payloadParamsCount != 0)
            {
                if (payloadParamsCount > 1)
                    throw new WebApiContractException("Should be one user several paload parameters in the same method");

                if(parameters.Values.Count(p => p.Type == WebApiParameterType.FormItem) !=0)
                    throw new WebApiContractException("Shouldn't use paload parameter and one or more form parameters in the same method");
            }
        }

        public static WebApiMethodDescription Create(MethodInfo method)
        {
            var d = new WebApiMethodDescription
            {
                MethodId = CreateMethodId(method.Name, method.GetParameters().Select(p => p.Name))
            };

            Wac.ContentType defaultSubmitContentType;

            InitRelPAthAndHttpMethod(method, d, out defaultSubmitContentType);

            InitContentType(method, d, defaultSubmitContentType);

            InitParameters(method, d);

            CheckParametersConflict(d.Parameters);

            return d;
        }

        public static string CreateMethodId(string methodName, IEnumerable<string> parameterNames)
        {
            return methodName + string.Join("_", parameterNames);
        }
    }
}