/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Taobao.Workflow.Activities.Management;
using System.IO;
using System.Xml.Linq;

namespace Taobao.Workflow.Activities.AdminWeb.Controllers
{
    public class ProcessTypeController : BaseController
    {
        private ITFlowEngine _managementApi;
        private string _MiddleWebWhiteList;

        public ProcessTypeController(ITFlowEngine managementApi
            , string MiddleWebWhiteList)
        {
            this._managementApi = managementApi;
            this._MiddleWebWhiteList = MiddleWebWhiteList;
        }

        public ActionResult Index(string p)
        {
            var processTypes = this._managementApi.GetProcessTypes();
            ViewBag.Total = processTypes.Count();
            var page = Convert.ToInt32(p);
            ViewBag.ProcessTypes = processTypes.Skip((page <= 1 ? 0 : page - 1) * 20).Take(20);
            return View();
        }

        public ActionResult Version(string processTypeName, string p)
        {
            var processTypes = this._managementApi.GetAllVersionsOfProcessType(processTypeName)
                .OrderByDescending(o => o.Version);
            ViewBag.Total = processTypes.Count();
            var page = Convert.ToInt32(p);
            ViewBag.ProcessTypes = processTypes.Skip((page <= 1 ? 0 : page - 1) * 20).Take(20);
            return View();
        }

        [HttpPost]
        public ActionResult SetVersion(string version, string processTypeName)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._managementApi.SetCurrentProcessType(processTypeName, version);
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(string processTypeName, string description, string group)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            if(string.IsNullOrEmpty(processTypeName) || string.IsNullOrEmpty(group))
                throw new InvalidOperationException("processTypeName为空 或者 group为空");
            var files = this.Request.Files;
            HttpPostedFileBase file = null;
            if (files.Count > 0)
                file = files[0];
            if (Path.GetExtension(file.FileName).ToLower() != ".xml")
                throw new Exception("文件格式不对，请使用xml格式");
            var xml = "";
            using (StreamReader reader = new StreamReader(file.InputStream))
                xml = reader.ReadToEnd();

            string workflowXml = "", settingsXml = "";
            this.ParseXml(xml, ref workflowXml, ref settingsXml);

            this._managementApi.CreateProcessType(processTypeName
                , workflowXml
                , settingsXml
                , description
                , group);
            return this.RedirectToAction("Index");
        }

        //解析Xml为workflowXml和settingsXml
        private void ParseXml(string xml, ref string workflowXml, ref string settingsXml)
        {
            var root = XElement.Parse(xml);
            var activitiesElement = root.Descendants("activities").FirstOrDefault();

            settingsXml = activitiesElement.ToString();
            activitiesElement.Remove();
            workflowXml = root.ToString();
        }
    }
}
