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

namespace Taobao.Workflow.Activities.AdminWeb.Controllers
{
    public class ErrorController : BaseController
    {
        private ITFlowEngine _engine;
        private string _MiddleWebWhiteList;

        public ErrorController(ITFlowEngine engine
            , string MiddleWebWhiteList)
        {
            this._engine = engine;
            this._MiddleWebWhiteList = MiddleWebWhiteList;
        }

        public ActionResult Index(string p)
        {
            var errors = this._engine.GetErrors();
            ViewBag.Total = errors.Count();
            var page = Convert.ToInt32(p);
            ViewBag.Errors = errors.Skip((page <= 1 ? 0 : page - 1) * 10).Take(10);
            return View();
        }

        [HttpPost]
        public ActionResult RetryProcess(Guid processId)
        {
            this.ValidateOperate(this._MiddleWebWhiteList);

            this._engine.RetryFaultProcess(processId);
            return Json("", JsonRequestBehavior.AllowGet);
        }
    }
}
