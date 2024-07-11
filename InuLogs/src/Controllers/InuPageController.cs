using Google.Protobuf.WellKnownTypes;
using InuLogs.src.Filters;
using InuLogs.src.Helpers;
using InuLogs.src.Managers;
using InuLogs.src.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Immutable;
using System.Linq;

namespace InuLogs.src.Controllers
{
    [AllowAnonymous]
    public class InuPageController : Controller
    {
        public InuPageController()
        {

        }

        [CustomAuthenticationFilter]
        public async Task<JsonResult> Index(string searchString = "", string verbString = "", string statusCode = "", int pageNumber = 1, int resultCode = -1)
        {
            var result = await DynamicDBManager.GetAllInuLogs(searchString, verbString, statusCode, pageNumber, resultCode);
            return Json(new { PageIndex = result.PageIndex, TotalPages = result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result.Data }, GeneralHelper.CamelCaseSerializer);
        }

        [CustomAuthenticationFilter]
        public async Task<JsonResult> Exceptions(string searchString = "", int pageNumber = 1)
        {
            var result = await DynamicDBManager.GetAllInuExceptionLogs(searchString, pageNumber);
            return Json(new { PageIndex = result.PageIndex, TotalPages = result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result.Data }, GeneralHelper.CamelCaseSerializer);
        }

        [CustomAuthenticationFilter]
        public async Task<JsonResult> Logs(string searchString = "", string logLevelString = "", int pageNumber = 1)
        {
            var result = await DynamicDBManager.GetAllLogs(searchString, logLevelString, pageNumber);
            return Json(new { PageIndex = result.PageIndex, TotalPages = result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result.Data }, GeneralHelper.CamelCaseSerializer);
        }

        [CustomAuthenticationFilter]
        public async Task<JsonResult> ClearLogs()
        {
            var cleared = await DynamicDBManager.ClearLogs();
            return Json(cleared);
        }


        [HttpPost]
        public JsonResult Auth(string username, string password)
        {

            if (username.ToLower() == InuLogsConfigModel.UserName.ToLower() && password == InuLogsConfigModel.Password)
            {
                HttpContext.Session.SetString("isAuth", "true");
                return Json(true);
            }
            else
            {
                return Json(false);
            }
        }

        public JsonResult LogOut()
        {
            HttpContext.Session.Remove("isAuth");
            return Json(true);
        }

        public JsonResult IsAuth()
        {

            if (!HttpContext.Session.TryGetValue("isAuth", out var isAuth))
            {
                return Json(false);
            }
            else
            {
                return Json(true);
            }
        }
        [CustomAuthenticationFilter]
        public JsonResult RequestRetry([FromBody] RequestRetryInput input)
        {
            Dictionary<string, string> HeaderDic = new Dictionary<string, string>();
            //Dictionary<string, string> BodyDic = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(input.headers))
            {
                HeaderDic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(input.headers);
            }
            string retrunString = "";
            try
            {
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                HttpClient client = new HttpClient(httpClientHandler);
                if (HeaderDic.Count > 0)
                {
                    foreach (var headeritem in HeaderDic)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(headeritem.Key, headeritem.Value);
                    }
                }
                if (input.method.ToLower() == "get")
                {
                    var response = client.GetAsync(input.url).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    retrunString = result;
                }
                else if (input.method.ToLower() == "post")
                {
                    var buffer = Encoding.UTF8.GetBytes(input.body);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    var response = client.PostAsync(input.url, byteContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    retrunString = result;
                }
                else if (input.method.ToLower() == "put")
                {
                    var buffer = Encoding.UTF8.GetBytes(input.body);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    var response = client.PutAsync(input.url, byteContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    retrunString = result;
                }
                else if (input.method.ToLower() == "delete")
                {
                    var response = client.DeleteAsync(input.url).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    retrunString = result;
                }
                else if (input.method.ToLower() == "patch")
                {
                    var buffer = Encoding.UTF8.GetBytes(input.body);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    var response = client.PatchAsync(input.url, byteContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    retrunString = result;
                }
            }
            catch (Exception e)
            {
                retrunString = "请求异常：" + e.Message;
            }
            return Json(retrunString);
        }
    }
}
